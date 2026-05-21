using System.ComponentModel.DataAnnotations;

namespace nasa_pictures.Models;

public class Rating
{
    [Key]
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    
    public int ApodPostId { get; set; }
    public ApodPost? ApodPost { get; set; }
    
    public int Score { get; set; }
}