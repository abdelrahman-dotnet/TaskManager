using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskManager.API.Authorization;
using TaskManager.API.Filters;
using TaskManager.API.HealthChecks;
using TaskManager.API.Middleware;
using TaskManager.API.Services;
using TaskManager.API.Validators.Task;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Config;
using TaskManager.Bussiness.Services;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.UnitOfWork;

namespace TaskManager.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(
                new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build())
              .CreateLogger();
            var builder = WebApplication.CreateBuilder(args);
            // serilog
            builder.Host.UseSerilog((ctx, lc) =>
            {
                lc.ReadFrom.Configuration(ctx.Configuration)
                  .Enrich.FromLogContext()
                  .Enrich.WithProperty("Application", "TaskManagerAPI")
                  .Enrich.WithProperty("Environment", ctx.HostingEnvironment.EnvironmentName)
                  .WriteTo.Console();
            });
            // Add services to the container.
            builder.Services.AddControllers(options =>
            {                
                options.Filters.Add<ValidationFilter>();
                options.Filters.Add<ApiResponseFilter>();
            }).AddJsonOptions(options =>
            {
                options.JsonSerializerOptions
                .Converters
                .Add(new JsonStringEnumConverter());
            });
            //HEALTH CHECK
            builder.Services.AddApplicationHealthChecks(builder.Configuration);
            // Cache
            builder.Services.AddMemoryCache();
            builder.Services.AddScoped<ICacheService, CacheService>();
            builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379"));
            // Swagger/OpenAPI
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskManager API", Version = "v1" });

                // Security Scheme (JWT)
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "ضع الـ JWT هنا بدون كلمة Bearer",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey
                });

                // Apply Security to all endpoints
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

            // DbContext
            builder.Services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
                //options.UseNpgsql(
                //      builder.Configuration.GetConnectionString("DefaultConnection"));

            });
            // current user
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            // Identity
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            // Auto Mapper
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            // JWT Settings
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
            builder.Services.AddScoped<ITokenService, TokenServices>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<ITaskService, TaskService>();
            builder.Services.AddScoped<IProjectService, ProjectService>();
            builder.Services.AddScoped<ITeamService, TeamService>();
            builder.Services.AddScoped<ICommentService, CommentService>();
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();
            builder.Services.AddScoped<INotificationService, NotificationService>();
            builder.Services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
            builder.Services.AddScoped<ITaskStatusHistoryService, TaskStatusHistoryService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();

            //Jwt Settings
            var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>();
            builder.Services.AddSingleton(jwtSettings);
            var key = Encoding.UTF8.GetBytes(jwtSettings.Key);

            // Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            // Authorization
            builder.Services.AddAuthorization(options =>
            {
                foreach (var permission in Permissions.All)
                {
                    options.AddPolicy(permission,
                        policy =>
                        {
                            policy.RequireClaim(
                                CustomClaimTypes.Permission,
                                permission);
                        });
                }
            });

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Repositories & Services
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


            // FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskValidator>();

            // ApiBehaviorOptions (لمنع الرد الافتراضي للأخطاء)
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            //{
                app.UseSwagger();
                app.UseSwaggerUI();
            //}
            // CorrelationIdMiddleware
            app.UseMiddleware<CorrelationIdMiddleware>();
            // Middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();
            //serilog
            app.UseSerilogRequestLogging();
            //app.UseMiddleware<ResponseWrapperMiddleware>();

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            // Identity Seeder
            //using var scope = app.Services.CreateScope();

            //var services = scope.ServiceProvider;

            //var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            //var roleManager =
            //    services.GetRequiredService<RoleManager<ApplicationRole>>();

            //var dbContext =
            //    services.GetRequiredService<AppDbContext>();

            //await PermissionAndRoleSeeder.SeedAsync(userManager, roleManager, dbContext);

            app.MapControllers();
            // health check
            app.MapApplicationHealthChecks();
            app.Run();
        }
    }
}
