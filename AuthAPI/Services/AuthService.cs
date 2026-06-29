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

namespace AuthAPI.Services;

public interface IAuthService
{
    Task<TokenResponse> Register(RegisterRequest request);
    Task<TokenResponse> Login(LoginRequest request);
    Task<TokenResponse> RefreshToken(RefreshTokenRequest request);
}

public class AuthService(AppDbContext context, IConfiguration config) : IAuthService
{
    private readonly AppDbContext _context = context;
    private readonly IConfiguration _config = config;

    public async Task<TokenResponse> Register(RegisterRequest request)
    {
        // Check Email Exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };

        // Save User
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await _context.SaveChangesAsync();

        return tokenResponse;
    }

    public async Task<TokenResponse> Login(LoginRequest request)
    {
        // Find Email
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user is null)
            throw new Exception("Invalid Email or Password");

        // Verify Password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid Email or Password");

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await _context.SaveChangesAsync();

        return tokenResponse;
    }

    public async Task<TokenResponse> RefreshToken(RefreshTokenRequest request)
    {
        // Find Refresh Token
        var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
        if (user is null)
            throw new Exception("Invalid Refresh Token");

        // Check Expiry
        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new Exception("Refresh Token Expired");

        // Generate Token Response
        var tokenResponse = GenerateTokenResponse(user);

        user.RefreshToken = tokenResponse.RefreshToken;
        user.RefreshTokenExpiry = GetRefreshTokenExpiry();
        await _context.SaveChangesAsync();

        return tokenResponse;
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

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
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
        return DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:AccessTokenExpiryMinutes"]!));
    }

    private DateTime GetRefreshTokenExpiry()
    {
        return DateTime.UtcNow.AddDays(double.Parse(_config["Jwt:RefreshTokenExpiryDays"]!));
    }
}