using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace nasa_pictures.controllers
{
    public class AstroPost : models.IAstroPost
    {
        [Key]
        [Column("date_id")]
        [MaxLength(10)]
        public string DateId { get; set; }

        [Required]
        [Column("title")]
        [MaxLength(255)]
        public string Title { get; set; }

        [Column("explanation")]
        public string Explanation { get; set; }

        [Required]
        [Column("media_url")]
        public string MediaUrl { get; set; }

        [Column("hd_media_url")]
        public string HdMediaUrl { get; set; }

        [Column("media_type")]
        [MaxLength(20)]
        public string MediaType { get; set; }
    }
}