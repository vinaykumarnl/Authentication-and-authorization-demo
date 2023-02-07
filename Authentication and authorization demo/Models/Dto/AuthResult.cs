namespace Authentication_and_authorization_demo.Models.Dto
{
    public class AuthResult
    {
        public string Token { get; set; }    
        public string RefreshToken { get; set; }
        public bool Result { get; set; }
    }
}
