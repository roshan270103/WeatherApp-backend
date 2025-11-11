using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Weatherapibackend.Services;

namespace Weatherapibackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // ✅ Protects all routes with Supabase JWT
    public class FavoritesController : ControllerBase
    {
        private readonly FavoritesRepository _repository;

        public FavoritesController(FavoritesRepository repository)
        {
            _repository = repository;
        }

        // Helper to extract user id
        private string? GetUserIdFromJwt()
        {
            Console.WriteLine("[DEBUG] ---- ALL USER CLAIMS ----");
            foreach (var claim in User.Claims)
                Console.WriteLine($"Type: {claim.Type}   Value: {claim.Value}");

            // Try standard JWT 'sub' claim first
            var userId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            // Fallback: the .NET nameidentifier claim type
            if (string.IsNullOrWhiteSpace(userId))
                userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Console.WriteLine($"[RESULT] Will use userId = {userId}");
            return userId;
        }

        // ✅ GET /api/favorites/get
        [HttpGet("get")]
        public async Task<IActionResult> GetFavorites()
        {
            try
            {
                var userId = GetUserIdFromJwt();

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { Error = "Invalid or missing user token." });

                var cities = await _repository.GetFavoriteCities(userId);

                if (cities == null || cities.Count == 0)
                    return Ok(new { Message = "No favorite cities added yet.", Favorites = new List<string>() });

                return Ok(new { Message = "Favorite cities loaded successfully.", Favorites = cities });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] GetFavorites: {ex.Message}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // ✅ POST /api/favorites/add
        [HttpPost("add")]
        public async Task<IActionResult> AddFavorite([FromBody] FavoriteRequest request)
        {
            try
            {
                var userId = GetUserIdFromJwt();

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { Error = "Invalid or missing user token." });

                if (string.IsNullOrWhiteSpace(request.City))
                    return BadRequest(new { Error = "City name cannot be empty." });

                await _repository.AddFavorite(userId, request.City, request.Notes);
                return Ok(new { Message = $"'{request.City}' added to favorites." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 AddFavorite Exception: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // ✅ DELETE /api/favorites/remove
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFavorite([FromBody] FavoriteRequest request)
        {
            try
            {
                var userId = GetUserIdFromJwt();

                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { Error = "Invalid or missing user token." });

                if (string.IsNullOrWhiteSpace(request.City))
                    return BadRequest(new { Error = "City name cannot be empty." });

                await _repository.RemoveFavorite(userId, request.City);
                return Ok(new { Message = $"'{request.City}' removed from favorites." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 RemoveFavorite Exception: {ex.Message}");
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        // ✅ Request DTO
        public class FavoriteRequest
        {
            public string City { get; set; }
            public string? Notes { get; set; }
        }
    }
}
