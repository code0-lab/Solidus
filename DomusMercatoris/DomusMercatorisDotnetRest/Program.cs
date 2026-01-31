using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization;
using DomusMercatoris.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DomusMercatoris.Core.Repositories;
using DomusMercatoris.Data.Repositories;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.Mappings;
using DomusMercatoris.Service.Interfaces;
using DomusMercatorisDotnetRest.Infrastructure;
using DomusMercatorisDotnetRest.Services;
using DomusMercatorisDotnetRest.Hubs;
using DomusMercatorisDotnetRest.Authentication;
using Microsoft.AspNetCore.Authentication;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(cfg => 
{
    cfg.AddProfile<MappingProfile>();
    cfg.AddProfile<ApiMappingProfile>();
});

// Repositories
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<ICommentRepository, CommentRepository>();

// Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<BrandService>();
builder.Services.AddScoped<VariantProductService>();
builder.Services.AddScoped<CargoService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CompanyService>();
builder.Services.AddScoped<OrdersService>();
builder.Services.AddScoped<ModeratorService>();
builder.Services.AddScoped<UsersService>();
builder.Services.AddScoped<BannerService>();
builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<RefundService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<BlacklistService>();

// Python AI Service
builder.Services.AddSingleton<MockBankInfo>();
builder.Services.AddHostedService<PythonRunnerService>();
builder.Services.AddHostedService<MockBankRunnerService>();

// Common Services
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddScoped<CompanySettingsService>();
builder.Services.AddHttpClient<IGeminiCommentService, GeminiCommentService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    .AddJsonOptions(x => 
    {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddDbContext<DomusDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddDomusSwagger();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.Services.AddJwtAuth(builder.Configuration);
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationHandler.SchemeName, null);

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Default API");
    });
    
    // Separate Swagger UI for API Key
    app.UseSwaggerUI(c =>
    {
        c.RoutePrefix = "apikey-docs";
        c.SwaggerEndpoint("/swagger/apikey/swagger.json", "API Key Access");
    });

    app.Lifetime.ApplicationStarted.Register(() =>
    {
        Console.WriteLine("--------------------------------------------------");
        foreach (var url in app.Urls)
        {
            try
            {
                var uri = new Uri(url);
                var host = uri.Host == "0.0.0.0" ? "localhost" : uri.Host;
                var swaggerUrl = $"{uri.Scheme}://{host}:{uri.Port}/swagger";
                var apiKeySwaggerUrl = $"{uri.Scheme}://{host}:{uri.Port}/apikey-docs";
                Console.WriteLine($"Swagger        : {swaggerUrl}");
                Console.WriteLine($"API Key Swagger: {apiKeySwaggerUrl}");
            }
            catch
            {
                Console.WriteLine($"Swagger        : {url.TrimEnd('/')}/swagger");
                Console.WriteLine($"API Key Swagger: {url.TrimEnd('/')}/apikey-docs");
            }
            Console.WriteLine("--------------------------------------------------");
        }
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

// Serve static files from local Resources (e.g. placeholder)
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.ContentRootPath, "Resources")),
    RequestPath = "/Resources"
});

// Serve uploaded files directly from the MVC project's wwwroot/uploads directory
// This avoids copying files and ensures the API serves the single source of truth
var mvcUploadsPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "../MVC/MVC/wwwroot/uploads"));
if (Directory.Exists(mvcUploadsPath))
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(mvcUploadsPath),
        RequestPath = "/uploads"
    });
}
else 
{
    Console.WriteLine($"WARNING: MVC Uploads directory not found at: {mvcUploadsPath}");
}

app.UseAuthentication();

// Custom middleware to support API Key authentication alongside JWT or for public endpoints
app.Use(async (context, next) =>
{
    if (context.Request.Headers.ContainsKey(ApiKeyAuthenticationHandler.HeaderName))
    {
        var result = await context.AuthenticateAsync(ApiKeyAuthenticationHandler.SchemeName);
        if (result.Succeeded && result.Principal != null)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // If already authenticated (e.g. JWT), add the API Key identity/claims
                if (result.Principal.Identity is ClaimsIdentity apiIdentity)
                {
                    context.User.AddIdentity(apiIdentity);
                }
            }
            else
            {
                // If not authenticated, set the API Key principal
                context.User = result.Principal;
            }
        }
    }
    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<PaymentHub>("/paymentHub");

app.Run();
