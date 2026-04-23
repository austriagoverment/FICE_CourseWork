namespace nasa_pictures
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var apiService = new ApiService();
            var picture = await apiService.GetPictureOfTheDayAsync();
            Console.WriteLine($"Title: {picture.Title}");
            Console.WriteLine($"Date: {picture.Date}");
            Console.WriteLine($"Explanation: {picture.Explanation}");
        }
    }
}