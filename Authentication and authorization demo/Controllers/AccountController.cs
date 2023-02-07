using Authentication_and_authorization_demo.AppDbContext;
using Authentication_and_authorization_demo.Configuration;
using Authentication_and_authorization_demo.Models;
using Authentication_and_authorization_demo.Models.Dto;
using Authentication_and_authorization_demo.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Authentication_and_authorization_demo.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<IdentityUser> userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly TokenValidationParameters _tokenValidationParameters;
        private readonly IConfiguration Config;
        ResponseDto responseDto;
        private readonly IUserService service;
        private readonly FoodDbContext context;

        public AccountController(
            UserManager<IdentityUser> _userManager, 
            IConfiguration _jwtConfig, 
            RoleManager<IdentityRole> roleManager,
            IUserService _service, FoodDbContext _context, TokenValidationParameters tokenValidationParameters)
        {
            service= _service;
            userManager = _userManager;
            Config = _jwtConfig;
            responseDto= new ResponseDto();
            _roleManager = roleManager;
            context= _context;
            _tokenValidationParameters = tokenValidationParameters;
        }


        [HttpPost]
        [Route("Register")]
        public async Task<object> Register([FromBody] RegisterDto register)
        {
            if (!_roleManager.RoleExistsAsync(Helper.Helper.Admin).GetAwaiter().GetResult())
            {
                await _roleManager.CreateAsync(new IdentityRole(Helper.Helper.Admin));
                await _roleManager.CreateAsync(new IdentityRole(Helper.Helper.User));
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            else
            {
                var userByUsername = await userManager.FindByNameAsync(register.UserName);
                var userbyemail=await userManager.FindByEmailAsync(register.Email);
                if (userByUsername == null && userbyemail == null)
                {
                    var user = new User()
                    {
                        UserName = register.UserName,
                        Email = register.Email,
                        Type="User"
                        
                    };
                    var result=await userManager.CreateAsync(user, register.Password);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(user, "User");
                        var token=await GenerateToken(user);
                        return token;
                        // responseDto.Result = new
                        //{
                        //    Token = token,
                        //    User_Id = user.Id,
                        //    Type=user.Type
                        //};
                        
                    }
                    else
                    {
                        return BadRequest("Server error");
                    }
                }else if(userByUsername != null)
                {
                    return BadRequest("UserName already exists");
                }
                else if (userbyemail != null)
                {
                    return BadRequest("Email already exists");
                }
                return responseDto;
            }
            
        }


        private async Task<ActionResult<AuthResult>> GenerateToken(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(Config.GetSection("JwtConfig:Secret").Value);
            

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("Id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTime.Now.ToUniversalTime().ToString()),
                   new Claim(ClaimTypes.Role, user.Type.ToString())
                }),
                Expires = DateTime.UtcNow.Add(TimeSpan.Parse(Config.GetSection("JwtConfig:ExpiryTimeFrame").Value)),
                SigningCredentials=new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256),
            };

            var token=jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken=jwtTokenHandler.WriteToken(token);
            
            var refreshToken = new RefreshToken()
            {
                JwtId=token.Id,
                UserId=user.Id,
                Token = RandomStringGenerator(23),
                IsUsed=false,
                IsRevoked=false,
                CreatedDate=DateTime.Now,
                ExpireDate=DateTime.Now.AddMinutes(3)
            };
            
            await context.refreshTokens.AddAsync(refreshToken);
            await context.SaveChangesAsync();
            return new AuthResult
            {
                Token=jwtToken,
                RefreshToken=refreshToken.Token,
                Result=true
            };
        }

        [HttpPost]
        [Route("Login")]
        public async Task<ActionResult<Object>> Login([FromBody] LoginDto loginDto)
        {
            if (!ModelState.IsValid)
            {
                responseDto.IsSuccess = false;
                responseDto.DisplayMessage = "Invalid model state";
                responseDto.Result = Unauthorized();
                return responseDto;
            }
            else
            {
                var user=await userManager.FindByEmailAsync(loginDto.Email);
                if (user == null)
                {
                    responseDto.DisplayMessage = "User does not exists";
                    responseDto.IsSuccess=false;
                    responseDto.Result=Unauthorized();
                    return responseDto;
                }
                var checkUser=await userManager.CheckPasswordAsync(user, loginDto.Password);
                if (checkUser == false)
                {
                    responseDto.IsSuccess=false;
                    responseDto.DisplayMessage = "Password is incorrect";
                    responseDto.Result=Unauthorized();
                    return responseDto ;
                }
                var newuser=await service.GetUser(user.Id);

                var jwttoken = GenerateToken(newuser).Result.Value;
                responseDto.DisplayMessage = "Logged in successfully";
                responseDto.Result = new
                {
                    Token=jwttoken.Token,
                    RefreshToken=jwttoken.RefreshToken,
                    UserName=newuser.UserName,
                    UserId=newuser.Id,
                    Result=true,
                    Type=newuser.Type,
                };
                return responseDto;

            }
        }

        private string RandomStringGenerator(int length)
        {
            var randomstring = new Random();
            var s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890_abcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(s,length).Select(x=>x[randomstring.Next(s.Length)]).ToArray());
        }

        [HttpPost]
        [Route("RefreshToken")]
        public async Task<ActionResult<object>> RefreshToken([FromBody] RequestToken requestToken)
        {
            if (ModelState.IsValid)
            {
                var result =await VerifyAndGenerateToken(requestToken);
                if (result == null)
                {
                    return BadRequest(new ResponseDto()
                    {
                        ErrorMessages =new List<string>()
                        {
                            "InvalidToken"
                        },
                        Result = false
                    });
                }
                return result;
            }
                return BadRequest(new ResponseDto()
                {
                    ErrorMessages = new List<string>()
                    {
                        "Invalid Parameters"
                    },
                    Result = false
                });
            
        }

        private async Task<ActionResult<object>> VerifyAndGenerateToken(RequestToken requestToken)
        {
            var jwtTokenHandler=new JwtSecurityTokenHandler();
            try
            {
                
                _tokenValidationParameters.ValidateLifetime = false;
                var tokenInValidation = jwtTokenHandler.ValidateToken(requestToken.Token, _tokenValidationParameters, out var validatedToken);
                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (!result)
                    {
                        return null;
                    }
                   

                }
                var utcExpiryDate = long.Parse(tokenInValidation.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
                var expirydate = UnixTimeStampToDateTime(utcExpiryDate);
                if (expirydate > DateTime.Now)
                {
                    return new ResponseDto()
                    {
                        ErrorMessages = new List<string>()
                            {
                                "Token is Expired"
                            },
                        Result = false,

                    };
                }
                var tokenResult = await context.refreshTokens.FirstOrDefaultAsync(x => x.Token == requestToken.RefreshToken);
                if (tokenResult == null)
                {
                    return new ResponseDto()
                    {
                        ErrorMessages = new List<string>()
                            {
                                "Invalid Token"
                            },
                        Result = false,
                    };
                }
                if (tokenResult.IsUsed)
                {
                    return new ResponseDto()
                    {
                        ErrorMessages = new List<string>()
                            {
                                "Invalid Token"
                            },
                        Result = false,
                    };
                }
                if (tokenResult.IsRevoked)
                {
                    return new ResponseDto()
                    {
                        ErrorMessages = new List<string>()
                            {
                                "Invalid Token"
                            },
                        Result = false,
                    };
                }

                var jti = tokenInValidation.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
                if (tokenResult.JwtId != jti)
                {
                    return new ResponseDto()
                    {
                        ErrorMessages = new List<string>()
                            {
                                "Invalid Token"
                            },
                        Result = false,
                    };
                }

                tokenResult.IsUsed = true;
                context.refreshTokens.Update(tokenResult);
                await context.SaveChangesAsync();
                var dbUser = await context.Users.FirstOrDefaultAsync(x => x.Id == tokenResult.UserId);
                return await GenerateToken(dbUser);

            }
            catch (Exception ex)
            {
                return BadRequest(new ResponseDto()
                {
                    ErrorMessages = new List<string>()
                            {
                                "Server Error"
                            },
                    Result = false,
                });
            }
        }

        private DateTime UnixTimeStampToDateTime(long utcExpiryDate)
        {
            var datetime=new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            datetime=datetime.AddSeconds(utcExpiryDate).ToUniversalTime();
            return datetime;
        }
    }
}
