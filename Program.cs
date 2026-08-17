using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using AsusLaptop.Data;
using AsusLaptop.Services; // ← THÊM DÒNG NÀY
using AsusLaptop.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<AsusLaptop.Services.NotificationService>();
builder.Services.AddHostedService<AsusLaptop.Services.OrderAutoCancelService>();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<AsusLaptop.Services.RoboflowService>();

// ── ĐĂNG KÝ VNPAY SERVICE ──────────────────────────────────────────────────
builder.Services.AddScoped<VnPayService>();
builder.Services.AddScoped<AsusLaptop.Services.MomoService>();
// ── ĐĂNG KÝ EMAIL SERVICE ─────────────────────────────────────────────────
builder.Services.AddScoped<AsusLaptop.Services.EmailService>();
builder.Services.AddScoped<AsusLaptop.Services.PersonalizedRecommendationService>();
builder.Services.AddScoped<AsusLaptop.Services.LaptopCopilotService>();
builder.Services.AddScoped<AsusLaptop.Services.ProductDescriptionAiService>();
builder.Services.AddScoped<AsusLaptop.Services.ZaloAiTtsService>();
builder.Services.AddScoped<AsusLaptop.Services.FutureFitAiService>();
builder.Services.AddScoped<AsusLaptop.Services.ProductAutoFillAiService>();
builder.Services.AddSingleton<AsusLaptop.Services.WebsiteAutomationStore>();
builder.Services.AddScoped<AsusLaptop.Services.FlashSaleEngine>();
builder.Services.AddScoped<AsusLaptop.Services.WebsiteAutomationRunner>();
builder.Services.AddHostedService<AsusLaptop.Services.WebsiteAutomationService>();
// ──────────────────────────────────────────────────────────────────────────

// ── ĐĂNG KÝ IMAGE OPTIMIZATION SERVICE & OUTPUT CACHING ───────────────────
builder.Services.AddScoped<AsusLaptop.Services.ImageOptimizationService>();
builder.Services.AddResponseCaching();
builder.Services.AddOutputCache(options =>
{
    options.AddBasePolicy(b => b.Expire(TimeSpan.FromSeconds(30)));
    options.AddPolicy("CatalogCache", b => b.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("search", "series", "brand", "minPrice", "maxPrice", "sort", "page"));
    options.AddPolicy("FastReadPolicy", b => b.Expire(TimeSpan.FromSeconds(120)));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=.;Database=AsusLaptopDB;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// ===== AUTHENTICATION: Cookie + Google + Facebook =====
var authBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

var googleClientId = builder.Configuration["Google:ClientId"] ?? "";
var googleClientSecret = builder.Configuration["Google:ClientSecret"] ?? "";
if (!string.IsNullOrEmpty(googleClientId))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

var facebookAppId = builder.Configuration["Facebook:AppId"] ?? "";
var facebookAppSecret = builder.Configuration["Facebook:AppSecret"] ?? "";
if (!string.IsNullOrEmpty(facebookAppId))
{
    authBuilder.AddFacebook(options =>
    {
        options.AppId = facebookAppId;
        options.AppSecret = facebookAppSecret;
        options.CallbackPath = "/signin-facebook";
        options.Scope.Clear();
        options.Scope.Add("public_profile");
       
        options.SaveTokens = true;
        options.Events.OnRemoteFailure = ctx =>
        {
            ctx.Response.Redirect("/Account/Login");
            ctx.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== RATE LIMITING =====
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Policy Đăng nhập (tối đa 5 lần / phút theo IP)
    options.AddFixedWindowLimiter("login-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 5;
        opt.QueueLimit = 0;
    });

    // Policy OTP (tối đa 3 lần / phút theo IP)
    options.AddFixedWindowLimiter("otp-policy", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 3;
        opt.QueueLimit = 0;
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor |
                       Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");
    }
});
app.UseRouting();
app.UseResponseCaching();
app.UseOutputCache();
app.UseRateLimiter();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<OrderTrackingHub>("/hubs/order-tracking");
app.MapHub<NotificationHub>("/hubs/notifications");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        DbInitializer.EnsureTablesCreated(context);
        context.Database.EnsureCreated();
        DbInitializer.Initialize(context);
        DbInitializer.SeedVariants(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the DB.");
    }
}

app.Run();
