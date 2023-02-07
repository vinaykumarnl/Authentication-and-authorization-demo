using System.ComponentModel.DataAnnotations;

namespace Authentication_and_authorization_demo.Models.Dto
{
    public class LoginDto
    {
        
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }
}
