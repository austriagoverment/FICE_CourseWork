using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using TelegramBotClient = Telegram.Bot.ITelegramBotClient;
using TelegramInputFile = Telegram.Bot.Types.InputFile;
using TelegramInlineKeyboard = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup;
using TelegramInlineButton = Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton;
using Telegram.Bot.Types.Enums;
using nasa_pictures.Services;
using nasa_pictures.Models;
using Telegram.Bot;

namespace nasa_pictures.Background;

public class DailyPostingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly TelegramBotClient _bot;
    private readonly TimeOnly _postTime;

    public DailyPostingService(IServiceScopeFactory scopeFactory, TelegramBotClient bot)
    {
        _scopeFactory = scopeFactory;
        _bot = bot;
        _postTime = new TimeOnly(9, 0); // время рассылки — 09:00 UTC
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            TimeSpan delay = GetDelayUntilNextPost();
            Console.WriteLine($"[DailyPosting] Следующая рассылка через {delay.TotalMinutes:F0} минут.");
            await Task.Delay(delay, ct);

            if (!ct.IsCancellationRequested)
                await SendDailyPhotoToAllUsersAsync(ct);
        }
    }

    private TimeSpan GetDelayUntilNextPost()
    {
        DateTime now = DateTime.UtcNow;
        DateTime next = now.Date.Add(_postTime.ToTimeSpan());
        if (next <= now)
            next = next.AddDays(1);
        return next - now;
    }

    private async Task SendDailyPhotoToAllUsersAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nasaService = scope.ServiceProvider.GetRequiredService<NasaApiService>();

        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        Console.WriteLine($"[DailyPosting] Получаю фото дня за {today}...");

        ApodPost? post = await nasaService.GetPictureByDateAsync(today);
        if (post == null)
        {
            Console.WriteLine("[DailyPosting] Фото дня не найдено или это видео. Рассылка отменена.");
            return;
        }

        var ratings = db.Ratings
            .Where(r => r.ApodPost.Date == today)
            .Select(r => r.Score)
            .ToList();

        string avgText = ratings.Count > 0
            ? $"\n⭐ Средняя оценка: {ratings.Average():F1} ({ratings.Count} оценок)"
            : "";

        string caption = $"🌌 <b>Фото дня — {post.Date}</b>\n\n<b>{post.Title}</b>{avgText}";

        var keyboard = new TelegramInlineKeyboard(new[]
        {
            new[]
            {
                TelegramInlineButton.WithCallbackData("❤️ В избранное", $"fav_{post.Date}"),
                TelegramInlineButton.WithCallbackData("⭐ Оценить", $"askrate_{post.Date}")
            }
        });

        var users = db.Users.ToList();
        Console.WriteLine($"[DailyPosting] Рассылаю {users.Count} пользователям...");

        int success = 0, failed = 0;

        foreach (var user in users)
        {
            try
            {
                await _bot.SendPhoto(
                    user.ChatId,
                    TelegramInputFile.FromUri(post.ImageUrl),
                    caption: caption,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: ct);

                success++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DailyPosting] Не удалось отправить пользователю {user.ChatId}: {ex.Message}");
                failed++;
            }

            await Task.Delay(100, ct); // пауза между отправками чтобы не попасть в rate limit
        }

        Console.WriteLine($"[DailyPosting] Готово. Успешно: {success}, ошибок: {failed}.");
    }
}