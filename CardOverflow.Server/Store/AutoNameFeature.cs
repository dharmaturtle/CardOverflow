using Fluxor;

namespace CardOverflow.Server.Store {

  public abstract class AutoNameFeature<T>: Feature<T> {
    public override string GetName() => typeof(T).Name;
  }

}
