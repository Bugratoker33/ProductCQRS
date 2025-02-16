using Application.Features.Product.Commands.Create;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : BaseControler
    {
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] CretedProductCommand cretedProductCommand)
        {
            CreatedProductResponse response = await Mediator.Send(cretedProductCommand);

            return Ok(response);
        }
    }
}
