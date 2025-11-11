using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Threading.Tasks;

namespace Weatherapibackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestDbController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestDbController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            Console.WriteLine($"[DEBUG] Using connection string: {connectionString}");

            try
            {
                await using var connection = new NpgsqlConnection(connectionString);
                await connection.OpenAsync();

                // Simple query to verify connection
                await using var command = new NpgsqlCommand("SELECT NOW();", connection);
                var result = await command.ExecuteScalarAsync();

                return Ok(new
                {
                    status = "✅ Connection successful",
                    message = $"Connected to Supabase Postgres. Server time: {result}"
                });
            }
            catch (PostgresException pgEx)
            {
                Console.WriteLine($"[Postgres ERROR] {pgEx.MessageText}");
                return StatusCode(500, new
                {
                    status = "❌ Connection failed",
                    error = pgEx.MessageText
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
                return StatusCode(500, new
                {
                    status = "❌ Connection failed",
                    error = ex.Message
                });
            }
        }
    }
}
