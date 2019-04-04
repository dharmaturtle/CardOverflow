using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
[Table("History")]
    public partial class HistoryEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CardId { get; set; }
        public byte ScoreAndMemorizationState { get; set; }
        [Column(TypeName = "smalldatetime")]
        public DateTime Timestamp { get; set; }
        public short Interval { get; set; }
        public short EaseFactor { get; set; }
        public short Time { get; set; }

        [ForeignKey("CardId")]
        [InverseProperty("Histories")]
        public virtual CardEntity Card { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Histories")]
        public virtual UserEntity User { get; set; }
    }
}
