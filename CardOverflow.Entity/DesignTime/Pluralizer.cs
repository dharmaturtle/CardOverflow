using Microsoft.EntityFrameworkCore.Design;

namespace CardOverflow.Entity.DesignTime {
  public class Pluralizer : IPluralizer {

    public string Pluralize(string name) => 
      Inflector.Inflector.Pluralize(name) ?? name;

    public string Singularize(string name) => 
      Inflector.Inflector.Singularize(name) ?? name;

  }
}
