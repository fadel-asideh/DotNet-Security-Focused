using DotNetSecurityFocused.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSecurityFocused.Controllers;

[ApiController]
[Route("[Controller]")]
public class ProductsController : ControllerBase
{

    private readonly ProductSearchService _productSearchService;

    public ProductsController(ProductSearchService productSearchService)
    {
        _productSearchService = productSearchService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name)
    {
        var results = await _productSearchService.SearchByNameSafeAsync(name);
        return Ok(results);
    }
}