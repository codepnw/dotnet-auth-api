using System;

namespace AuthAPI.DTOs.Responses;

public class TokenResponse
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime AccessTokenExpiry { get; set; }
}
