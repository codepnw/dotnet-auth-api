using System;

namespace AuthAPI.Models;

public class User
{
	public int Id { get; set; }
	public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string Role { get; set; } = "User";
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
}
