using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;
using nasa_pictures.Models;
using nasa_pictures.Dto;

namespace nasa_pictures.Services;

public class NasaApiService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly AppDbContext _db;
    private readonly string _apiKey;

    public NasaApiService(HttpClient http, IMemoryCache cache, AppDbContext db)
    {
        _http = http;
        _cache = cache;
        _db = db;
        _apiKey = Environment.GetEnvironmentVariable("NASA_API_KEY") ?? "DEMO_KEY";
    }

    public async Task<ApodPost?> GetPictureByDateAsync(string date)
    {
        string cacheKey = $"apod_{date}";

        if (_cache.TryGetValue(cacheKey, out ApodPost? cachedPost))
        {
            Console.WriteLine("Взято з кеша");
            return cachedPost;
        }

        var dbPost = _db.ApodPosts.FirstOrDefault(p => p.Date == date);
        if (dbPost != null)
        {
            Console.WriteLine("Взято из БД!");
            _cache.Set(cacheKey, dbPost, TimeSpan.FromHours(24)); 
            return dbPost;
        }

        Console.WriteLine("Запрос к NASA API...");
        string requestUrl = $"https://api.nasa.gov/planetary/apod?api_key={_apiKey}&date={date}";
        var response = await _http.GetFromJsonAsync<NasaApodDto>(requestUrl);

        if (response != null && response.MediaType == "image")
        {
            var newPost = new ApodPost
            {
                Date = response.Date ?? date,
                Title = response.Title ?? "Без названия",
                Explanation = response.Explanation ?? "",
                ImageUrl = response.Url ?? ""
            };

            _db.ApodPosts.Add(newPost);
            await _db.SaveChangesAsync();

            _cache.Set(cacheKey, newPost, TimeSpan.FromHours(24));
            
            return newPost;
        }

        return null;
    }
}