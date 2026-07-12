using AuthAPI.DTOs.Requests;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AuthAPI.Commons;
using FluentValidation;

namespace AuthAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(
    IAuthService service,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshValidator
) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var validator = await registerValidator.ValidateAsync(request);
        if (!validator.IsValid)
        {
            var errors = validator.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(errors);
        }

        var result = await service.Register(request);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.ErrorCode, result.ErrorMessage!);

        return Created("", result.Data);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var validator = await loginValidator.ValidateAsync(request);
        if (!validator.IsValid)
        {
            var errors = validator.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(errors);
        }
        
        var result = await service.Login(request);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.ErrorCode, result.ErrorMessage!);

        return Ok(result.Data);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var validator = await refreshValidator.ValidateAsync(request);
        if (!validator.IsValid)
        {
            var errors = validator.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            return BadRequest(errors);
        }
        
        var result = await service.RefreshToken(request);
        if (!result.IsSuccess)
            return MapErrorToResponse(result.ErrorCode, result.ErrorMessage!);

        return Ok(result.Data);
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var claims = User.Claims.Select(c => new
        {
            type = c.Type,
            value = c.Value
        });

        return Ok(new
        {
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = User.FindFirst(ClaimTypes.Email)?.Value,
            role = User.FindFirst(ClaimTypes.Role)?.Value,
            claims
        });
    }

    public IActionResult MapErrorToResponse(ErrorCode errorCode, string errorMessage)
    {
        return errorCode switch
        {
            ErrorCode.BadRequest => BadRequest(new { message = errorMessage }),
            ErrorCode.NotFound => NotFound(new { message = errorMessage }),
            ErrorCode.Unauthorized => Unauthorized(new { message = errorMessage }),
            ErrorCode.Forbidden => StatusCode(403, new { message = errorMessage }),
            ErrorCode.Conflict => Conflict(new { message = errorMessage }),
            _ => StatusCode(500, new { message = "An unexpected error occurred" })
        };
    }
}