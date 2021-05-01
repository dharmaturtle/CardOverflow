﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;
using NUlid;
using NodaTime;

namespace CardOverflow.Entity
{
    public partial class CardEntity
    {
        public CardEntity()
        {
            Histories = new HashSet<HistoryEntity>();
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public Guid Id { get; set; } = Ulid.NewUlid().ToGuid();
        public Guid UserId { get; set; }
        public Guid ConceptId { get; set; }
        public Guid ExampleId { get; set; }
        public Guid RevisionId { get; set; }
        public short Index { get; set; }
        public short CardState { get; set; }
        public short EaseFactorInPermille { get; set; }
        public short IntervalOrStepsIndex { get; set; }
        public Instant Due { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Instant Created { get; set; }
        public Instant? Modified { get; set; }
        public Guid CardSettingId { get; set; }
        public Guid DeckId { get; set; }
        public bool IsLapsed { get; set; }
        [Required]
        [StringLength(5000)]
        public string FrontPersonalField
        {
            get => _FrontPersonalField;
            set
            {
                if (value.Length > 5000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and FrontPersonalField has a maximum length of 5000. Attempted value: {value}");
                _FrontPersonalField = value;
            }
        }
        private string _FrontPersonalField = "";
        [Required]
        [StringLength(5000)]
        public string BackPersonalField
        {
            get => _BackPersonalField;
            set
            {
                if (value.Length > 5000) throw new ArgumentOutOfRangeException($"String too long! It was {value.Length} long, and BackPersonalField has a maximum length of 5000. Attempted value: {value}");
                _BackPersonalField = value;
            }
        }
        private string _BackPersonalField = "";
        [Required]
        [StringLength(300)]
        public string[] Tags { get; set; } = new string[0];
        public string TsvHelper { get; set; }
        public NpgsqlTsVector Tsv { get; set; }

        [ForeignKey("ExampleId")]
        [InverseProperty("CardExamples")]
        public virtual ExampleEntity Example { get; set; }
        [ForeignKey("RevisionId")]
        [InverseProperty("Cards")]
        public virtual RevisionEntity Revision { get; set; }
        public virtual ExampleEntity ExampleNavigation { get; set; }
        [ForeignKey("CardSettingId")]
        [InverseProperty("Cards")]
        public virtual CardSettingEntity CardSetting { get; set; }
        [ForeignKey("DeckId")]
        [InverseProperty("Cards")]
        public virtual DeckEntity Deck { get; set; }
        [ForeignKey("ConceptId")]
        [InverseProperty("Cards")]
        public virtual ConceptEntity Concept { get; set; }
        [ForeignKey("UserId")]
        [InverseProperty("Cards")]
        public virtual UserEntity User { get; set; }
        [InverseProperty("Card")]
        public virtual ICollection<HistoryEntity> Histories { get; set; }
    }
}
