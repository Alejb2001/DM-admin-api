using DmAdminApi.Common.Extensions;
using DmAdminApi.Common.Middleware;
using DmAdminApi.Features.Hubs;
using DmAdminApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Load local overrides (gitignored — contains real secrets for local dev)
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddOpenApi();
    builder.Services.AddSignalR();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddApplicationServices(builder.Configuration);
    builder.Services.AddCorsPolicy(builder.Configuration);

    // Set Stripe API key globally at startup
    Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

    var app = builder.Build();

    // Auto-run pending migrations on startup
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "DM Admin API";
            options.Theme = ScalarTheme.DeepSpace;
        });
    }

    app.UseSerilogRequestLogging();
    app.UseCors("DefaultPolicy");
    app.UseAuthentication();
    app.UseAuthorization();
    // app.UseMiddleware<FeatureGatingMiddleware>(); // Desactivado: app gratuita por ahora
    app.MapControllers();

    app.MapHub<CampaignHub>("/hubs/campaign");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
