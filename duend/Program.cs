using duend;
using duend.Database;
using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Test;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System.Runtime.InteropServices;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
Log.Information("starting up");



var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var migrationsAssembly = typeof(Config).Assembly.GetName().Name;

builder.Services.AddAuthentication()
            .AddGoogle(options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.ClientId = "626294547203-tqjev6ib2gjmubkf1otkmuv49eqtv9f9.apps.googleusercontent.com";
                options.ClientSecret = "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";
            });


builder.Host.UseSerilog((ctx, lc) =>
{
    lc.MinimumLevel.Debug()
     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
      .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
      .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
      .MinimumLevel.Override("System", LogEventLevel.Warning)
      .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
        theme: AnsiConsoleTheme.Code)
      .Enrich.FromLogContext();
});




builder.Services.AddRazorPages();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqloptions => sqloptions.MigrationsAssembly(migrationsAssembly));
});
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    options.EmitStaticAudienceClaim = true;
})
    .AddConfigurationStore(options =>
{
    options.ConfigureDbContext = b =>
        b.UseSqlServer(connectionString, opt => opt.MigrationsAssembly(migrationsAssembly));

}).AddOperationalStore(options =>
{
    options.ConfigureDbContext = b => b.UseSqlServer(connectionString, opt => opt.MigrationsAssembly(migrationsAssembly));
})
    .AddAspNetIdentity<IdentityUser>();
var app = builder.Build();
app.UseIdentityServer();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages().RequireAuthorization();



if (args.Contains("/seed"))
{
    Log.Information("Seeding database");
    SeedData.EnsureSeedData(app);
    Log.Information("Done seeding database. Exiting.");
    return;
}


app.Run();
