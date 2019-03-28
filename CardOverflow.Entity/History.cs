using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class History
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }
        public byte Difficulty { get; set; }
        public DateTime Timestamp { get; set; }

        public virtual Card Card { get; set; }
        public virtual User User { get; set; }
    }
}
