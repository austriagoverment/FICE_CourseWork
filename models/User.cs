using System.ComponentModel.DataAnnotations;

namespace nasa_pictures.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    [Required]
    public long ChatId { get; set; }
    public string? Username { get; set; }
}