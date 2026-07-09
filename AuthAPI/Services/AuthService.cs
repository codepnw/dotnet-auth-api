using AuthAPI.Data;
using AuthAPI.DTOs.Requests;
using AuthAPI.DTOs.Responses;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthAPI.Commons;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AuthAPI.Services;

public interface IAuthService
{
    Task<Result<TokenResponse>> Register(RegisterRequest request);
    Task<Result<TokenResponse>> Login(LoginRequest request);
    Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest request);
}

public class AuthService(AppDbContext context, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<TokenResponse>> Register(RegisterRequest request)
    {
        // Check Email Exists
        if (await context.Users.AnyAsync(u => u.Email == request.Email))
        {
            logger.LogWarning("Register Failed: {Email} already exists", request.Email);
            return Result<TokenResponse>.Failure("Email already exists", ErrorCode.Conflict);
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        // Save User
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await context.SaveChangesAsync();

        return Result<TokenResponse>.Success(tokenResponse);
    }

    public async Task<Result<TokenResponse>> Login(LoginRequest request)
    {
        // Find Email
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
        {
            logger.LogWarning("Login Failed: Email: {Email} not found", request.Email);
            return Result<TokenResponse>.Failure("Invalid email or password", ErrorCode.BadRequest);
        }

        // Verify Password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login Failed: Email: {Email} invalid password", request.Email);
            return Result<TokenResponse>.Failure("Invalid email or password", ErrorCode.BadRequest);
        }

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await context.SaveChangesAsync();

        return Result<TokenResponse>.Success(tokenResponse);
    }

    public async Task<Result<TokenResponse>> RefreshToken(RefreshTokenRequest request)
    {
        // Find Refresh Token
        var user = await context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
        if (user is null)
        {
            logger.LogWarning("Refresh Token Failed: {Token} invalid", request.RefreshToken);
            return Result<TokenResponse>.Failure("Invalid refresh token", ErrorCode.BadRequest);
        }

        // Check Expiry
        if (user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            logger.LogWarning("Refresh Token Failed: {Token} expired", request.RefreshToken);
            return Result<TokenResponse>.Failure("Refresh token expired", ErrorCode.BadRequest);
        }

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await context.SaveChangesAsync();

        return Result<TokenResponse>.Success(tokenResponse);
    }

    // =========================== PRIVATE ==============================

    private TokenResponse GenerateTokenResponse(User user)
    {
        var accessToken = GetAccessToken(user);
        var refreshToken = GetRefreshToken();

        return new TokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = GetAccessTokenExpiry()
        };
    }

    private string GetAccessToken(User user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: GetAccessTokenExpiry(),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GetRefreshToken()
    {
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        return Convert.ToBase64String(randomNumber);
    }

    private DateTime GetAccessTokenExpiry()
    {
        return DateTime.UtcNow.AddMinutes(double.Parse(config["Jwt:AccessTokenExpiryMinutes"]!));
    }

    private DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(double.Parse(config["Jwt:RefreshTokenExpiryDays"]!));
    }
}