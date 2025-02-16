using Application.Services.Repositories;
using AutoMapper;
using Domain.Entities;
using MediatR;



namespace Application.Features.Product.Commands.Create;

public class CretedProductCommand:IRequest<CreatedProductResponse>
{
    public string  ProductName { get; set; }
    public int? CategoryId { get; set; }
    public short? UnitsInStock { get; set; }
    public decimal? UnitPrice { get; set; }

    public class CretedProductCommandHandler : IRequestHandler<CretedProductCommand, CreatedProductResponse>
    {
        private readonly IProductRepository _productRepositori;
        private readonly IMapper _mapper;

        public CretedProductCommandHandler(IProductRepository productRepositori, IMapper mapper)
        {
            _productRepositori = productRepositori;
            _mapper = mapper;
        }

        public async Task<CreatedProductResponse> Handle(CretedProductCommand request, CancellationToken cancellationToken)
        {
         Domain.Entities.Product product = _mapper.Map<Domain.Entities.Product>(request);
            product.Id= Guid.NewGuid();
            var result = await _productRepositori.AddAsync(product);
            CreatedProductResponse createdProductResponse = _mapper.Map<CreatedProductResponse>(result);
            return createdProductResponse;

        }
    }
}
