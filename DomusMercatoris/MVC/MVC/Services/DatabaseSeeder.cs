using System;
using System.Collections.Generic;
using System.Linq;
using BCrypt.Net;
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
            var rexPasswordHash = BCrypt.Net.BCrypt.HashPassword("rex123");

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
            var moderatorPasswordHash = BCrypt.Net.BCrypt.HashPassword("moderator123");
            
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
            var managerPasswordHash = BCrypt.Net.BCrypt.HashPassword("manager123");
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
            var workerPasswordHash = BCrypt.Net.BCrypt.HashPassword("worker123");
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
            var testManagerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1A");
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
            var testWorkerPasswordHash = BCrypt.Net.BCrypt.HashPassword("Password1A");
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
        }
    }
}

