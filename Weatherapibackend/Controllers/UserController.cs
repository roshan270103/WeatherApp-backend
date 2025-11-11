using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Supabase;
using Weatherapibackend.Models;

namespace Weatherapibackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public UserController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // POST /api/user/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var session = await _supabase.Auth.SignUp(request.Email, request.Password);
                if (session?.User != null)
                {
                    return Ok(new
                    {
                        id = session.User.Id,
                        email = session.User.Email,
                        message = "User registered successfully"
                    });
                }
                return BadRequest(new { error = "Registration failed — unknown reason." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // POST /api/user/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var session = await _supabase.Auth.SignIn(request.Email, request.Password);

                if (session?.User != null)
                {
                    return Ok(new
                    {
                        id = session.User.Id,
                        email = session.User.Email,
                        accessToken = session.AccessToken,  // ✅ REAL JWT for backend
                        message = "Login successful"
                    });
                }

                return Unauthorized(new { error = "Invalid credentials." });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        // DTOs
        public class RegisterRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }

        public class LoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }
        }
    }
}
