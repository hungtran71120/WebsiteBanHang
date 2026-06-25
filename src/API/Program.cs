using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using ShopeeClone.API.Hubs;
using ShopeeClone.API.Middleware;
using ShopeeClone.Application;
using ShopeeClone.Application.Chat.Interfaces;
using ShopeeClone.Application.Notifications.Interfaces;
using ShopeeClone.Infrastructure;
using ShopeeClone.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

const string CorsPolicyName = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment.WebRootPath);
builder.Services.AddSignalR();
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddScoped<INotificationPusher, SignalRNotificationPusher>();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "ShopeeClone API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT theo dạng: Bearer {token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

await SeedData.SeedAsync(app.Services);

// Configure the HTTP request pipeline.
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

var uploadsRootPath = builder.Configuration["Storage:UploadsRootPath"];
if (!string.IsNullOrWhiteSpace(uploadsRootPath))
{
    var uploadsPhysicalPath = Path.Combine(uploadsRootPath, "uploads");
    Directory.CreateDirectory(uploadsPhysicalPath);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadsPhysicalPath),
        RequestPath = "/uploads"
    });
}

app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();

public partial class Program
{
}
