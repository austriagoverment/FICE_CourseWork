using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nasa_pictures.models
{
    public interface IAstroPost
    {
        string DateId { get; set; } 
        
        string Title { get; set; } 
        string Explanation { get; set; } 
        
        string MediaUrl { get; set; } 
        
        string HdMediaUrl { get; set; } 
        
        string MediaType { get; set; }
    }
}