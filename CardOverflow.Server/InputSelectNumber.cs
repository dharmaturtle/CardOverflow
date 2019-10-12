using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;

namespace CardOverflow.Server {
  // https://github.com/aspnet/AspNetCore/issues/11181#issuecomment-506288035
  public class InputSelectNumber<T> : InputSelect<T> {
    protected override bool TryParseValueFromString(string value, out T result, out string validationErrorMessage) {
      if (typeof(T) == typeof(int)) {
        if (int.TryParse(value, out var resultInt)) {
          result = (T) (object) resultInt;
          validationErrorMessage = null;
          return true;
        } else {
          result = default;
          validationErrorMessage = "The chosen value is not a valid number.";
          return false;
        }
      } else {
        return base.TryParseValueFromString(value, out result, out validationErrorMessage);
      }
    }
  }
}
