using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using CardOverflow.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ThoughtDesign.IdentityProvider.Areas.Identity.Data;
using CardOverflow.Api;
using Microsoft.EntityFrameworkCore;

namespace ThoughtDesign.IdentityProvider.Areas.Identity.Pages.Account {
  [AllowAnonymous]
  public class RegisterModel : PageModel {
    private readonly CardOverflowDb _db;
    private readonly SignInManager<ThoughtDesignUser> _signInManager;
    private readonly UserManager<ThoughtDesignUser> _userManager;
    private readonly ILogger<RegisterModel> _logger;
    private readonly IEmailSender _emailSender;

    public RegisterModel(
        UserManager<ThoughtDesignUser> userManager,
        SignInManager<ThoughtDesignUser> signInManager,
        CardOverflowDb db,
        ILogger<RegisterModel> logger,
        IEmailSender emailSender) {
      _userManager = userManager;
      _signInManager = signInManager;
      _db = db;
      _logger = logger;
      _emailSender = emailSender;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public class InputModel {
      [Required]
      [StringLength(32, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
      [Display(Name = "Display Name")]
      public string DisplayName { get; set; }

      [Required]
      [EmailAddress]
      [Display(Name = "Email")]
      public string Email { get; set; }

      [Required]
      [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
      [DataType(DataType.Password)]
      [Display(Name = "Password")]
      public string Password { get; set; }

      [DataType(DataType.Password)]
      [Display(Name = "Confirm password")]
      [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
      public string ConfirmPassword { get; set; }

      [DataType(DataType.Password)]
      [Display(Name = "Invite code")]
      public string InviteCode { get; set; }
    }

    public async Task OnGetAsync(string returnUrl = null) {
      ReturnUrl = returnUrl;
      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null) {
      returnUrl = returnUrl ?? Url.Content("~/");
      ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
      var key = await _db.AlphaBetaKey.SingleOrDefaultAsync(x => x.Key == Input.InviteCode && !x.IsUsed);
      if (ModelState.IsValid && key != null) {
        var user = new ThoughtDesignUser { UserName = Input.Email, Email = Input.Email };
        var result = await _userManager.CreateAsync(user, Input.Password);
        if (result.Succeeded) {
          _logger.LogInformation("User created a new account with password.");

          var defaultSetting = CardSettingsRepository.defaultCardSettingsEntity.Invoke(0);
          var cardOverflowUser = new UserEntity {
            Id = user.Id,
            DisplayName = Input.DisplayName,
            CardSettings = new List<CardSettingEntity> { defaultSetting },
            Filters = new List<FilterEntity> { new FilterEntity { Name = "All", Query = "" } },
            User_TemplateInstances = _db.TemplateInstance
              .Where(x => x.Template.AuthorId == 2)
              .Select(x => x.Id)
              .ToList()
              .Select(id => new User_TemplateInstanceEntity { TemplateInstanceId = id, DefaultCardSetting = defaultSetting })
              .ToList(),
          };
          key.IsUsed = true;
          _db.User.Add(cardOverflowUser);
          await _db.SaveChangesAsync();
          cardOverflowUser.DefaultCardSetting = defaultSetting;
          await _db.SaveChangesAsync();
          var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
          code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
          var callbackUrl = Url.Page(
              "/Account/ConfirmEmail",
              pageHandler: null,
              values: new { area = "Identity", userId = user.Id, code = code },
              protocol: Request.Scheme);

          await _emailSender.SendEmailAsync(Input.Email, "Confirm your email",
              $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

          if (_userManager.Options.SignIn.RequireConfirmedAccount) {
            return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
          } else {
            await _signInManager.SignInAsync(user, isPersistent: false);
            return LocalRedirect(returnUrl);
          }
        }
        foreach (var error in result.Errors) {
          ModelState.AddModelError(string.Empty, error.Description);
        }
      }

      if (key == null) {
        ModelState.AddModelError(string.Empty, "Invalid invite code.");
      }

      // If we got this far, something failed, redisplay form
      return Page();
    }
  }
}