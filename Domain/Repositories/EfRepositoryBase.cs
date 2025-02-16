using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Domain.Dynamic;
using Domain.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Domain.Repositories;

namespace Domain.Repositories;

public class EfRepositoryBase<TEntity, TEntityId, TContext>
    : IAsyncRepository<TEntity, TEntityId>, IRepository<TEntity, TEntityId>
    where TEntity : Entitiy<TEntityId>
    where TContext : DbContext //sen bir dbcontext olmak zorundasın 
{
    protected readonly TContext Context;
    public EfRepositoryBase(TContext context)
    {
        Context = context;
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        entity.CreatedDate = DateTime.UtcNow;
        await Context.AddAsync(entity);
        await Context.SaveChangesAsync();
        return entity;

    }

    public async Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities)
    {

        foreach (TEntity entity in entities)

            entity.CreatedDate = DateTime.UtcNow;
        await Context.AddAsync(entities);
        await Context.SaveChangesAsync();
        return entities;
    }

    //belirtiğiz filtre değerine göre elimizde veri var mı yok mu 
    public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();//eğer silinenleri difolt olarak gtirme 
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.AnyAsync(cancellationToken);
    }

    public async Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entity, permanent);
        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false)
    {
        await SetEntityAsDeletedAsync(entities, permanent);
        await Context.SaveChangesAsync();
        return entities;
    }



    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    public async Task<Paginate<TEntity>> GetListAsync(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {//joinli yapılarımızın hepsine destek veriri 
        IQueryable<TEntity> queryable = Query();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        if (orderBy != null)
            return await orderBy(queryable).ToPaginateAsync(index, size, cancellationToken);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public async Task<Paginate<TEntity>> GetListByDynamicAsync(DynamicQuery dynamic, Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Query().ToDynamic(dynamic);
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);
        return await queryable.ToPaginateAsync(index, size, cancellationToken);
    }

    public IQueryable<TEntity> Query() => Context.Set<TEntity>();


    public async Task<TEntity> UpdateAsync(TEntity entity)
    {
        entity.UpdateDate = DateTime.UtcNow;
        Context.Update(entity);
        await Context.SaveChangesAsync();
        return entity;
    }

    public async Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities)
    {
        foreach (TEntity entity in entities)
            entity.UpdateDate = DateTime.UtcNow;
        Context.Update(entities);
        await Context.SaveChangesAsync();
        return entities;
    }

    protected async Task SetEntityAsDeletedAsync(TEntity entity, bool permanent)
    {
        if (!permanent)
        {
            CheckHasEntityHaveOneToOneRelation(entity);
            await setEntityAsSoftDeletedAsync(entity);
        }
        else
        {
            Context.Remove(entity);
        }
    }

    protected void CheckHasEntityHaveOneToOneRelation(TEntity entity)
    {
        bool hasEntityHaveOneToOneRelation =
            Context
                .Entry(entity)
                .Metadata.GetForeignKeys()
                .All(
                    x =>
                        x.DependentToPrincipal?.IsCollection == true
                        || x.PrincipalToDependent?.IsCollection == true
                        || x.DependentToPrincipal?.ForeignKey.DeclaringEntityType.ClrType == entity.GetType()
                ) == false;
        if (hasEntityHaveOneToOneRelation)
            throw new InvalidOperationException(
                "Entity has one-to-one relationship. Soft Delete causes problems if you try to create entry again by same foreign key."
            );
    }

    private async Task setEntityAsSoftDeletedAsync(IEntityTimestamps entity)
    {
        if (entity.DeletedDate.HasValue)
            return;
        entity.DeletedDate = DateTime.UtcNow;

        var navigations = Context
            .Entry(entity)
            .Metadata.GetNavigations()
            .Where(x => x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade })
            .ToList();
        foreach (INavigation? navigation in navigations)
        {
            if (navigation.TargetEntityType.IsOwned())
                continue;
            if (navigation.PropertyInfo == null)
                continue;

            object? navValue = navigation.PropertyInfo.GetValue(entity);
            if (navigation.IsCollection)
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType()).ToListAsync();
                    if (navValue == null)
                        continue;
                }

                foreach (IEntityTimestamps navValueItem in (IEnumerable)navValue)
                    await setEntityAsSoftDeletedAsync(navValueItem);
            }
            else
            {
                if (navValue == null)
                {
                    IQueryable query = Context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();
                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
                        .FirstOrDefaultAsync();
                    if (navValue == null)
                        continue;
                }

                await setEntityAsSoftDeletedAsync((IEntityTimestamps)navValue);
            }
        }

        Context.Update(entity);
    }

    protected IQueryable<object> GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
    {
        Type queryProviderType = query.Provider.GetType();
        MethodInfo createQueryMethod =
            queryProviderType
                .GetMethods()
                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
                ?.MakeGenericMethod(navigationPropertyType)
            ?? throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
        var queryProviderQuery =
            (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: new object[] { query.Expression })!;
        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
    }

    protected async Task SetEntityAsDeletedAsync(IEnumerable<TEntity> entities, bool permanent)
    {
        foreach (TEntity entity in entities)
            await SetEntityAsDeletedAsync(entity, permanent);
    }

    public TEntity? Get(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetList(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public Paginate<TEntity> GetListByDynamic(DynamicQuery dynamic, Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public bool Any(Expression<Func<TEntity, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true)
    {
        throw new NotImplementedException();
    }

    public TEntity Add(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> AddRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public TEntity Update(TEntity entity)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> UpdateRange(ICollection<TEntity> entities)
    {
        throw new NotImplementedException();
    }

    public TEntity Delete(TEntity entity, bool permanent = false)
    {
        throw new NotImplementedException();
    }

    public ICollection<TEntity> DeleteRange(ICollection<TEntity> entity, bool permanent = false)
    {
        throw new NotImplementedException();
    }

  
}
//public class EfRepositoryBase<TEntitiy, TEntitiyId, TContext> : IAsyncRepository<TEntitiy, TEntitiyId>, IRepository<TEntitiy, TEntitiyId>
//    where TEntitiy : Entity<TEntitiyId>
//    where TContext : DbContext //sen bir dbcontext olmak zorundasın 
//{
//    protected readonly TContext Context;

//    public EfRepositoryBase(TContext context)
//    {
//        Context = context;
//    }

//    public async Task<TEntitiy> AddAsync(TEntitiy entity)
//    {
//        entity.CreatedDate = DateTime.UtcNow;
//        await Context.AddAsync(entity);
//        await Context.SaveChangesAsync();
//        return entity;

//    }

//    public async Task<ICollection<TEntitiy>> AddRangeAsync(ICollection<TEntitiy> entities)
//    {

//        foreach (TEntitiy entity in entities)

//            entity.CreatedDate = DateTime.UtcNow;
//        await Context.AddAsync(entities);
//        await Context.SaveChangesAsync();
//        return entities;
//    }

//    //belirtiğiz filtre değerine göre elimizde veri var mı yok mu 
//    public async Task<bool> AnyAsync(Expression<Func<TEntitiy, bool>>? predicate = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
//    {
//        IQueryable<TEntitiy> queryable = Query();
//        if (!enableTracking)
//            queryable = queryable.AsNoTracking();
//        if (withDeleted)
//            queryable = queryable.IgnoreQueryFilters();//eğer silinenleri difolt olarak gtirme 
//        if (predicate != null)
//            queryable = queryable.Where(predicate);
//        return await queryable.AnyAsync(cancellationToken);
//    }

//    public async Task<TEntitiy> DeleteAsync(TEntitiy entity, bool permanent = false)//silmeyi permenent mı kalıcı mı yapacam ayarı olur 
//       //eğer kalıcı olmazsa update yapmamız lazım 
//    {
//        await SetEntitiyAsDeletedAsync(entity, permanent);
//        await Context.SaveChangesAsync();
//        return entity;

//    }

//    public async Task<ICollection<TEntitiy>> DeleteRangeAsync(ICollection<TEntitiy> entities, bool permanent = false)
//    {
//        await SetEntityAsDeletedAsync(entities, permanent);
//        await Context.SaveChangesAsync();
//        return entities;
//    }

//    public Task<TEntitiy?> GetAsync(Expression<Func<TEntitiy, bool>> predicate, Func<IQueryable<TEntitiy>, IIncludableQueryable<TEntitiy, object>>? include = null, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<Paginate<TEntitiy>> GetListAsync(Expression<Func<TEntitiy, bool>>? predicate = null, Func<IQueryable<TEntitiy>, IOrderedQueryable<TEntitiy>>? orderBy = null, Func<IQueryable<TEntitiy>, IIncludableQueryable<TEntitiy, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<Paginate<TEntitiy>> GetListByDynamicAsync(DynamicQuery dynamic, Expression<Func<TEntitiy, bool>>? predicate = null, Func<IQueryable<TEntitiy>, IIncludableQueryable<TEntitiy, object>>? include = null, int index = 0, int size = 10, bool withDeleted = false, bool enableTracking = true, CancellationToken cancellationToken = default)
//    {
//        throw new NotImplementedException();
//    }

//    public IQueryable<TEntitiy> Query()
//    {
//        throw new NotImplementedException();
//    }

//    public Task<TEntitiy> UpdateAsync(TEntitiy entity)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<ICollection<TEntitiy>> UpdateRangeAsync(ICollection<TEntitiy> entities)
//    {
//        throw new NotImplementedException();
//    }
//    protected async Task SetEntitiyAsDeletedAsync(TEntitiy entity, bool permanent)
//    {
//        if (!permanent)//eğer kalıcı değise her yerdeen sileceğiz 
//        {
//            CheckHasEntityHaveOneToOneRelation(entity);//bakbakalım bire bir ilişkisi varmı yani bireysel ve kurumsal bankacılıktaan ikisinide silme 
//            await setEntityAsSoftDeletedAsync(entity);
//        }
//        else
//        {
//            Context.Remove(entity);
//        }
//    }

//    protected void CheckHasEntityHaveOneToOneRelation(TEntitiy entity)
//    {
//        bool hasEntityHaveOneToOneRelation =
//            Context
//                .Entry(entity)
//                .Metadata.GetForeignKeys()
//                .All(
//                    x =>
//                        x.DependentToPrincipal?.IsCollection == true
//                        || x.PrincipalToDependent?.IsCollection == true
//                        || x.DependentToPrincipal?.ForeignKey.DeclaringEntityType.ClrType == entity.GetType()//forinkey olan var mı birebir ilişkisi olan var mı ona bak 
//                ) == false;
//        if (hasEntityHaveOneToOneRelation)
//            throw new InvalidOperationException(
//                "Entity has one-to-one relationship. Soft Delete causes problems if you try to create entry again by same foreign key."
//            );
//    }
//    private async Task setEntityAsSoftDeletedAsync(IEntityTimestamps entity)
//    {
//        if (entity.DeletedDate.HasValue)
//            return;
//        entity.DeletedDate = DateTime.UtcNow;

//        var navigations = Context
//            .Entry(entity)
//            .Metadata.GetNavigations()
//            .Where(x => x is { IsOnDependent: false, ForeignKey.DeleteBehavior: DeleteBehavior.ClientCascade or DeleteBehavior.Cascade })
//            .ToList();
//        foreach (INavigation? navigation in navigations)
//        {
//            if (navigation.TargetEntityType.IsOwned())
//                continue;
//            if (navigation.PropertyInfo == null)
//                continue;

//            object? navValue = navigation.PropertyInfo.GetValue(entity);
//            if (navigation.IsCollection)
//            {
//                if (navValue == null)
//                {
//                    IQueryable query = Context.Entry(entity).Collection(navigation.PropertyInfo.Name).Query();
//                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType()).ToListAsync();
//                    if (navValue == null)
//                        continue;
//                }

//                foreach (IEntityTimestamps navValueItem in (IEnumerable)navValue)
//                    await setEntityAsSoftDeletedAsync(navValueItem);
//            }
//            else
//            {
//                if (navValue == null)
//                {
//                    IQueryable query = Context.Entry(entity).Reference(navigation.PropertyInfo.Name).Query();
//                    navValue = await GetRelationLoaderQuery(query, navigationPropertyType: navigation.PropertyInfo.GetType())
//                        .FirstOrDefaultAsync();
//                    if (navValue == null)
//                        continue;
//                }

//                await setEntityAsSoftDeletedAsync((IEntityTimestamps)navValue);
//            }
//        }

//        Context.Update(entity);
//    }
//    protected IQueryable<object> GetRelationLoaderQuery(IQueryable query, Type navigationPropertyType)
//    {
//        Type queryProviderType = query.Provider.GetType();
//        MethodInfo createQueryMethod =
//            queryProviderType
//                .GetMethods()
//                .First(m => m is { Name: nameof(query.Provider.CreateQuery), IsGenericMethod: true })
//                ?.MakeGenericMethod(navigationPropertyType)
//            ?? throw new InvalidOperationException("CreateQuery<TElement> method is not found in IQueryProvider.");
//        var queryProviderQuery =
//            (IQueryable<object>)createQueryMethod.Invoke(query.Provider, parameters: new object[] { query.Expression })!;
//        return queryProviderQuery.Where(x => !((IEntityTimestamps)x).DeletedDate.HasValue);
//    }
//    protected async Task SetEntityAsDeletedAsync(IEnumerable<TEntitiy> entities, bool permanent)
//    {
//        foreach (TEntity entity in entities)
//            await SetEntityAsDeletedAsync(entity, permanent);
//    }
//}