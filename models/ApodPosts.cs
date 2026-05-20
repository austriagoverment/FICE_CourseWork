using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace кр.models
{
    public interface ApodPosts
    {
        int Id { get; set; }
       string Date { get; set; }
       string Title { get; set; }
       string ImageUrl { get; set; }
       string Description { get; set; }
       int AvaregeRating { get; set; }
    }
}