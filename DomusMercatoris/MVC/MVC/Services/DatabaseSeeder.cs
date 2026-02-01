using System;
using System.Collections.Generic;
using System.Linq;
using DomusMercatoris.Core.Entities;
using DomusMercatoris.Data;

namespace DomusMercatorisDotnetMVC.Services
{
    public static class DatabaseSeeder
    {
        public static void Seed(DomusDbContext db)
        {
            if (!db.Companies.Any())
            {
                db.Companies.Add(new Company
                {
                    Name = "Domus Mercatoris",
                    CreatedAt = DateTime.Now
                });
                db.SaveChanges();
            }

            var defaultCompanyId = db.Companies.First().CompanyId;

            var rexEmail = "rex@domus.com";
            var rexUser = db.Users.FirstOrDefault(u => u.Email == rexEmail);
            var rexPasswordHash = HashSha256("RexPassword1!");

            if (rexUser == null)
            {
                rexUser = new User
                {
                    Email = rexEmail,
                    FirstName = "Rex",
                    LastName = "User",
                    Password = rexPasswordHash,
                    CompanyId = null,
                    Roles = new List<string> { "Rex" },
                    CreatedAt = DateTime.Now,
                    Address = "Rex HQ",
                    Phone = "555-REX"
                };
                db.Users.Add(rexUser);
                db.SaveChanges();
            }
            else
            {
                rexUser.Password = rexPasswordHash;
                rexUser.CompanyId = null; // Ensure Rex has no company

                if (!rexUser.Roles.Contains("Rex"))
                {
                    rexUser.Roles.Add("Rex");
                }

                db.SaveChanges();
            }

            var moderatorEmail = "moderator@domus.com";
            var moderatorUser = db.Users.FirstOrDefault(u => u.Email == moderatorEmail);
            var moderatorPasswordHash = HashSha256("ModeratorPassword1!");
            
            if (moderatorUser == null)
            {
                moderatorUser = new User
                {
                    Email = moderatorEmail,
                    FirstName = "Moderator",
                    LastName = "User",
                    Password = moderatorPasswordHash,
                    CompanyId = null,
                    Roles = new List<string> { "Moderator" },
                    CreatedAt = DateTime.Now,
                    Address = "Moderator HQ",
                    Phone = "555-MOD"
                };
                db.Users.Add(moderatorUser);
                db.SaveChanges();
            }
            else
            {
                moderatorUser.Password = moderatorPasswordHash;
                moderatorUser.CompanyId = null; // Ensure Moderator has no company
                
                if (!moderatorUser.Roles.Contains("Moderator"))
                {
                    moderatorUser.Roles.Add("Moderator");
                }
                
                db.SaveChanges();
            }

            var managerEmail = "manager@domus.com";
            var managerUser = db.Users.FirstOrDefault(u => u.Email == managerEmail);
            var managerPasswordHash = HashSha256("ManagerPassword1!");
            if (managerUser == null)
            {
                managerUser = new User
                {
                    Email = managerEmail,
                    FirstName = "Manager",
                    LastName = "User",
                    Password = managerPasswordHash,
                    CompanyId = defaultCompanyId,
                    Roles = new List<string> { "Manager", "User" },
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(managerUser);
                db.SaveChanges();
            }
            else
            {
                managerUser.Password = managerPasswordHash;
                if (!managerUser.Roles.Contains("Manager")) managerUser.Roles.Add("Manager");
                if (!managerUser.Roles.Contains("User")) managerUser.Roles.Add("User");
                db.SaveChanges();
            }

            var workerEmail = "worker@domus.com";
            var workerUser = db.Users.FirstOrDefault(u => u.Email == workerEmail);
            var workerPasswordHash = HashSha256("WorkerPassword1!");
            if (workerUser == null)
            {
                workerUser = new User
                {
                    Email = workerEmail,
                    FirstName = "Worker",
                    LastName = "User",
                    Password = workerPasswordHash,
                    CompanyId = defaultCompanyId,
                    Roles = new List<string> { "User" },
                    CreatedAt = DateTime.Now
                };
                db.Users.Add(workerUser);
                db.SaveChanges();
            }
            else
            {
                workerUser.Password = workerPasswordHash;
                if (!workerUser.Roles.Contains("User")) workerUser.Roles.Add("User");
                db.SaveChanges();
            }

            var hasCategoriesAccess = db.UserPageAccesses.Any(a => a.CompanyId == defaultCompanyId && a.UserId == workerUser.Id && a.PageKey == "Categories");
            if (!hasCategoriesAccess)
            {
                db.UserPageAccesses.Add(new UserPageAccess
                {
                    UserId = workerUser.Id,
                    CompanyId = defaultCompanyId,
                    PageKey = "Categories",
                    CreatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }

            var testManagerEmail = "auto.manager@example.com";
            var testManager = db.Users.FirstOrDefault(u => u.Email == testManagerEmail);
            var testManagerPasswordHash = HashSha256("AutoManagerPassword1!");
            if (testManager == null)
            {
                testManager = new User
                {
                    Email = testManagerEmail,
                    FirstName = "Auto",
                    LastName = "Manager",
                    Password = testManagerPasswordHash,
                    CompanyId = defaultCompanyId,
                    Roles = new List<string> { "Manager", "User", "Customer" },
                    CreatedAt = DateTime.Now,
                    Address = "Test Manager Address",
                    Phone = "555-TEST-MGR"
                };
                db.Users.Add(testManager);
            }
            else
            {
                testManager.Password = testManagerPasswordHash;
                if (testManager.Roles == null || !testManager.Roles.Contains("Manager"))
                {
                    testManager.Roles = new List<string> { "Manager", "User", "Customer" };
                }
            }
            db.SaveChanges();

            var testWorkerEmail = "auto.worker@example.com";
            var testWorker = db.Users.FirstOrDefault(u => u.Email == testWorkerEmail);
            var testWorkerPasswordHash = HashSha256("AutoWorkerPassword1!");
            if (testWorker == null)
            {
                testWorker = new User
                {
                    Email = testWorkerEmail,
                    FirstName = "Auto",
                    LastName = "Worker",
                    Password = testWorkerPasswordHash,
                    CompanyId = defaultCompanyId,
                    Roles = new List<string> { "User" },
                    CreatedAt = DateTime.Now,
                    Address = "Test Worker Address",
                    Phone = "555-TEST-WKR"
                };
                db.Users.Add(testWorker);
            }
            else
            {
                testWorker.Password = testWorkerPasswordHash;
            }
            db.SaveChanges();

            // Test User Seed
            var testUserEmail = "test@domus.com";
            var testUser = db.Users.FirstOrDefault(u => u.Email == testUserEmail);
            var testUserPasswordHash = HashSha256("TestUserPassword1!");
            if (testUser == null)
            {
                testUser = new User
                {
                    Email = testUserEmail,
                    FirstName = "Test",
                    LastName = "User",
                    Password = testUserPasswordHash,
                    CompanyId = null, // Regular customer
                    Roles = new List<string> { "Customer" },
                    CreatedAt = DateTime.Now,
                    Address = "Test Address",
                    Phone = "555-TEST"
                };
                db.Users.Add(testUser);
                db.SaveChanges();
            }
            else
            {
                testUser.Password = testUserPasswordHash;
                if (!testUser.Roles.Contains("Customer")) testUser.Roles.Add("Customer");
                db.SaveChanges();
            }

            // Banned User Seed
            var bannedUserEmail = "banned@domus.com";
            var bannedUser = db.Users.FirstOrDefault(u => u.Email == bannedUserEmail);
            var bannedUserPasswordHash = HashSha256("BannedUserPassword1!");
            if (bannedUser == null)
            {
                bannedUser = new User
                {
                    Email = bannedUserEmail,
                    FirstName = "Banned",
                    LastName = "User",
                    Password = bannedUserPasswordHash,
                    CompanyId = null,
                    Roles = new List<string> { "Baned" },
                    CreatedAt = DateTime.Now,
                    Address = "Banned Address",
                    Phone = "555-BAN"
                };
                db.Users.Add(bannedUser);
                db.SaveChanges();
            }
            else
            {
                bannedUser.Password = bannedUserPasswordHash;
                if (!bannedUser.Roles.Contains("Baned")) bannedUser.Roles.Add("Baned");
                db.SaveChanges();
            }
        }
        private static string HashSha256(string input)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            var builder = new System.Text.StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}

