using INFP_Proj.Data;
using INFP_Proj.Model;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));
builder.Services.AddScoped<VitalsChartService>();
builder.Services.AddScoped<AdminLogService>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddDbContext<AuthLogin>();
builder.Services.AddIdentity<AppUser, IdentityRole>().AddEntityFrameworkStores<AuthLogin>();
builder.Services.AddRazorPages();


builder.Services.ConfigureApplicationCookie(Config =>
{
    Config.LoginPath = "/Login";
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    AppDbSeeder.Seed(services);
}

app.Run();