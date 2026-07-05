using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotNetSecurityFocused.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SecretsController : ControllerBase
{
    [HttpGet("admin")]
    [Authorize(Roles ="Admin")]
    public IActionResult AdminSecrets()
    {
        return Ok("You have Admin access.");
    }

    [HttpGet("user")]
    [Authorize(Roles ="User")]
    public IActionResult UserSecrets()
    {
        return Ok("You have User access.");
    }

    [HttpGet("manager")]
    [Authorize(Roles ="Manager")]
    public IActionResult ManagerSecrets()
    {
        return Ok("You have Manager access.");
    }

    [HttpGet("admin-or-manager")]
    [Authorize(Roles ="Admin,Manager")]
    public IActionResult AdminOrManager()
    {
        return Ok("You have Admin or Manager access.");
    }

    [HttpGet("admin-and-manager")]
    [Authorize(Roles ="Admin")]
    [Authorize(Roles ="Manager")]
    public IActionResult AdminAndManager()
    {
        return Ok("You have Admin and Manager access.");
    }

}