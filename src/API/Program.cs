using System.Text.Json.Serialization;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;
using HungStore.API.Hubs;
using HungStore.API.Middleware;
using HungStore.Application;
using HungStore.Application.Chat.Interfaces;
using HungStore.Application.Notifications.Interfaces;
using HungStore.Infrastructure;
using HungStore.Infrastructure.Persistence;

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
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "HungStore API", Version = "v1" });

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

app.Lifetime.ApplicationStarted.Register(() => _ = Task.Run(async () =>
{
    try
    {
        await SeedData.SeedAsync(app.Services);
    }
    catch (Exception ex)
    {
        app.Services.GetRequiredService<ILogger<Program>>().LogError(ex, "Database seeding failed on startup");
    }
}));

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
