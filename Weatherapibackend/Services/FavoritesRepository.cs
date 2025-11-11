using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Weatherapibackend.Services
{
    public class FavoritesRepository
    {
        private readonly string _connectionString;

        public FavoritesRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(_connectionString))
                throw new InvalidOperationException("Database connection string 'Default' is missing in appsettings.json");
        }

        // ✅ Add Favorite City
        public async Task AddFavorite(string userId, string city, string? notes = null)
        {
            Console.WriteLine($"[DEBUG] Attempting to add favorite city '{city}' for user {userId}...");

            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("[ERROR] userId cannot be empty.");
                throw new ArgumentException("userId cannot be empty.");
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                Console.WriteLine("[ERROR] Invalid userId format — must be a GUID.");
                throw new ArgumentException("Invalid userId format — must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                Console.WriteLine("[ERROR] City name cannot be empty.");
                throw new ArgumentException("City name cannot be empty.");
            }

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                Console.WriteLine("[DEBUG] Database connection opened.");

                // Check for duplicates
                var checkSql = "SELECT COUNT(*) FROM favorite_cities WHERE user_id=@uid AND city_name=@city";
                await using (var checkCmd = new NpgsqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("uid", parsedUserId);
                    checkCmd.Parameters.AddWithValue("city", city);

                    var count = (long)await checkCmd.ExecuteScalarAsync();
                    Console.WriteLine($"[DEBUG] Existing count for '{city}': {count}");

                    if (count > 0)
                    {
                        Console.WriteLine($"[INFO] City '{city}' already exists for user {userId}.");
                        throw new Exception($"'{city}' is already in favorites.");
                    }
                }

                // Insert new city
                var insertSql = "INSERT INTO favorite_cities (user_id, city_name, notes) VALUES (@uid, @city, @notes)";
                await using var cmd = new NpgsqlCommand(insertSql, conn);
                cmd.Parameters.AddWithValue("uid", parsedUserId);
                cmd.Parameters.AddWithValue("city", city);
                cmd.Parameters.AddWithValue("notes", notes ?? (object)DBNull.Value);

                var rows = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"[DEBUG] Inserted {rows} row(s) for city '{city}'.");
            }
            catch (PostgresException pgEx)
            {
                Console.WriteLine($"[ERROR] PostgreSQL Code: {pgEx.SqlState}");
                Console.WriteLine($"[ERROR] PostgreSQL Message: {pgEx.MessageText}");
                Console.WriteLine($"[ERROR] Detail: {pgEx.Detail}");
                Console.WriteLine($"[ERROR] Hint: {pgEx.Hint}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] General Exception: {ex.Message}");
                Console.WriteLine($"[STACKTRACE] {ex.StackTrace}");
                throw;
            }

        }

        // ✅ Get all favorites for a user
        public async Task<List<string>> GetFavoriteCities(string userId)
        {
            Console.WriteLine($"[DEBUG] Fetching favorites for user {userId}...");
            var result = new List<string>();

            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("[ERROR] userId cannot be empty.");
                throw new ArgumentException("userId cannot be empty.");
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                Console.WriteLine("[ERROR] Invalid userId format — must be a GUID.");
                throw new ArgumentException("Invalid userId format — must be a valid GUID.");
            }

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                Console.WriteLine("[DEBUG] Database connection opened.");

                var sql = "SELECT city_name FROM favorite_cities WHERE user_id=@uid ORDER BY city_name ASC";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", parsedUserId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var city = reader.GetString(0);
                    Console.WriteLine($"[DEBUG] Found favorite city: {city}");
                    result.Add(city);
                }
            }
            catch (PostgresException pgEx)
            {
                Console.WriteLine($"[ERROR] PostgreSQL: {pgEx.MessageText}");
                throw new Exception($"Database error: {pgEx.MessageText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to fetch favorites: {ex.Message}");
                throw new Exception("Failed to fetch favorite cities", ex);
            }

            Console.WriteLine($"[DEBUG] Returning {result.Count} favorites for user {userId}.");
            return result;
        }

        // ✅ Remove Favorite City
        public async Task RemoveFavorite(string userId, string city)
        {
            Console.WriteLine($"[DEBUG] Removing favorite city '{city}' for user {userId}...");

            if (string.IsNullOrWhiteSpace(userId))
            {
                Console.WriteLine("[ERROR] userId cannot be empty.");
                throw new ArgumentException("userId cannot be empty.");
            }

            if (!Guid.TryParse(userId, out Guid parsedUserId))
            {
                Console.WriteLine("[ERROR] Invalid userId format — must be a GUID.");
                throw new ArgumentException("Invalid userId format — must be a valid GUID.");
            }

            if (string.IsNullOrWhiteSpace(city))
            {
                Console.WriteLine("[ERROR] City name cannot be empty.");
                throw new ArgumentException("City name cannot be empty.");
            }

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                Console.WriteLine("[DEBUG] Database connection opened.");

                var sql = "DELETE FROM favorite_cities WHERE user_id=@uid AND city_name=@city";
                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("uid", parsedUserId);
                cmd.Parameters.AddWithValue("city", city);

                var rows = await cmd.ExecuteNonQueryAsync();
                Console.WriteLine($"[DEBUG] Deleted {rows} row(s).");

                if (rows == 0)
                    throw new Exception($"'{city}' was not found in favorites.");
            }
            catch (PostgresException pgEx)
            {
                Console.WriteLine($"[ERROR] PostgreSQL: {pgEx.MessageText}");
                throw new Exception($"Database error: {pgEx.MessageText}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to remove favorite city: {ex.Message}");
                throw new Exception("Failed to remove favorite city", ex);
            }
        }
    }
}
