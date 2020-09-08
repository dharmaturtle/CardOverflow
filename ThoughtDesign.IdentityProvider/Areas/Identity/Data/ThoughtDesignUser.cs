using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace ThoughtDesign.IdentityProvider.Areas.Identity.Data {
  public class ThoughtDesignUser : IdentityUser<Guid> {

    [Required]
    [StringLength(32)]
    [PersonalData]
    public string DisplayName { get; set; }

  }
}
