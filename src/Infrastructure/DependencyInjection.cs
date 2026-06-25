using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Application.Products.Interfaces;
using ShopeeClone.Domain.Interfaces;
using ShopeeClone.Infrastructure.BackgroundJobs;
using ShopeeClone.Infrastructure.Email;
using ShopeeClone.Infrastructure.FileStorage;
using ShopeeClone.Infrastructure.Identity;
using ShopeeClone.Infrastructure.Persistence;
using ShopeeClone.Infrastructure.Persistence.Repositories;

namespace ShopeeClone.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration, string webRootPath)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.EnableRetryOnFailure()));

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            (context.HttpContext.Request.Path.StartsWithSegments("/hubs/chat") ||
                             context.HttpContext.Request.Path.StartsWithSegments("/hubs/notifications")))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IChatRepository, ChatRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IWishlistRepository, WishlistRepository>();
        services.AddScoped<IVoucherRepository, VoucherRepository>();
        services.AddScoped<IFlashSaleRepository, FlashSaleRepository>();
        var uploadsRootPath = configuration["Storage:UploadsRootPath"];
        var effectiveUploadsRoot = string.IsNullOrWhiteSpace(uploadsRootPath) ? webRootPath : uploadsRootPath;
        services.AddSingleton<IFileStorageService>(new LocalFileStorageService(effectiveUploadsRoot));

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        services.AddHostedService<OrderAutoDeliveryBackgroundService>();

        return services;
    }
}
