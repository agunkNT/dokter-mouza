using NLog;
using NLog.Web;
using Microsoft.EntityFrameworkCore;


AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Debug("init main");


try
{
    var builder = WebApplication.CreateBuilder(args);

    // NLog: Setup NLog for Dependency injection
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Add services to the container.
    builder.Services.AddAuthentication("CookieAuth")
        .AddCookie("CookieAuth", options =>
        {
            options.Cookie.Name = "DokterAbdurrahman.Auth";
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
            
            // Security: Set Configurable Expiration
            var expireMinutes = builder.Configuration.GetValue<int>("AuthSettings:CookieExpirationMinutes");
            if(expireMinutes == 0) expireMinutes = 30; // Default fallback

            options.ExpireTimeSpan = TimeSpan.FromMinutes(expireMinutes);
            options.SlidingExpiration = true; // Refresh token if user is active
        });

    builder.Services.AddHttpClient(); // Often useful
    builder.Services.AddScoped<DokterAbdurrahman.Services.GoogleDriveService>();
    builder.Services.AddScoped<DokterAbdurrahman.Services.MediaCompressionService>();
    builder.Services.AddScoped<DokterAbdurrahman.Services.LocalMediaService>();
    
    builder.Services.AddControllersWithViews();
    builder.Services.AddMemoryCache();
    // Konfigurasi ukuran max upload untuk API media dari Flutter
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 209_715_200; // 200 MB
    });
    builder.Services.AddDbContext<DokterAbdurrahman.Data.AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorCodesToAdd: null);
        }));



var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DokterAbdurrahman.Data.AppDbContext>();
        DokterAbdurrahman.Data.DbInitializer.Initialize(context);
    }
    catch (Exception ex)
    {
        // Use the NLog logger defined at the top
        logger.Error(ex, "An error occurred creating the DB.");
    }
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();


app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


    app.Run();
}
catch (Exception exception)
{
    // NLog: catch setup errors
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}
