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
using ApiPermissions = TaskManager.API.Authorization.Permissions;
using BizPermissions = TaskManager.Bussiness.Authorization.Permissions;
using TaskManager.API.Filters;
using TaskManager.API.HealthChecks;
using TaskManager.API.Middleware;
using TaskManager.API.Services;
using TaskManager.API.Validators.Task;
using TaskManager.Business.Services.Interfaces;
using TaskManager.Business.UnitOfWork;
using TaskManager.Bussiness.Authorization;
using TaskManager.Bussiness.Config;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Bussiness.Repositories;
using TaskManager.Bussiness.Services;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Data.Repositories;
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
                    Description = "?? ??? JWT ??? ???? ???? Bearer",
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
builder.Services.AddScoped<IWorkspaceService, WorkspaceService>();
            builder.Services.AddScoped<ITaskItemStatusHistoryService, TaskItemStatusHistoryService>();
            builder.Services.AddScoped<IAuditLogService, AuditLogService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<IInvitationService, InvitationService>();

            // AUTH PIPELINE (13 أغسطس 2026) — الـ Authorization Pipeline الجديد
            builder.Services.AddScoped<IWorkspaceAuthorizationService, WorkspaceAuthorizationService>();

            // JWT signing material must be supplied through protected configuration
            // (for example, the JWT__Key environment variable), never source control.
            var jwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>()
                ?? throw new InvalidOperationException("JWT configuration is required.");
            if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
            {
                throw new InvalidOperationException("JWT:Key must be supplied through protected configuration and contain at least 32 bytes.");
            }

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
                // BizPermissions are workspace-level under X2. They must own any
                // shared policy name so endpoint gates stay authenticated-only;
                // WorkspaceMember.Role -> RolePermissionCatalog -> Pipeline makes
                // the actual workspace authorization decision in the service.
                var workspacePermissions = new HashSet<string>(BizPermissions.All, StringComparer.Ordinal);

                // ApiPermissions remain claim-gated only when they are not also
                // workspace business permissions. This preserves platform/API
                // authorization without letting an Identity role override workspace authority.
                foreach (var permission in ApiPermissions.All)
                {
                    if (workspacePermissions.Contains(permission))
                        continue;

                    options.AddPolicy(permission,
                        policy =>
                        {
                            policy.RequireClaim(
                                CustomClaimTypes.Permission,
                                permission);
                        });
                }

                foreach (var permission in BizPermissions.All)
                {
                    options.AddPolicy(permission,
                        policy =>
                        {
                            policy.RequireAuthenticatedUser();
                        });
                }
            });

            // CORS is opt-in and origin-specific. Configure Cors:AllowedOrigins through
            // environment-specific settings (for example, Cors__AllowedOrigins__0).
            var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ConfiguredCors", policy =>
                {
                    if (allowedCorsOrigins.Length == 0)
                    {
                        return;
                    }

                    policy.WithOrigins(allowedCorsOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            // Repositories & UnitOfWork
            builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(Repository<>));
            builder.Services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
            builder.Services.AddScoped<ITaskRepository, TaskRepository>();
            builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IAttachmentRepository, AttachmentRepository>();
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();


            // FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<CreateTaskValidator>();

            // ApiBehaviorOptions
            builder.Services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            var app = builder.Build();

            // Permission/Role seed (existing IdentitySeeder pattern).
            // Required: the JWT permission claims come from AspNetRoles → RolePermissions,
            // so without this seed no [Authorize(Policy=...)] endpoint is reachable.
            using (var seedScope = app.Services.CreateScope())
            {
                var userManager = seedScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = seedScope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
                var context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
                await PermissionAndRoleSeeder.SeedAsync(userManager, roleManager, context);
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // CorrelationIdMiddleware
            app.UseMiddleware<CorrelationIdMiddleware>();
            // Middleware
            app.UseMiddleware<GlobalExceptionMiddleware>();
            //serilog
            app.UseSerilogRequestLogging();

            app.UseHttpsRedirection();
            app.UseCors("ConfiguredCors");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            // health check
            app.MapHealthCheckEndpoints(app.Environment.IsDevelopment());

            if (app.Environment.IsDevelopment())
            {
                app.MapHealthCheckDashboard();
            } 

            app.Run();
        }
    }
}
