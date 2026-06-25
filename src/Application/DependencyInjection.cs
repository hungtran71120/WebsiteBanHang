using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ShopeeClone.Application.Auth;
using ShopeeClone.Application.Auth.Interfaces;
using ShopeeClone.Application.Cart;
using ShopeeClone.Application.Cart.Interfaces;
using ShopeeClone.Application.Categories;
using ShopeeClone.Application.Chat;
using ShopeeClone.Application.Chat.Interfaces;
using ShopeeClone.Application.Categories.Interfaces;
using ShopeeClone.Application.FlashSales;
using ShopeeClone.Application.FlashSales.Interfaces;
using ShopeeClone.Application.Notifications;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Application.Orders;
using ShopeeClone.Application.Orders.Interfaces;
using ShopeeClone.Application.Products;
using ShopeeClone.Application.Products.Interfaces;
using ShopeeClone.Application.Recommendations;
using ShopeeClone.Application.Recommendations.Interfaces;
using ShopeeClone.Application.Reviews;
using ShopeeClone.Application.Reviews.Interfaces;
using ShopeeClone.Application.Statistics;
using ShopeeClone.Application.Statistics.Interfaces;
using ShopeeClone.Application.Users;
using ShopeeClone.Application.Users.Interfaces;
using ShopeeClone.Application.Vouchers;
using ShopeeClone.Application.Vouchers.Interfaces;
using ShopeeClone.Application.Wishlist;
using ShopeeClone.Application.Wishlist.Interfaces;

namespace ShopeeClone.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<IStatisticsService, StatisticsService>();
        services.AddScoped<IWishlistService, WishlistService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IFlashSaleService, FlashSaleService>();

        return services;
    }
}
