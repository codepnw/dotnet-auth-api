namespace AuthAPI.DTOs.Requests;

public class GoogleLoginRequest
{
    public string IdToken { get; set; } = string.Empty;
}