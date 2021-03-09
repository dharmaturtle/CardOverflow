using CardOverflow.Server.Data;
using Fluxor;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CardOverflow.Server.Store {

  public record WeatherState {
    public bool IsLoading { get; init; }
    public IEnumerable<WeatherForecast> Forecasts { get; init; }
  }

  public record GetWeatherAction { }
  public record GetWeatherOutcome {
    public IEnumerable<WeatherForecast> Forecasts { get; init; }
  }

  public class WeatherStateFeature : AutoNameFeature<WeatherState> {
    protected override WeatherState GetInitialState() => new() {
      IsLoading = true,
      Forecasts = null,
    };
  }

  public class WeatherStateEffects {
    private readonly WeatherForecastService _forecastService;

    public WeatherStateEffects(WeatherForecastService forecastService) {
      _forecastService = forecastService;
    }

    [EffectMethod] public async Task _1
      (GetWeatherAction _, IDispatcher dispatcher) {
      var forecasts = await _forecastService.GetForecastAsync(DateTime.Now);
      dispatcher.Dispatch(new GetWeatherOutcome() { Forecasts = forecasts });
    }
  }

  public static class WeatherStateReducer {

    [ReducerMethod] public static WeatherState _1
      (WeatherState state, GetWeatherAction _) => state with {
      IsLoading = true,
    };

    [ReducerMethod] public static WeatherState _2
      (WeatherState _, GetWeatherOutcome outcome) => new() {
      IsLoading = false,
      Forecasts = outcome.Forecasts
    };

  }

}
