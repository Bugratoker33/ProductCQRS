
using Domain.Dynamic;
using Domain.Paging;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Repositories;

public interface IAsyncRepository<TEntity, TEntityId> : IQuery<TEntity>
where TEntity : Entitiy<TEntityId>//brand verdim ve ayrıca veri tipinide belirtmek istiyorum  //entityden inherti edilecek ve Id si olmalı başka bir şey olmamlı 
{
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate, //landa ile sorgu yapmak için 
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null, // join yaaparak da data getirme işlemi 
        bool withDeleted = false,//silinilenleri sorguda getireyim mi getirmiyeyim mi demeke biz getirme diyoruz 
        bool enableTracking = true,//
        CancellationToken cancellationToken = default);


    Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    //
    );

    Task<Paginate<TEntity>> GetListByDynamicAsync(
        DynamicQuery dynamic,//getlist ie aynı ama biz ayrıyeten dynamic sorgu getiriyoruz arabanın rengine göre fiiltreleme gibi 
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default
    );

    Task<bool> AnyAsync(
       Expression<Func<TEntity, bool>>? predicate = null,
       bool withDeleted = false,
       bool enableTracking = true,
       CancellationToken cancellationToken = default
   );

    Task<TEntity> AddAsync(TEntity entity);

    Task<ICollection<TEntity>> AddRangeAsync(ICollection<TEntity> entities);//birden fazla entiity ver onun hepsini kaydedeyim 

    Task<TEntity> UpdateAsync(TEntity entity);

    Task<ICollection<TEntity>> UpdateRangeAsync(ICollection<TEntity> entities);

    Task<TEntity> DeleteAsync(TEntity entity, bool permanent = false);

    Task<ICollection<TEntity>> DeleteRangeAsync(ICollection<TEntity> entities, bool permanent = false);

}