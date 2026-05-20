using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace кр.models
{
    public interface Rating
    {
        int Id { get; set; }
        int UserId { get; set; }
        int ApodPostId { get; set; }
        int Scrore { get; set; }
    }
}