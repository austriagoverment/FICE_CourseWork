namespace nasa_pictures
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            var apiService = new ApiService();
            var picture = await apiService.GetPictureOfTheDayAsync();
                if (picture == null)
                {
                    Console.WriteLine("Не вдалось отримати дані від NASA API.");
                    return;
                }
            Console.WriteLine($"Title: {picture.Title}");
            Console.WriteLine($"Date: {picture.Date}");
            Console.WriteLine($"Explanation: {picture.Explanation}");
        }
    }
}