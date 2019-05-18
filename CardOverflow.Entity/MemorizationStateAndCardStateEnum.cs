namespace CardOverflow.Entity {
  public enum MemorizationStateAndCardStateEnum : byte {
    NewNormal,
    NewSchedulerBuried,
    NewUserBuried,
    NewSuspended,
    LearningNormal,
    LearningSchedulerBuried,
    LearningUserBuried,
    LearningSuspended,
    MatureNormal,
    MatureSchedulerBuried,
    MatureUserBuried,
    MatureSuspended,
    LapsedNormal,
    LapsedSchedulerBuried,
    LapsedUserBuried,
    LapsedSuspended,
  }
}
