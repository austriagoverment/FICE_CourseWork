using System.ComponentModel.DataAnnotations;

namespace nasa_pictures.Models;

public class ApodPost
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Date { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public double AverageRating { get; set; } = 0;
}