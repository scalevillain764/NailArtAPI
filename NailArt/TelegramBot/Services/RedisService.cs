using TelegramBot.BotHandlers;
using TelegramBot.DTO.Bookings;
using TelegramBot.DTO.Clients;
using IRedisService = TelegramBot.Interfaces.IRedisService;
using StackExchange.Redis;
using System.Text.Json;
namespace TelegramBot.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _redis;
        private static readonly Dictionary<Type, string> _keyMappingsProcesses = new()
        {
            { typeof(ContactProcess), "contact:process" },         
            { typeof(BookingProcess), "booking:process" },
        };
        private static readonly Dictionary<Type, string> _keyMappingsDrafts = new()
        {       
            { typeof(ShareContactDraft), "contact:creation_draft" },
            { typeof(EditContactDraft), "contact:edit_draft" },
            { typeof(CreateBookingDraft), "booking:creation_draft" }
        };
        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis.GetDatabase();
        }
        
        private async Task DeleteAsync<T>(long userId, Dictionary<Type, string> keyMappings)
        {
            if (keyMappings.TryGetValue(typeof(T), out string prefix))
                await _redis.KeyDeleteAsync($"{prefix}:{userId}");
        }

        private async Task<T?> GetAsync<T>(long userId, Dictionary<Type, string> keyMappings) where T : class
        {
            if (keyMappings.TryGetValue(typeof(T), out string prefix))
            {
                var cachedValue = await _redis.StringGetAsync($"{prefix}:{userId}");

                if (cachedValue.IsNullOrEmpty)
                    return null;

                var deserializedProcess = JsonSerializer.Deserialize<T>((string)cachedValue!);

                return deserializedProcess;
            }

            return null;
        }
        private async Task SaveAsync<T>(long userId, T value, Dictionary<Type, string> keyMappings)
        {
            if (keyMappings.TryGetValue(typeof(T), out string prefix))
            {
                var serializedValue = JsonSerializer.Serialize(value);
                await _redis.StringSetAsync($"{prefix}:{userId}", serializedValue);
            }
        }

        public Task<T?> GetProcessAsync<T>(long userId) where T : class
            => GetAsync<T>(userId, _keyMappingsProcesses);

        public Task SaveProcessAsync<T>(long userId, T process)
            => SaveAsync(userId, process, _keyMappingsProcesses);

        public Task<T?> GetDraftAsync<T>(long userId) where T : class
            => GetAsync<T>(userId, _keyMappingsDrafts);

        public Task SaveDraftAsync<T>(long userId, T draft)
            => SaveAsync<T>(userId, draft, _keyMappingsDrafts);
        public Task DeleteProcessAsync<T>(long userId)
            => DeleteAsync<T>(userId, _keyMappingsProcesses);

        public Task DeleteDraftAsync<T>(long userId)
          => DeleteAsync<T>(userId, _keyMappingsDrafts);
    }
}