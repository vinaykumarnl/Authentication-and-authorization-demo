using Authentication_and_authorization_demo.AppDbContext;
using Authentication_and_authorization_demo.Models;
using Authentication_and_authorization_demo.Models.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Authentication_and_authorization_demo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FoodController : ControllerBase
    {
        private readonly FoodDbContext context;
        public FoodController(FoodDbContext _context)
        {
            context = _context;
        }

        [HttpGet]
        public async Task<ActionResult<object>> GetFood()
        {
            var foods =await context.Foods.ToListAsync();
            return foods;
        }

        [HttpPost]
        public async Task<ActionResult<object>> AddFood(FoodDto food)
        {
            Food f1=new Food()
            {
                Name = food.Name,
                Rating = food.Rating,
            };
            var f =await context.Foods.AddAsync(f1);
            return f;
        }
    }
}
