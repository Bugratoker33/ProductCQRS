using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Features.Product.Commands.Create;

public class CreatedProductResponse
{
    public Guid Id { get; set; }
    public string ProductName { get; set; }
    public DateTime CreatedDate { get; set; }
    public short? UnitsInStock { get; set; }
    public decimal? UnitPrice { get; set; }
}
