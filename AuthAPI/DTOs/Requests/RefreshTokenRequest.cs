using System;

namespace AuthAPI.DTOs.Requests;

public class RefreshTokenRequest
{
    public required string RefreshToken { get; set; }
}
