using TelegramBot.BotFlows;
namespace TelegramBot.BotSessions
{
    public class BotSession
    {
        public BotFlow currentFlow { get; set; }
        public BotSession()
        {
            currentFlow = BotFlow.Menu;
        }
    }
}