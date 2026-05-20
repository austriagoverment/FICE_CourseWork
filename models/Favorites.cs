using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
    
namespace nasa_pictures.models
{
    public interface Favourits
    {
       int Id { get; set; }
       int UserId { get; set; }
       int ApodPostId { get; set; }
        
    }
}