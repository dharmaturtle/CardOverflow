using Humanizer;
using Microsoft.EntityFrameworkCore.Design;

namespace CardOverflow.Entity.DesignTime {
  public class Pluralizer : IPluralizer {

    public string Pluralize(string name) =>
      name.Pluralize() ?? name;

    public string Singularize(string name) =>
      name.Singularize() ?? name;

  }
}
