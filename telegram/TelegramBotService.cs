using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using nasa_pictures.Services;
using nasa_pictures.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace nasa_pictures.Telegram;

public class TelegramBotService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<long, string> _userStates = new();

    public TelegramBotService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    private ReplyKeyboardMarkup GetMainMenu()
    {
        return new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "🌌 Фото дня", "📅 За датою" },
            new KeyboardButton[] { "⭐ Оцінити фото", "❤️ Збережене" }
        })
        { ResizeKeyboard = true };
    }

    private InlineKeyboardMarkup GetPictureKeyboard(string date)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❤️ Зберегти", $"fav_{date}"),
                InlineKeyboardButton.WithCallbackData("⭐ Оцінити", $"askrate_{date}")
            }
        });
    }

    private InlineKeyboardMarkup GetRatingKeyboard(string date)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("1 ⭐", $"rate_1_{date}"),
                InlineKeyboardButton.WithCallbackData("2 ⭐", $"rate_2_{date}"),
                InlineKeyboardButton.WithCallbackData("3 ⭐", $"rate_3_{date}"),
                InlineKeyboardButton.WithCallbackData("4 ⭐", $"rate_4_{date}"),
                InlineKeyboardButton.WithCallbackData("5 ⭐", $"rate_5_{date}")
            }
        });
    }

    private async Task<Models.User> GetOrCreateUserAsync(AppDbContext db, long chatId, string? username, CancellationToken ct)
    {
        var user = db.Users.FirstOrDefault(u => u.ChatId == chatId);
        if (user == null)
        {
            user = new Models.User { ChatId = chatId, Username = username };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
        }
        return user;
    }

    private async Task SendApodAsync(ITelegramBotClient bot, long chatId, ApodPost post, CancellationToken ct)
    {
        var ratings = await Task.Run(() =>
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var list = db.Ratings.Where(r => r.ApodPost.Date == post.Date).Select(r => r.Score).ToList();
            return list;
        });

        string avgText = ratings.Count > 0
            ? $"\n⭐ Середня оцінка: {ratings.Average():F1} ({ratings.Count} оцінок)"
            : "\n⭐ Оцеіок поки немає";

        await bot.SendPhoto(
            chatId,
            InputFile.FromUri(post.ImageUrl),
            caption: $"<b>{post.Title}</b>\n{post.Date}{avgText}",
            parseMode: ParseMode.Html,
            replyMarkup: GetPictureKeyboard(post.Date),
            cancellationToken: ct);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var nasaService = scope.ServiceProvider.GetRequiredService<NasaApiService>();

        if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
        {
            var callback = update.CallbackQuery;
            long chatId = callback.Message!.Chat.Id;
            string data = callback.Data!;

            var user = await GetOrCreateUserAsync(db, chatId, callback.From.Username, ct);

            if (data.StartsWith("fav_"))
            {
                string date = data["fav_".Length..];
                var post = db.ApodPosts.FirstOrDefault(p => p.Date == date);

                if (post == null)
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Пост не знайдено.", cancellationToken: ct);
                    return;
                }

                bool alreadySaved = db.Favorites.Any(f => f.UserId == user.Id && f.ApodPostId == post.Id);
                if (!alreadySaved)
                {
                    db.Favorites.Add(new Favorite { UserId = user.Id, ApodPostId = post.Id });
                    await db.SaveChangesAsync(ct);
                    await bot.AnswerCallbackQuery(callback.Id, "Додано в збережене! ❤️", cancellationToken: ct);
                }
                else
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Вже в збереженому!", showAlert: true, cancellationToken: ct);
                }
                return;
            }

            if (data.StartsWith("askrate_"))
            {
                string date = data["askrate_".Length..];
                await bot.AnswerCallbackQuery(callback.Id, cancellationToken: ct);
                await bot.SendMessage(
                    chatId,
                    $"Поставте оцінку фото від {date}:",
                    replyMarkup: GetRatingKeyboard(date),
                    cancellationToken: ct);
                return;
            }

            if (data.StartsWith("rate_"))
            {
                string[] parts = data.Split('_');
                if (parts.Length < 3 || !int.TryParse(parts[1], out int score))
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Некоректні дані.", cancellationToken: ct);
                    return;
                }
                string date = parts[2];

                var post = db.ApodPosts.FirstOrDefault(p => p.Date == date);
                if (post == null)
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Пост не знайдено.", cancellationToken: ct);
                    return;
                }

                bool alreadyRated = db.Ratings.Any(r => r.UserId == user.Id && r.ApodPostId == post.Id);
                if (!alreadyRated)
                {
                    db.Ratings.Add(new Rating { UserId = user.Id, ApodPostId = post.Id, Score = score });
                    await db.SaveChangesAsync(ct);

                    var allScores = db.Ratings.Where(r => r.ApodPostId == post.Id).Select(r => r.Score).ToList();
                    string avg = allScores.Count > 0 ? $"{allScores.Average():F1}" : "—";

                    await bot.AnswerCallbackQuery(callback.Id, $"Ви поставили {score} ⭐!", cancellationToken: ct);
                    await bot.SendMessage(chatId, $"Дякуємо за оцінку! Середня оцінка: {avg} ⭐ ({allScores.Count} голосів)", cancellationToken: ct);
                }
                else
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Ви вже оцінювали це фото!", showAlert: true, cancellationToken: ct);
                }
                return;
            }

            if (data.StartsWith("delefav_"))
            {
                string dateStr = data["delefav_".Length..];
                if (!long.TryParse(dateStr, out long favId))
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Помилка.", cancellationToken: ct);
                    return;
                }
                var fav = db.Favorites.FirstOrDefault(f => f.Id == favId && f.UserId == user.Id);
                if (fav != null)
                {
                    db.Favorites.Remove(fav);
                    await db.SaveChangesAsync(ct);
                    await bot.AnswerCallbackQuery(callback.Id, "Видалено з улюбленого.", cancellationToken: ct);
                }
                else
                {
                    await bot.AnswerCallbackQuery(callback.Id, "Не знайдено.", cancellationToken: ct);
                }
                return;
            }

            return;
        }

        if (update.Message is not { Text: { } messageText } message) return;
        long msgChatId = message.Chat.Id;

        if (_userStates.TryGetValue(msgChatId, out string? state) && state == "waiting_for_date")
        {
            _userStates[msgChatId] = "none";

            if (!DateTime.TryParseExact(messageText, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
            {
                await bot.SendMessage(msgChatId, "Невірний формат. Використовуйте YYYY-MM-DD, наприклад 2023-10-15.", replyMarkup: GetMainMenu(), cancellationToken: ct);
                return;
            }

            var post = await nasaService.GetPictureByDateAsync(messageText);
            if (post != null)
            {
                await SendApodAsync(bot, msgChatId, post, ct);
            }
            else
            {
                await bot.SendMessage(msgChatId, "Фото не знайдено або це відео.", cancellationToken: ct);
            }
            await bot.SendMessage(msgChatId, "Головне меню:", replyMarkup: GetMainMenu(), cancellationToken: ct);
            return;
        }

        if (_userStates.TryGetValue(msgChatId, out string? rateState) && rateState == "waiting_for_rate_date")
        {
            _userStates[msgChatId] = "none";

            if (!DateTime.TryParseExact(messageText, "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
            {
                await bot.SendMessage(msgChatId, "Невірний формат. Використовуйте YYYY-MM-DD.", replyMarkup: GetMainMenu(), cancellationToken: ct);
                return;
            }

            var post = db.ApodPosts.FirstOrDefault(p => p.Date == messageText);
            if (post == null)
            {
                post = await nasaService.GetPictureByDateAsync(messageText);
            }

            if (post != null)
            {
                await bot.SendMessage(
                    msgChatId,
                    $"Поставте оцінку фото від {messageText}:",
                    replyMarkup: GetRatingKeyboard(messageText),
                    cancellationToken: ct);
            }
            else
            {
                await bot.SendMessage(msgChatId, "Фото не знайдено або це відео.", cancellationToken: ct);
            }
            await bot.SendMessage(msgChatId, "Головне меню:", replyMarkup: GetMainMenu(), cancellationToken: ct);
            return;
        }

        switch (messageText)
        {
            case "/start":
                await bot.SendMessage(msgChatId, "Привіт! Я бот NASA 🚀\nОберіть дію:", replyMarkup: GetMainMenu(), cancellationToken: ct);
                break;

            case "🌌 Фото дня":
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                Console.WriteLine($"[ФотоДня] Запрос фото: {today}");
                try
                {
                    var todayPost = await nasaService.GetPictureByDateAsync(today);
                    if (todayPost != null)
                    {
                        Console.WriteLine($"[ФотоДня] Не знайдено: {todayPost.Title}, URL: {todayPost.ImageUrl}");
                        await SendApodAsync(bot, msgChatId, todayPost, ct);
                    }
                    else
                    {
                        Console.WriteLine("[ФотоДня] null — можливо, це відео або помилка API.");
                        await bot.SendMessage(msgChatId,
                            $"Сьогодні ({today}) NASA виклали відео, а не фото. Спробуйте вчорашню дату через 📅 За датою.",
                            replyMarkup: GetMainMenu(), cancellationToken: ct);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ФотоДня] Виключення: {ex.Message}");
                    await bot.SendMessage(msgChatId, "Помилка при отриманні фото дня.", replyMarkup: GetMainMenu(), cancellationToken: ct);
                }
            break;

            case "❤️ Избранное":
                var user = await GetOrCreateUserAsync(db, msgChatId, message.From?.Username, ct);
                var favorites = db.Favorites
                    .Include(f => f.ApodPost)
                    .Where(f => f.UserId == user.Id)
                    .ToList();

                if (favorites.Count == 0)
                {
                    await bot.SendMessage(msgChatId, "Поки що немає вибраних фото. Додайте через кнопку ❤️ В збережене.", replyMarkup: GetMainMenu(), cancellationToken: ct);
                    break;
                }

                foreach (var fav in favorites)
                {
                    var favPost = fav.ApodPost;
                    var removeKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🗑 Видалити зі збережених", $"delefav_{fav.Id}") }
                    });
                    await bot.SendPhoto(
                        msgChatId,
                        InputFile.FromUri(favPost.ImageUrl),
                        caption: $"<b>{favPost.Title}</b>\n{favPost.Date}",
                        parseMode: ParseMode.Html,
                        replyMarkup: removeKeyboard,
                        cancellationToken: ct);
                }
                await bot.SendMessage(msgChatId, "Ваші збережені фото ☝️", replyMarkup: GetMainMenu(), cancellationToken: ct);
                break;

            default:
                await bot.SendMessage(msgChatId, "Використовуйте кнопки меню.", replyMarkup: GetMainMenu(), cancellationToken: ct);
                break;
        }
    }

    public Task HandlePollingErrorAsync(ITelegramBotClient bot, Exception ex, CancellationToken ct) => Task.CompletedTask;
}