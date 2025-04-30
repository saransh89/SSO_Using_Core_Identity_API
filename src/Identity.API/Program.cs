using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Identity.Infrastructure.Services;
using Identity.Application.Contracts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Identity.Shared.Enums;
using Identity.Shared.Helper;
using Hangfire;
using System.IdentityModel.Tokens.Jwt;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 🔧 Add Services
builder.Services.AddControllers();

// 🗃 Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🔐 Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// 🔒 Add Authentication & JWT Bearer
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//}).AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateLifetime = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = builder.Configuration["Jwt:Issuer"],
//        ValidAudience = builder.Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
//        //RoleClaimType = "Role"  // map your claim to role
//    };
//});

// 🔒 Add Authentication & JWT Bearer
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    // Intercept requests to check for query string token for Hangfire
    options.Events = new JwtBearerEvents
    {
        

        OnMessageReceived = context =>
        {
            var path = context.HttpContext.Request.Path;
            // Check if the request is targeting Hangfire dashboard
            if (path.StartsWithSegments("/hangfire") ||
                ((path.StartsWithSegments("/hangfire/css") || path.StartsWithSegments("/hangfire/js") || path.StartsWithSegments("/hangfire/lib"))))
            {
                var token = context.Request.Query["token"];  // Get token from query string//"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIzYTI2NTEzMC05YmJhLTRlYTUtYTljOC1lYTNkM2M0N2QxZjUiLCJlbWFpbCI6IlNhcmFuc2g4OUBob3RtYWlsLmNvbSIsImp0aSI6IjEwMTI4ZmQxLTY3ZGYtNDM1Yi1hZDE4LThkYjgzZThlZDQ5MyIsIkRlcGFydG1lbnQiOiJJVCIsIlJvbGUiOiJBZG1pbiIsImV4cCI6MTc0NTk1NTI5OCwiaXNzIjoiSWRlbnRpdHkuQVBJIiwiYXVkIjoiSWRlbnRpdHkuQ2xpZW50In0.1BZkXUbDWTfKLvfT3t3yEdpJTmTr4xe7oHsLbHBJ57I";// 
                if (!string.IsNullOrEmpty(token))
                {
                    context.Response.Cookies.Append("HangfireAuth", token, new CookieOptions
                    {
                        HttpOnly = true,
                        SameSite = SameSiteMode.Strict,
                        Secure = true, // Use true if your site runs on HTTPS
                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    });

                    context.Token = token;  // Use the token found in the query string
                }
                else
                {
                    if (context.Request.Cookies.TryGetValue("HangfireAuth", out var cookieToken))
                    {
                        if (!string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }
                    }
                }
            }

            return Task.CompletedTask;
        },

        OnAuthenticationFailed = context =>
        {
            // Optionally log authentication failure
            Console.WriteLine($"Authentication failed: {context.Exception.Message}");
            return Task.CompletedTask;
        }
    };

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        NameClaimType = JwtRegisteredClaimNames.Sub, // Optional, set if you want to map sub as User.Identity.Name
        RoleClaimType = "Role"  // Optional, map Role claim if necessary
    };
});



// ✅ Add Authorization Policies
builder.Services.AddAuthorization(options =>
{
    // Policy for Hangfire access
    options.AddPolicy("HangfireAccess", policy =>
    {
        policy.RequireClaim("Department", "IT");
        policy.RequireClaim("Role", "Admin");
    });

    options.AddPolicy("ITAdminOnly", policy =>
        policy.RequireClaim("Department", "IT"));
});


builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("BackgroundJobsOnly", policy =>
    {
        policy.RequireAssertion(context =>
                context.User.HasClaim(c =>
                    c.Type == "JobName" && BackgroundJobPolicies.AllowedJobs.Contains(c.Value))
            );
    });
});

builder.Services.AddHangfire(config =>
    config.UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddHangfireServer(); // Adds background processing


// 💼 Dependency Injection
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// 📄 Add Swagger + JWT Auth Support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Identity API", Version = "v1" });

    // 🔒 JWT token support
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token **_only_**",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 🌐 Middleware
app.UseSwagger();
app.UseSwaggerUI();

app.UseStaticFiles();

app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

// 🔐 Apply global authentication for the rest of the app
app.UseAuthentication();
app.UseAuthorization();

// Add custom exception middleware
app.UseMiddleware<ExceptionMiddleware>();

app.MapControllers();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] {
        new HangfireAuthorizationFilter("HangfireAccess")
    }
});// Hangfire UI at /hangfire


RecurringJob.AddOrUpdate<IAuditLogService>(
    recurringJobId: "archive-audit-logs-job",
    methodCall: job => job.ArchiveAuditLogsAsync(null),
    cronExpression: Cron.Minutely,
    options: new RecurringJobOptions
    {
        TimeZone = TimeZoneInfo.Local
    });

app.Run();
