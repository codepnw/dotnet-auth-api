using AuthAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AuthAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
}
