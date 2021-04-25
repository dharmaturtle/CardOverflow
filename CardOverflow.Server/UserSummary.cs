namespace CardOverflow.Server {
  public static class UserSummary {
    public static Domain.User.Events.Summary init = null;
    public static bool isAuthed(Domain.User.Events.Summary summary) => summary != init;
  }
}
