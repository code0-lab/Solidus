using System.Net;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using DomusMercatorisDotnetMVC.Dto.Mappings;
using DomusMercatorisDotnetMVC.Middleware;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Service.Services;

var builder = WebApplication.CreateBuilder(args);

// Persist authentication keys to survive app restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "dp_keys")))
    .SetApplicationName("DomusMercatoris");

// DbContext
var dbConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DomusMercatoris.Data.DomusDbContext>(option =>
{
    option.UseSqlServer(dbConn, b => b.MigrationsAssembly("DomusMercatoris.Data"));
    option.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

// AutoMapper
builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddProfile<AppProfile>();
    cfg.AddProfile<DomusMercatoris.Service.Mappings.MappingProfile>();
});

// Add services to the container.
builder.Services.AddRazorPages();

// Add DI
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<AsyncLogService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<VariantProductService>();
builder.Services.AddScoped<CargoService>();
builder.Services.AddScoped<DomusMercatorisDotnetMVC.Services.CommentService>();
builder.Services.AddScoped<DomusMercatorisDotnetMVC.Services.TaskService>();
builder.Services.AddScoped<DomusMercatoris.Service.Services.OrderService>();
builder.Services.AddScoped<DomusMercatoris.Service.Services.RefundService>();
builder.Services.AddHttpClient<GeminiService>();
builder.Services.AddScoped<GeminiService>();
builder.Services.AddScoped<BannerService>();
// Shared Services
builder.Services.AddScoped<CompanySettingsService>();
builder.Services.AddHttpClient<GeminiCommentService>();
builder.Services.AddScoped<GeminiCommentService>();
builder.Services.AddHttpClient<IClusteringService, ClusteringService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<MockBankInfo>();
builder.Services.AddHostedService<PythonRunnerService>();
builder.Services.AddHostedService<MockBankRunnerService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IAuthorizationHandler, HybridPageAccessHandler>();

// Session ve Cookies AddAuthorization
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/";
        options.Cookie.HttpOnly = true;
        //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });
builder.Services.AddSession();    

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CategoriesAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Categories", "Manager")));
    options.AddPolicy("ProductsAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Products", "Manager")));
    options.AddPolicy("BrandsAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Brands", "Manager")));
    options.AddPolicy("OrdersAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Orders", "Manager")));
    options.AddPolicy("CustomersAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Customers", "Manager")));
    options.AddPolicy("WorkersAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Workers", "Manager")));
    options.AddPolicy("CompanyBannersAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("CompanyBanners", "Manager")));
    options.AddPolicy("RefundsAccess", policy =>
        policy.Requirements.Add(new HybridPageAccessRequirement("Refunds", "Manager")));
});

var app = builder.Build();

app.UseMiddleware<ExceptionLoggingMiddleware>();

app.UseExceptionHandler("/Error");
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Ban Middleware must be AFTER Auth/Authorization (to know who the user is) 
// but BEFORE endpoints mapping.
app.UseMiddleware<BanEnforcementMiddleware>();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

// Ensure uploads directory exists
using (var scope0 = app.Services.CreateScope())
{
    var env = scope0.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
    var root = env.WebRootPath;
    if (string.IsNullOrEmpty(root))
    {
        root = Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }
    var uploadsDir = Path.Combine(root, "uploads", "products");
    Directory.CreateDirectory(uploadsDir);
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DomusMercatoris.Data.DomusDbContext>();
    var provider = db.Database.ProviderName;
    if (provider != null && provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        using var conn = db.Database.GetDbConnection();
        conn.Open();
        bool hasRolesColumn = false;
        bool hasCompanyIdColumn = false;
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info('Users')";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var colName = reader.GetString(1);
                if (string.Equals(colName, "Roles", StringComparison.OrdinalIgnoreCase))
                {
                    hasRolesColumn = true;
                }
                if (string.Equals(colName, "CompanyId", StringComparison.OrdinalIgnoreCase))
                {
                    hasCompanyIdColumn = true;
                }
            }
        }

        if (!hasRolesColumn)
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN Roles TEXT NOT NULL DEFAULT '[]';");
            db.Database.ExecuteSqlRaw("UPDATE Users SET Roles = '[]';");
        }

        if (!hasCompanyIdColumn)
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE Users ADD COLUMN CompanyId INTEGER NOT NULL DEFAULT 0;");
        }

        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS UserRoles;");
        db.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS Roles;");
    }
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DomusMercatoris.Data.DomusDbContext>();
    DomusMercatorisDotnetMVC.Services.DatabaseSeeder.Seed(db);
}

// Print Local Network URL on Startup
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
        .Where(i => i.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 || 
                    i.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet)
        .SelectMany(i => i.GetIPProperties().UnicastAddresses)
        .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && 
                    !System.Net.IPAddress.IsLoopback(a.Address))
        .Select(a => a.Address.ToString())
        .ToList();

    var port = app.Urls.FirstOrDefault()?.Split(':').Last() ?? "5147"; // Default to 5147 if not found

    Console.WriteLine("\n----------------------------------------------------------");
    Console.WriteLine($" Application Started! Access it via:");
    Console.WriteLine($" > Local:   http://localhost:{port}");
    foreach (var ip in addresses)
    {
        Console.WriteLine($" > Network: http://{ip}:{port}");
    }
    Console.WriteLine("----------------------------------------------------------\n");
});

app.Run();

public partial class Program { }
