using AuthAPI.Commons;
using AuthAPI.Commons.Constrants;
using AuthAPI.DTOs.Requests;
using AuthAPI.DTOs.Responses;
using AuthAPI.Models;
using AuthAPI.Services;
using AuthAPI.Tests.Helpers;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using IConfiguration = Castle.Core.Configuration.IConfiguration;

namespace AuthAPI.Tests.Services;

public class AuthServiceTests
{
    private readonly IConfigurationRoot _config;
    private const string MockEmail = "admin@test.com";
    private const string MockPassword = "AdminTest!";

    public AuthServiceTests()
    {
        var configData = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "Example-JWT-Key-32CharactersLong",
            ["Jwt:Issuer"] = "AuthAPI",
            ["Jwt:Audience"] = "AuthAPIClients",
            ["Jwt:AccessTokenExpiryMinutes"] = "15",
            ["Jwt:RefreshTokenExpiryDays"] = "7",
            ["Google:ClientId"] = "test-client-id.apps.google.com",
            ["Google:ClientSecret"] = "test-client-secret"
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();
    }

    [Fact]
    public async Task Register_Success()
    {
        await using var context = TestDbContext.Create();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.Register(new RegisterRequest
        {
            Email = MockEmail,
            Password = MockPassword
        });

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();
        result.Data!.RefreshToken.Should().NotBeNullOrEmpty();

        var user = context.Users.FirstOrDefault(u => u.Email == MockEmail);
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRoles.User);
        user.RefreshToken.Should().Be(result.Data.RefreshToken);
    }

    // Private Method Generate Token
    [Fact]
    public async Task Register_Success_GenerateJWT()
    {
        await using var context = TestDbContext.Create();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.Register(new RegisterRequest
        {
            Email = "jwt@test.com",
            Password = "PasswordTest"
        });

        result.IsSuccess.Should().BeTrue();

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(result.Data!.AccessToken);

        jwtToken.Issuer.Should().Be("AuthAPI");
        jwtToken.Audiences.Should().Contain("AuthAPIClients");
    }

    [Fact]
    public async Task Register_Fail_DuplicateEmail()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.Register(new RegisterRequest
        {
            Email = MockEmail,
            Password = MockPassword
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Email already exists");
        result.ErrorCode.Should().Be(ErrorCode.Conflict);
    }

    [Fact]
    public async Task Login_Success()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.Login(new LoginRequest
        {
            Email = MockEmail,
            Password = MockPassword
        });

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_Fail_InvalidEmail()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.Login(new LoginRequest
        {
            Email = "invalidm@test.com",
            Password = MockPassword
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
    }

    [Fact]
    public async Task Login_Fail_WrongPassword()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var server = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await server.Login(new LoginRequest
        {
            Email = MockEmail,
            Password = "WrongPassword"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid email or password");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_Success()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var oldRefreshToken = "valid-refresh-token";

        var result = await service.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = oldRefreshToken
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.AccessToken.Should().NotBeNullOrEmpty();
        result.Data.RefreshToken.Should().NotBeNullOrEmpty();

        // NewRefreshToken != OldRefreshToken
        result.Data.RefreshToken.Should().NotBe(oldRefreshToken);

        // Check Db
        var user = context.Users.FirstOrDefault(u => u.Email == MockEmail);
        user!.RefreshToken.Should().Be(result.Data.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Fail_InvalidToken()
    {
        await using var context = TestDbContext.CreateWithUsers();
        var logger = NullLogger<AuthService>.Instance;
        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "invalid-refresh-token"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid refresh token");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_Fail_Expired()
    {
        await using var context = TestDbContext.Create();
        var logger = NullLogger<AuthService>.Instance;

        context.Users.Add(new User
        {
            Id = 1,
            Email = "expired@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("PasswordTest"),
            Role = UserRoles.User,
            RefreshToken = "expired-token",
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(-1)
        });
        await context.SaveChangesAsync();

        var service = new AuthService(context, _config, logger, Mock.Of<IOAuthService>());

        var result = await service.RefreshToken(new RefreshTokenRequest
        {
            RefreshToken = "expired-token"
        });

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Be("Refresh token expired");
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
    }

    [Fact]
    public async Task GoogleLogin_Success_NewUser()
    {
        // Arrange
        await using var context = TestDbContext.Create();
        var logger = NullLogger<AuthService>.Instance;
        var mockGoogleService = new Mock<IOAuthService>();
        mockGoogleService.Setup(x => x.GoogleVerifyToken(It.IsAny<string>()))
            .ReturnsAsync(new GoogleLoginResponse
            {
                GoogleId = "google-1",
                Email = "google@test.com",
                DisplayName = "Test User",
                PictureUrl = "",
                Verified = true
            });

        var service = new AuthService(context, _config, logger, mockGoogleService.Object);

        // Act
        var result = await service.GoogleLogin(new GoogleLoginRequest
        {
            IdToken = "valid-token"
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data!.AccessToken.Should().NotBeNullOrEmpty();

        var user = context.Users.FirstOrDefault(u => u.Email == "google@test.com");
        user.Should().NotBeNull();
        user!.GoogleId.Should().Be("google-1");
    }

    [Fact]
    public async Task GoogleLogin_Success_ExistUser()
    {
        // Arrange
        await using var context = TestDbContext.Create();
        context.Users.Add(new User
        {
            GoogleId = "exist-1",
            Email = "exist@test.com",
            DisplayName = "Exist User"
        });
        await context.SaveChangesAsync();

        var logger = NullLogger<AuthService>.Instance;

        var mockGoogleService = new Mock<IOAuthService>();
        mockGoogleService.Setup(x => x.GoogleVerifyToken(It.IsAny<string>()))
            .ReturnsAsync(new GoogleLoginResponse
            {
                GoogleId = "exist-1",
                Email = "exist@test.com",
                Verified = true
            });

        var service = new AuthService(context, _config, logger, mockGoogleService.Object);

        // Act
        var result = await service.GoogleLogin(new GoogleLoginRequest
        {
            IdToken = "valid-token"
        });

        // Assert
        result.IsSuccess.Should().BeTrue();
        context.Users.Count().Should().Be(1);
    }

    [Fact]
    public async Task GoogleLogin_Fail_InvalidToken()
    {
        // Arrange
        await using var context = TestDbContext.Create();
        var logger = NullLogger<AuthService>.Instance;
        var mockGoogleService = new Mock<IOAuthService>();
        mockGoogleService.Setup(x => x.GoogleVerifyToken(It.IsAny<string>()))
            .ReturnsAsync((GoogleLoginResponse?)null);

        var service = new AuthService(context, _config, logger, mockGoogleService.Object);
        
        // Act
        var result = await service.GoogleLogin(new GoogleLoginRequest
        {
            IdToken = "invalid-token"
        });
        
        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.BadRequest);
    }
}