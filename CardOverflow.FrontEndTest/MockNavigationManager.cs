using Blazored.Toast;

using static Bunit.ComponentParameterFactory;
using CardOverflow.Debug;
using CardOverflow.Pure;
using CardOverflow.Server.Pages;
using CardOverflow.Server.Pages.Deck;
using CardOverflow.Test;
using FsCheck;
using FsCheck.Xunit;
using System;
using System.Linq;
using Xunit;
using Bunit;
using static CardOverflow.FrontEndTest.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using CardOverflow.Server;
using ThoughtDesign.WebLibrary;
using CardOverflow.Api;
using BlazorStrap;
using FluentValidation;
using CardOverflow.Sanitation;
using System.Threading.Tasks;
using CardOverflow.Entity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CardOverflow.FrontEndTest {

  public class MockNavigationManager : NavigationManager {
    protected override void NavigateToCore(string uri, bool forceLoad) {
      throw new NotImplementedException();
    }
  }
}
