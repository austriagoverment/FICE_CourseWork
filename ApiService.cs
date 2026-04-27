using System.Text.Json;

namespace nasa_pictures
{
    public class ApiService
    {
        private readonly HttpClient _httpClient;

        public ApiService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ApiResponse?> GetPictureOfTheDayAsync()
        {
            string apiUrl = "https://api.nasa.gov/planetary/apod?api_key=DEMO_KEY";
            var response = await _httpClient.GetAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ApiResponse>(jsonResponse);
            }
            else
            {
                return null;
            }
        }
    }
}