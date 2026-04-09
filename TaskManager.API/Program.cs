using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Bussiness.Repositories;
//using TaskManager.API.Authorization.Handlers;
//using TaskManager.API.Authorization.Requirements;
using TaskManager.API.Middleware;
//using TaskManager.Bussiness.Helpers;
//using TaskManager.Bussiness.Services;
using TaskManager.Data.Context;
using TaskManager.Data.Entities;
using TaskManager.Bussiness.Config;
using TaskManager.Bussiness.Services;
using TaskManager.Data.Seeders;
//using TaskManager.Data.Seeders;

namespace TaskManager.API
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers(options =>
            {
                options.Filters.Add<ApiResponseFilter>();
            });
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
            builder.Services.AddDbContext<AppDbContext>(Options =>
            {
                Options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddIdentity<ApplicationUser, Role>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();
            //bind jwt settings
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JWT"));
            builder.Services.AddScoped<ITokenService ,TokenServices>();
            builder.Services.AddScoped<AuthService>();

            ////DIRECT BINDING FOR NOW USING
            var JwtSettings = builder.Configuration.GetSection("JWT").Get<JwtSettings>();           
            builder.Services.AddSingleton(JwtSettings);
            var Key = Encoding.UTF8.GetBytes(JwtSettings.Key);
            //Add Authintication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidIssuer = JwtSettings.Issuer,
                    ValidAudience = JwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Key)
                }
            );
            //builder.Services.AddSingleton<IAuthorizationHandler, OwnerOrAdminHandler>();
            //builder.Services.AddSingleton<IAuthorizationHandler,TaskIsNotCompletedHandler>();
            //builder.Services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Manager", policy => policy.RequireRole("Manager"));
            //    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            //    options.AddPolicy("User", policy => policy.RequireRole("User"));
            //    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Manager","Admin"));
            //    options.AddPolicy("AdminOrOwner", policy => policy.Requirements.Add(new OwnerOrAdminRequirment()));
            //    options.AddPolicy("EditTaskIsNotCompleted", policy => policy.Requirements.Add(new TaskIsNotCompletedRequirement()));
            //    //options.AddPolicy("CanEditOwnTask", policy => policy.Requirements.Add(new CanEditOwnTaskRequirement()));

            //    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            //});
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });
            builder.Services.AddScoped<IUnitOfWork,UnitOfWorkRepo>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.UseMiddleware<GlobalExceptionMiddleware>();
            //app.UseMiddleware<ResponseWrapperMiddleware>();
            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            using (var scope = app.Services.CreateScope())
            { 
                await IdentitySeeder.SeedAsync(scope.ServiceProvider);
            }
            app.MapControllers();
            
            app.Run();
        }
    }
}
