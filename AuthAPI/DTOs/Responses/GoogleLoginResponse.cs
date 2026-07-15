namespace AuthAPI.DTOs.Responses;

public class GoogleLoginResponse
{
    public string GoogleId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PictureUrl { get; set; }
    public bool Verified { get; set; }
}