using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;
using System;

namespace CardOverflow.Debug {
  public static class Diff {
    // https://github.com/mmanela/diffplex#sample-code
    public static void ToConsole(string before, string after) {
      var diff = InlineDiffBuilder.Diff(before, after);

      var savedColor = Console.ForegroundColor;
      foreach (var line in diff.Lines) {
        switch (line.Type) {
          case ChangeType.Inserted:
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("+ ");
            break;
          case ChangeType.Deleted:
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("- ");
            break;
          default:
            Console.ForegroundColor = ConsoleColor.Gray; // compromise for dark or light background
            Console.Write("  ");
            break;
        }

        Console.WriteLine(line.Text);
      }
      Console.ForegroundColor = savedColor;
    }

  }
}
