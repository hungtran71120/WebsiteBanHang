using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using HungStore.Application.Auth;
using HungStore.Application.Auth.Interfaces;
using HungStore.Application.Banners;
using HungStore.Application.Banners.Interfaces;
using HungStore.Application.Cart;
using HungStore.Application.Cart.Interfaces;
using HungStore.Application.Categories;
using HungStore.Application.Chat;
using HungStore.Application.Chat.Interfaces;
using HungStore.Application.Categories.Interfaces;
using HungStore.Application.FlashSales;
using HungStore.Application.FlashSales.Interfaces;
using HungStore.Application.Notifications;
using HungStore.Application.Notifications.Interfaces;
using HungStore.Application.Orders;
using HungStore.Application.Orders.Interfaces;
using HungStore.Application.Products;
using HungStore.Application.Products.Interfaces;
using HungStore.Application.Recommendations;
using HungStore.Application.Recommendations.Interfaces;
using HungStore.Application.Reviews;
using HungStore.Application.Reviews.Interfaces;
using HungStore.Application.Statistics;
using HungStore.Application.Statistics.Interfaces;
using HungStore.Application.Users;
using HungStore.Application.Users.Interfaces;
using HungStore.Application.Vouchers;
using HungStore.Application.Vouchers.Interfaces;
using HungStore.Application.Wishlist;
using HungStore.Application.Wishlist.Interfaces;

namespace HungStore.Application;

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
        services.AddScoped<IBannerService, BannerService>();

        return services;
    }
}
