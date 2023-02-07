using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Authentication_and_authorization_demo.Models
{
    public class User:IdentityUser
    {
        [Required]
        public string Type { get; set; }
    }
}
