using Domain.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
namespace Domain.Entities;

public class Product : Entitiy<Guid>
{
   
    public int? CategoryId { get; set; }
    public string ProductName { get; set; }
    public short? UnitsInStock { get; set; }
    public decimal? UnitPrice { get; set; }

    public Product()
    {
        
    }

    public Product(int? categoryId, string productName, short? unitsInStock, decimal? unitPrice)
    {
        CategoryId = categoryId;
        ProductName = productName;
        UnitsInStock = unitsInStock;
        UnitPrice = unitPrice;
    }
}
