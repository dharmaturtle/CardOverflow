namespace CardOverflow.Entity {
  public enum MemorizationStateAndCardStateEnum : byte {
    NewNormal = 0,
    NewSchedulerBuried = 1,
    NewUserBuried = 2,
    NewSuspended = 3,
    LearningNormal = 4,
    LearningSchedulerBuried = 5,
    LearningUserBuried = 6,
    LearningSuspended = 7,
    MatureNormal = 8,
    MatureSchedulerBuried = 9,
    MatureUserBuried = 10,
    MatureSuspended = 11,
  }
}
