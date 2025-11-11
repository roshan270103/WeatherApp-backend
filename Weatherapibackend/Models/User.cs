using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Weatherapibackend.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Username")]
        public string Username { get; set; }

        [BsonElement("PasswordHash")]
        public string PasswordHash { get; set; }

        [BsonElement("FavoriteCities")]
        public List<FavoriteCity> FavoriteCities { get; set; } = new List<FavoriteCity>();
    }

    public class FavoriteCity
    {
        [BsonElement("CityName")]
        public string CityName { get; set; }

        [BsonElement("Notes")]
        public string? Notes { get; set; }
    }
}
