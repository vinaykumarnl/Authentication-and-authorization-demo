using Authentication_and_authorization_demo.AppDbContext;
using Authentication_and_authorization_demo.Models;
using Microsoft.EntityFrameworkCore;

namespace Authentication_and_authorization_demo.Service
{
    public class UserService:IUserService
    {
        private readonly FoodDbContext context;
        public UserService(FoodDbContext _context)
        {
            context = _context;
        }

        public async Task<object> GetAllUser()
        {
            return await context.Users.ToListAsync();
        }

        public async Task<User> GetUser(string id)
        {
            return await context.Users.FindAsync(id);
        }
    }
}
