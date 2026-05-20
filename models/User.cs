using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace nasa_pictures.models
{
    public interface User
    {
        int Id { get; set; }
        string TelegramChatId { get; set; }
        string Username { get; set; }
        string CreatedAt { get; set; }
    }
}