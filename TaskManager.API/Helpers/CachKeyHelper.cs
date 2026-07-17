using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskManager.API.Helpers
{
    public class CachKeyHelper
    {
        private static readonly JsonSerializerOptions jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        public static string GenerateKey<T>(string prefix,long version,T value)
        {
            var json = JsonSerializer.Serialize(value,jsonOptions);
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(json);
            var hashBytes = sha256.ComputeHash(bytes);
            var hash = Convert.ToHexString(hashBytes);
            return $"{prefix}:{version}:query{hash}";
        }
    }
}
