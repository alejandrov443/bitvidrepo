using BitVid11.Data;
using BitVid11.Hubs;
using BitVid11.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var stripeSettings = builder.Configuration.GetSection("Stripe").Get<StripeSettings>();
StripeConfiguration.ApiKey = stripeSettings.SecretKey;
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));
builder.Services.AddSingleton<StripeSettings>(sp => sp.GetRequiredService<IOptions<StripeSettings>>().Value);

// Add DbContext for MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    // Protect all pages by default
    options.Conventions.AuthorizeFolder("/");

    // Allow anonymous access to these pages
    options.Conventions.AllowAnonymousToPage("/Accounts/Login");
    options.Conventions.AllowAnonymousToPage("/Accounts/Register");
    options.Conventions.AllowAnonymousToPage("/Index");
    options.Conventions.AllowAnonymousToPage("/Accounts/Logout");   // optional
    options.Conventions.AllowAnonymousToPage("/Chat/Chat");
    options.Conventions.AllowAnonymousToPage("/Chat/Tiles");
});

// SignalR
builder.Services.AddSignalR();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Accounts/Login";        // Redirect if not authenticated
        options.AccessDeniedPath = "/AccessDenied";
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10000);
    options.Cookie.IsEssential = true;
});

// Form and request limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10L * 1024 * 1024 * 1024; // 10GB
});

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 10L * 1024 * 1024 * 1024; // 10GB
});

builder.Services.AddSingleton<ImageWorker2>();
builder.Services.AddHostedService<MyBackgroundService>();



builder.Services.AddHostedService<StartupTask>();

var app = builder.Build();


app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/Index");
        return;
    }
    await next();
});

app.UseCors("AllowAll");

// Serve static files
app.UseStaticFiles();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseRouting();

// Session & middleware
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and Razor pages
app.MapControllers();
app.MapRazorPages();

// Map SignalR hub
app.MapHub<ChatHub>("/chatHub");

//GitBashLauncher.StartGitBash();

app.Run();
