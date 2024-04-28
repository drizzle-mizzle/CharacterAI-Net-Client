using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharacterAiNet.Models.DTO
{
    public class ChatFull
    {
        public string? character_avatar_uri { get; set; }
        public string? character_id { get; set; }
        public string? character_name { get; set; }
        public string? character_visibility { get; set; }
        public Guid chat_id { get; set; }
        public DateTime create_time { get; set; }
        public string? creator_id { get; set; }
        public string? state { get; set; }
        public string? type { get; set; }
        public string? visibility { get; set; }
    }

    public class ChatShort
    {
        public string? character_id { get; set; }
        public Guid chat_id { get; set; }
        public string? creator_id { get; set; }
        public string? type { get; set; }
        public string? visibility { get; set; }
    }
}
