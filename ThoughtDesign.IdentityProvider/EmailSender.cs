using Microsoft.AspNetCore.Identity.UI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThoughtDesign.IdentityProvider {
  public class EmailSender : IEmailSender {
    
    public Task SendEmailAsync(string email, string subject, string htmlMessage) {
      return Task.CompletedTask; // highTODO fix
    }

  }
}
