using AuthAPI.DTOs.Responses;
using Google.Apis.Auth;

namespace AuthAPI.Services;

public interface IOAuthService
{
    Task<GoogleLoginResponse?> GoogleVerifyToken(string idToken);
}

public class OAuthService(IConfiguration config, ILogger<OAuthService> logger) : IOAuthService
{
    public async Task<GoogleLoginResponse?> GoogleVerifyToken(string idToken)
    {
        try
        {
            var clientId = config["Google:ClientId"];

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [clientId]
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

            if (!payload.EmailVerified)
            {
                logger.LogWarning("Google login attempt with unverified email: {Email}", payload.Email);
                return null;
            }

            return new GoogleLoginResponse
            {
                GoogleId = payload.Subject,
                Email = payload.Email,
                DisplayName = payload.Name,
                PictureUrl = payload.Picture,
                Verified = payload.EmailVerified
            };
        }
        catch (InvalidJwtException e)
        {
            logger.LogError(e, "Invalid Google ID Token");
            return null;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error Verifying Google Token");
            return null;
        }
    }
}