namespace CardOverflow.Server {
  public static class UserSummary {
    public static Domain.Summary.User init = null;
    public static bool isAuthed(Domain.Summary.User summary) => summary != init;
  }
}
