using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using API.Services;
using API.Infrastructure;
using System.Linq;
using Npgsql;

namespace API.Extensions;

public static class ServiceExtensions
{
  public static void AddCustomServices(this IServiceCollection services)
  {
    // Scoped Services
    services.AddScoped<IPasswordHasherService, PasswordHasherService>();
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<IProductRepository, ProductRepository>();
    services.AddScoped<IOrderRepository, OrderRepository>();
    services.AddScoped<IUserCommandService, UserCommandService>();
    services.AddScoped<IUserQueryService, UserQueryService>();
    services.AddScoped<IProductCommandService, ProductCommandService>();
    services.AddScoped<IProductQueryService, ProductQueryService>();
    services.AddScoped<IOrderCommandService, OrderCommandService>();
    services.AddScoped<IOrderQueryService, OrderQueryService>();
    services.AddScoped<IPaymentService, MockPaymentService>();
    services.AddScoped<IAdminService, AdminService>();
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<ISessionService, SessionService>();
    services.AddScoped<IEmailService, ResendEmailService>();

    services.AddControllers()
      .AddJsonOptions(options =>
      {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
      });
  }

  public static void ConfigureAuthentication(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
      options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = configuration["Jwt:Issuer"],
        ValidAudience = configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]))
      };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
      options.LoginPath = "/account/login";
      options.AccessDeniedPath = "/account/access-denied";
      options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
      options.SlidingExpiration = true;
    });
  }

  public static void ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddDbContext<ApplicationDbContext>(options =>
    {
      var rawConnectionString = configuration.GetConnectionString("DefaultConnection")
        ?? configuration["ConnectionStrings__DefaultConnection"]
        ?? configuration["DATABASE_URL"];

      if (string.IsNullOrWhiteSpace(rawConnectionString))
      {
        throw new InvalidOperationException("Database connection string is missing. Set ConnectionStrings__DefaultConnection or DATABASE_URL.");
      }

      var connectionString = NormalizeConnectionString(rawConnectionString);
      options.UseNpgsql(connectionString);
    });
  }

  private static string NormalizeConnectionString(string rawConnectionString)
  {
    var value = rawConnectionString.Trim().Trim('"');

    // Railway env values can accidentally include line breaks or extra pasted lines.
    // Keep the first non-empty line/token so Npgsql receives only the actual connection string.
    if (value.Contains('\n') || value.Contains('\r'))
    {
      value = value
        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .FirstOrDefault() ?? value;
    }

    if (value.Contains(' '))
    {
      value = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? value;
    }

    if (!(value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
      || value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)))
    {
      return value;
    }

    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
    {
      return value;
    }

    var builder = new NpgsqlConnectionStringBuilder
    {
      Host = uri.Host,
      Port = uri.IsDefaultPort ? 5432 : uri.Port,
      Database = uri.AbsolutePath.Trim('/'),
      SslMode = SslMode.Require,
      TrustServerCertificate = true
    };

    if (!string.IsNullOrWhiteSpace(uri.UserInfo))
    {
      var authParts = uri.UserInfo.Split(':', 2);
      builder.Username = Uri.UnescapeDataString(authParts[0]);
      if (authParts.Length > 1)
      {
        builder.Password = Uri.UnescapeDataString(authParts[1]);
      }
    }

    var query = uri.Query.TrimStart('?');
    if (!string.IsNullOrWhiteSpace(query))
    {
      var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
      foreach (var pair in pairs)
      {
        var kv = pair.Split('=', 2);
        var key = Uri.UnescapeDataString(kv[0]).Trim();
        var val = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]).Trim() : string.Empty;

        if (key.Equals("sslmode", StringComparison.OrdinalIgnoreCase))
        {
          if (string.IsNullOrWhiteSpace(val))
          {
            val = "require";
          }

          if (Enum.TryParse<SslMode>(val, true, out var sslMode))
          {
            builder.SslMode = sslMode;
          }
        }
      }
    }

    return builder.ConnectionString;
  }

  public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
  {
    var configuredOrigins = configuration["Cors:AllowedOrigins"];
    var origins = configuredOrigins?
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Where(origin => !string.IsNullOrWhiteSpace(origin))
      .ToArray() ?? Array.Empty<string>();

    services.AddCors(options =>
    {
      options.AddPolicy("FrontendCors", policy =>
      {
        if (origins.Length > 0)
        {
          policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
          return;
        }

        policy.AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
      });
    });
  }

  public static void ConfigureSwagger(this IServiceCollection services)
  {
    services.AddSwaggerGen(options =>
    {
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' followed by your token"
      });

      options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
              }
            },
            new string[] {}
          }
        });
    });
  }
}
