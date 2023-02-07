using System.ComponentModel.DataAnnotations;

namespace Authentication_and_authorization_demo.Models.Dto
{
    public class RequestToken
    {
        [Required]
        public string Token { get; set; }   
        [Required]
        public string RefreshToken { get; set; }
    }
}
