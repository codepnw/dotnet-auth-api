using AuthAPI.Commons.Constrants;
using AuthAPI.Data;
using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthAPI.Tests.Helpers;

public static class TestDbContext
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return context;
    }

    public static AppDbContext CreateWithUsers()
    {
        var context = Create();

        context.Users.AddRange(
            new User
            {
                Id = 1,
                Email = "admin@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminTest!"),
                Role = UserRoles.Admin,
                RefreshToken = "valid-refresh-token",
                RefreshTokenExpiry = DateTime.UtcNow.AddDays(7)
            },
            new User
            {
                Id = 2,
                Email = "user@test.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("UserTest!"),
                Role = UserRoles.User,
                RefreshToken = null,
                RefreshTokenExpiry = null
            }
        );

        context.SaveChanges();
        return context;
    }
}