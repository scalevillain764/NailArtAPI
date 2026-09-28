using TelegramBot.BotHandlers;
using TelegramBot.DTO.Bookings;
using TelegramBot.DTO.Clients;
using TelegramBot.BotSessions;
using TelegramBot.BotFlows;
namespace TelegramBot.Interfaces
{
    public interface IRedisService
    {
        Task<T?> GetProcessAsync<T>(long userId) where T : class;
        Task SaveProcessAsync<T>(long userId, T process);
        Task<T?> GetDraftAsync<T>(long userId) where T : class;
        Task SaveDraftAsync<T>(long userId, T draft);
        Task DeleteProcessAsync<T>(long userId);
        Task DeleteDraftAsync<T>(long userId);
        Task<BotSession?> GetCurrentSession(long userId);
        Task SaveCurrentSession(long userId, BotSession curSession);
    }
}