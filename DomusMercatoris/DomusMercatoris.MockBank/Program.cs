using DomusMercatoris.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

// DbContext
var dbConn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<DomusDbContext>(option =>
{
    option.UseSqlServer(dbConn, b => b.MigrationsAssembly("DomusMercatoris.Data"));
    option.ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();

app.Run();
