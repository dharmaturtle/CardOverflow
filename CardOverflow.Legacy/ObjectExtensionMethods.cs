using System;
using System.Text.RegularExpressions;

namespace CardOverflow.Debug {
  public static class ObjectExtensionMethods {

    public static TInput Dump<TInput>(this TInput input, string id = "") {
#if DEBUG
      if (string.IsNullOrWhiteSpace(id)) {
        Console.WriteLine(MyObjectDumper.Dump(input));
      } else {
        Console.WriteLine(id + ": " + MyObjectDumper.Dump(input));
      }
#endif
      return input;
    }

    public static TInput Dump<TInput, TDump>(this TInput input, Func<TInput, TDump> getValue, string id = "") {
      getValue(input).Dump(id);
      return input;
    }

    private static readonly DumpOptions DumpOptions = new DumpOptions {
      DumpStyle = DumpStyle.CSharp,
      LineBreakChar = "\r\n",
      IndentSize = 4,
    };

    public static TInput CDump<TInput>(this TInput input) {
      Console.WriteLine(ObjectDumper.Dump(input, DumpOptions));
      return input;
    }

    public static TInput FDump<TInput>(this TInput input) {
      var s = ObjectDumper.Dump(input, DumpOptions);
      s = s.Replace("var", "let");
      s = s.Replace(";\r\n", String.Empty);
      s = s.Replace("new ", String.Empty);
      s = Regex.Replace(s, @"[\r\n]+\s+{", "("); // replace { with (
      s = Regex.Replace(s, @"[\r\n]+ +\},*", ")"); // replace } with )
      s = Regex.Replace(s, @"\([\r\n\s]+\)", "()"); // remove gap between ( )
      s = s.Replace("\r\n    ", "\r\n        "); // tab everything over (except first line)
      s = s.Insert(s.IndexOf('=') + 1, "\r\n   "); // new line after first =
      var listPattern = @"\S*List<\S+>\(";
      if (Regex.IsMatch(s, listPattern)) { // Matches FSharpList<> and List<>, replaces with []
        s = Regex.Replace(s, listPattern, "[");
        var i = s.LastIndexOf('}');
        s = s.Remove(i).Insert(i, "    ]");
      }
      Console.WriteLine(s);
      return input;
    }

  }
}
