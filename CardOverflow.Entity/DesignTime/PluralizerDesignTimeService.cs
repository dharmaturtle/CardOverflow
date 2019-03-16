using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace CardOverflow.Entity.DesignTime {
  /// <summary>
  /// If there is a class that implements IDesignTimeServices, 
  /// then the EF Tools will call it to allow custom services 
  /// to be registered.
  /// </summary>
  public class PluralizerDesignTimeService : IDesignTimeServices {

    public void ConfigureDesignTimeServices(IServiceCollection services) {
      services.AddSingleton<IPluralizer, Pluralizer>();
    }

  }
}
