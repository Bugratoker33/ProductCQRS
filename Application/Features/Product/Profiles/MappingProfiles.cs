using Application.Features.Product.Commands.Create;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Product.Profiles;

public class MappingProfiles:Profile
{
    public MappingProfiles()
    {
        CreateMap<Domain.Entities.Product, CretedProductCommand>().ReverseMap();
        CreateMap<Domain.Entities.Product, CreatedProductResponse>().ReverseMap();


    }
}
