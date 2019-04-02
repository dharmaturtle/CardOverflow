using System;
using System.Collections.Generic;

namespace CardOverflow.Entity
{
    public partial class CardOption
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int UserId { get; set; }
        public string NewCardsSteps { get; set; }
        public short NewCardsMaxPerDay { get; set; }
        public byte NewCardsGraduatingInterval { get; set; }
        public byte NewCardsEasyInterval { get; set; }
        public short NewCardsStartingEase { get; set; }
        public bool NewCardsBuryRelated { get; set; }
        public short MatureCardsMaxPerDay { get; set; }
        public short MatureCardsEasyBonus { get; set; }
        public short MatureCardsIntervalModifier { get; set; }
        public short MatureCardsMaximumInterval { get; set; }
        public bool MatureCardsBuryRelated { get; set; }
        public string LapsedCardsSteps { get; set; }
        public short LapsedCardsNewInterval { get; set; }
        public byte LapsedCardsMinimumInterval { get; set; }
        public byte LapsedCardsLeechThreshold { get; set; }
        public bool ShowAnswerTimer { get; set; }
        public bool AutomaticallyPlayAudio { get; set; }
        public bool ReplayQuestionAnswerAudioOnAnswer { get; set; }

        public virtual User User { get; set; }
    }
}
