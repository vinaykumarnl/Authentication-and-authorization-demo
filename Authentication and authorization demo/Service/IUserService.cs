using Authentication_and_authorization_demo.Models;

namespace Authentication_and_authorization_demo.Service
{
    public interface IUserService
    {
          Task<object> GetAllUser();
        Task<User> GetUser(string id);
    }
}
