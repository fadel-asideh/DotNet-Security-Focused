using DotNetSecurityFocused.Models.DTOs;
using DotNetSecurityFocused.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using DotNetSecurityFocused.Data;
using FluentValidation;
using Microsoft.AspNetCore.RateLimiting;

namespace DotNetSecurityFocused.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly AppDBContext _appDbContext;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, SignInManager<ApplicationUser> signInManager, IValidator<RegisterRequest> registerValidator, AppDBContext appDBContext, ILogger<AuthController> logger, IConfiguration configuration)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
        _registerValidator = registerValidator;
        _appDbContext = appDBContext;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validations
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        // validate roles exist
        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
                return BadRequest(new { message = $"Role '{role}' does not exist" });
        }
        await using var transaction = await _appDbContext.Database.BeginTransactionAsync();
        try
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                await transaction.RollbackAsync();
                return BadRequest(result.Errors);
            }
            foreach(string role in request.Roles)
                await _userManager.AddToRoleAsync(user, role);
            
            await transaction.CommitAsync();
            return Ok(new { message = "User registered successfully" });
        }
        catch(Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Registration failed for {Email}", request.Email);
            throw;
        }
        
    }

    [HttpPost("login")]
    [EnableRateLimiting("ip-sliding")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        var result = await _signInManager.PasswordSignInAsync(user, request.Password, false, false);
    
        if (!result.Succeeded)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }
        var token = await GenerateJwtToken(user);
        return Ok(new { token = token });
    }

    private async Task<string> GenerateJwtToken(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}