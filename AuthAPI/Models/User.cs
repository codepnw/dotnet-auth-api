using System;
using AuthAPI.Commons.Constrants;

namespace AuthAPI.Models;

public class User
{
	public int Id { get; set; }
	public string Email { get; set; } = string.Empty;
	public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = UserRoles.User;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    
    // Google OAuth Fields
    public string? GoogleId { get; set; }
    public string? PictureUrl { get; set; }
    public string? DisplayName { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
}
