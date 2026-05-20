using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nasa_pictures.models
{
    public interface Rating
    {
        int Id { get; set; }
        int UserId { get; set; }
        int ApodPostId { get; set; }
        int Scrore { get; set; }
    }
}