using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CardOverflow.Entity
{
    public partial class UserAndCardEntity
    {
        public int UserId { get; set; }
        public int CardId { get; set; }
    }
}
