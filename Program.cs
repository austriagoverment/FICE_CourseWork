using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using DotNetEnv;
using nasa_pictures.Models;
using nasa_pictures.Services;
using nasa_pictures;
using nasa_pictures.Background;
using nasa_pictures.Telegram;

Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHostedService<DailyPostingService>();

string dbConnection = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")!;
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConnection));

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddScoped<NasaApiService>();

var isMigration = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Migration";

if (!isMigration)
{
    string botToken = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN")!;
    
    if (string.IsNullOrEmpty(botToken))
    {
        Console.WriteLine("Token is missing");
        return;
    }

    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));
    builder.Services.AddSingleton<TelegramBotService>();
}

var app = builder.Build();

if (!isMigration)
{
    var botClient = app.Services.GetRequiredService<ITelegramBotClient>();
    var telegramService = app.Services.GetRequiredService<TelegramBotService>();
    var cts = new CancellationTokenSource();

    botClient.StartReceiving(
        telegramService.HandleUpdateAsync,
        telegramService.HandlePollingErrorAsync,
        new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() },
        cts.Token
    );

    Console.WriteLine("Бот успішно запущений!");
}

app.Run();