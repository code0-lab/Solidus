using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using DomusMercatoris.Data;
using DomusMercatoris.Core.Entities;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Service.Services; // For EncryptionService
using AutoMapper;
using Moq; 
using Microsoft.Extensions.Configuration; // Add this

namespace MVC.Tests;

public class UserDeletionTests
{
    private DomusDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<DomusDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test
            .Options;
        return new DomusDbContext(options);
    }
    
    private EncryptionService GetEncryptionService()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["EncryptionKey"]).Returns("12345678901234567890123456789012");
        return new EncryptionService(mockConfig.Object);
    }

    [Fact]
    public async Task DeleteUser_WithRexRole_ShouldFail()
    {
        // 1. Setup
        var db = GetInMemoryDbContext();
        var encryptionService = GetEncryptionService();
        var mapperMock = new Mock<IMapper>();
        
        var userService = new UserService(db, mapperMock.Object, encryptionService);

        var companyId = 1;
        var rexUser = new User
        {
            Id = 10,
            FirstName = "Rex",
            LastName = "Test",
            Email = "rex.test@domus.com",
            CompanyId = companyId,
            Roles = new List<string> { "Rex" }
        };
        db.Users.Add(rexUser);
        await db.SaveChangesAsync();

        // 2. Act
        var result = await userService.DeleteUserInCompanyAsync(rexUser.Id, companyId);

        // 3. Assert
        Assert.False(result, "Should fail to delete user with Rex role");
        var userInDb = await db.Users.FindAsync(rexUser.Id);
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task DeleteUser_WithSuperAdminEmail_ShouldFail_EvenWithoutRole()
    {
        // 1. Setup
        var db = GetInMemoryDbContext();
        var encryptionService = GetEncryptionService();
        var mapperMock = new Mock<IMapper>();
        
        var userService = new UserService(db, mapperMock.Object, encryptionService);

        var companyId = 1;
        // User with Rex email but NO "Rex" role
        var targetUser = new User
        {
            Id = 11,
            FirstName = "Rex",
            LastName = "ByEmail",
            Email = "rex@domus.com", // Hardcoded sensitive email
            CompanyId = companyId,
            Roles = new List<string> { "User" } // Standard role
        };
        db.Users.Add(targetUser);
        await db.SaveChangesAsync();

        // 2. Act
        var result = await userService.DeleteUserInCompanyAsync(targetUser.Id, companyId);

        // 3. Assert
        Assert.False(result, "Should fail to delete user with rex@domus.com email");
        var userInDb = await db.Users.FindAsync(targetUser.Id);
        Assert.NotNull(userInDb);
    }

    [Fact]
    public async Task DeleteUser_WithPageAccess_ShouldDeleteBoth()
    {
        // 1. Setup
        var db = GetInMemoryDbContext();
        var encryptionService = GetEncryptionService();
        var mapperMock = new Mock<IMapper>();
        
        var userService = new UserService(db, mapperMock.Object, encryptionService);

        var companyId = 1;
        var userWithAccess = new User
        {
            Id = 13,
            FirstName = "Access",
            LastName = "User",
            Email = "access@domus.com",
            CompanyId = companyId,
            Roles = new List<string> { "User" }
        };
        db.Users.Add(userWithAccess);

        var access = new UserPageAccess
        {
            UserId = userWithAccess.Id,
            CompanyId = companyId,
            PageKey = "Categories"
        };
        db.UserPageAccesses.Add(access);
        await db.SaveChangesAsync();

        // 2. Act
        var result = await userService.DeleteUserInCompanyAsync(userWithAccess.Id, companyId);

        // 3. Assert
        Assert.True(result, "Should succeed to delete user with page access");
        var userInDb = await db.Users.FindAsync(userWithAccess.Id);
        Assert.Null(userInDb);
        
        var accessInDb = await db.UserPageAccesses.FirstOrDefaultAsync(a => a.UserId == userWithAccess.Id);
        Assert.Null(accessInDb);
    }
}
