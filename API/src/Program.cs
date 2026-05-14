using Amazon.SQS;
using Amazon.Extensions.NETCore.Setup;
using API.Extensions;
using API.Middleware;
using API.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{

    var builder = WebApplication.CreateBuilder(args);
    var allowedOrigins = (builder.Configuration["Cors:AllowedOrigins"] ?? string.Empty)
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddControllers();

    builder.Services.AddCustomServices();
    builder.Services.ConfigureAuthentication(builder.Configuration);
    builder.Services.ConfigureCors(builder.Configuration);
    builder.Services.ConfigureDatabase(builder.Configuration);
    builder.Services.ConfigureSwagger();

    builder.Services.AddDistributedMemoryCache();
    builder.Services.AddSession(options =>
    {
        options.IdleTimeout = TimeSpan.FromMinutes(30);
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAuthorization();

    var awsOptions = builder.Configuration.GetAWSOptions();
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<IAmazonSQS>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        try
        {
            // Use EnsureCreated instead of Migrate to avoid SQLite/PostgreSQL migration conflicts
            // This creates the database schema based on the current DbContext snapshot
            Log.Information("Ensuring database and tables exist...");
            bool created = dbContext.Database.EnsureCreated();
            
            if (created)
            {
                Log.Information("Database and tables created successfully.");
            }
            else
            {
                Log.Information("Database already exists. Verifying tables...");
            }

            // Verify critical tables exist by querying each one
            try
            {
                _ = dbContext.Products.AsNoTracking().FirstOrDefault();
                _ = dbContext.Orders.AsNoTracking().FirstOrDefault();
                _ = dbContext.Carts.AsNoTracking().FirstOrDefault();
                Log.Information("All critical tables verified successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Table verification failed: {Message}", ex.Message);
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database setup failed. Exception type: {ExceptionType}", ex.GetType().Name);
            throw;
        }
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers.Origin.ToString();
        var isAllowedOrigin = allowedOrigins.Count == 0 || (!string.IsNullOrWhiteSpace(origin) && allowedOrigins.Contains(origin));

        if (isAllowedOrigin && !string.IsNullOrWhiteSpace(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Vary"] = "Origin";
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type, X-Requested-With";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, PATCH, DELETE, OPTIONS";
        }

        if (HttpMethods.IsOptions(context.Request.Method))
        {
            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return;
        }

        await next();
    });

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("FrontendCors");
    app.UseSession();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.Run();

}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}