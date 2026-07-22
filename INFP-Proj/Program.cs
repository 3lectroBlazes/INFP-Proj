using System.Security.Claims;
using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<VitalsChartService>();
builder.Services.AddScoped<VitalsSimulationService>();
builder.Services.AddScoped<AdminLogService>();
builder.Services.AddScoped<UserContextService>();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<AppUser>, AdminRestricts>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AuthConnectionString")));

builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
})
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
    .RequireAuthenticatedUser()
    .Build();

    options.AddPolicy("AdminOnlyPolicy", policy =>
        policy.RequireAssertion(context =>
        {
            Claim? isAdminClaim = context.User.FindFirst("IsAdmin");
            return isAdminClaim != null && bool.Parse(isAdminClaim.Value);
        }));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOnlyPolicy");
});

builder.Services.ConfigureApplicationCookie(config =>
{
    config.LoginPath = "/Login";
});

builder.Services.AddTransient<ISmsService, MockSmsService>();

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddTransient<IEmailService, SmtpEmailService>();
builder.Services.AddTransient<IOtpService, OtpService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<INotificationService, ConsoleNotificationService>();
builder.Services.AddScoped<VitalsAlertService>();

builder.Services.AddScoped<VitalsAlertService>();

WebApplication app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

// Seed the database
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider services = scope.ServiceProvider;
    await AppDbSeeder.SeedAsync(services);
}

app.Run();