using Microsoft.Extensions.FileProviders;
using UniversityDecisionHub.Api.Features.Reviews;
using Microsoft.EntityFrameworkCore;
using UniversityDecisionHub.Api.Data;
using UniversityDecisionHub.Api.Features.Authentication;
using UniversityDecisionHub.Api.Features.Users;
using UniversityDecisionHub.Api.Features.CatalogImport;

// File-only validation exits before configuration, authentication or database setup.
if (args.Length > 0 && args[0] == CatalogValidationCommand.Name)
{
    Environment.ExitCode = await CatalogValidationCommand.RunAsync(args);
    return;
}

// Import arguments belong to our CLI, not ASP.NET's configuration parser.
var builder = WebApplication.CreateBuilder(
    args.Length > 0 && args[0] == CatalogImportCommand.Name ? Array.Empty<string>() : args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Developer command: do not start HTTP/authentication or apply migrations implicitly.
if (args.Length > 0 && args[0] == CatalogImportCommand.Name)
{
    Environment.ExitCode = await CatalogImportCommand.RunAsync(args, connectionString);
    return;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IReviewSubmissionService, ReviewSubmissionService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Registration/login, JWT validation, the Administrator policy and the login rate limit.
builder.Services.AddUdhAuthentication(builder.Configuration, builder.Environment);

// The HTML frontend is served from another origin (for example VS Code Live Server).
// Logins travel in the Authorization header, not in cookies, so no credentials are allowed here.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (builder.Environment.IsDevelopment()) policy.AllowAnyOrigin();
    else policy.WithOrigins(allowedOrigins);
    policy.AllowAnyHeader().AllowAnyMethod();
}));

var app = builder.Build();

// One-off commands that run against the configured database and exit, instead of starting the web server:
//   dotnet run -- make-admin someone@example.com
//   dotnet run -- import-users "C:\path\outside\the\repository\users.csv"
if (args.Length == 2 && args[0] == AdminCommand.Name)
{
    Environment.ExitCode = await AdminCommand.RunAsync(app.Services, args[1]);
    return;
}
if (args.Length == 2 && args[0] == UserImportCommand.Name)
{
    Environment.ExitCode = await UserImportCommand.RunAsync(app.Services, args[1]);
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // In Development the frontend may call the plain-http port; a redirect would break its CORS preflight.
    app.UseHttpsRedirection();
}

var frontendPath = Path.GetFullPath(
    Path.Combine(
        app.Environment.ContentRootPath,
        "..",
        "..",
        "Frontend"
    )
);

if (!Directory.Exists(frontendPath))
{
    throw new DirectoryNotFoundException(
        $"Frontend directory was not found: {frontendPath}"
    );
}

using var frontendFiles = new PhysicalFileProvider(frontendPath);

app.UseDefaultFiles(new DefaultFilesOptions
{
    FileProvider = frontendFiles
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = frontendFiles
});

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
