using AuthAPI.Commons.Constrants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    // ======================= Example Protected Routes =================

    private static readonly string[] Value = ["Product 1", "Product 2", "Product 3"];

    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok(new
        {
            message = "You have access!",
            products = Value
        });
    }

    [HttpGet("admin-only")]
    [Authorize(Roles = UserRoles.Admin)]
    public IActionResult GetAdminData()
    {
        return Ok(new { message = "Admin Only!" });
    }
}
