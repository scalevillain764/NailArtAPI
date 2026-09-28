using TelegramBot.BotFlows;
namespace TelegramBot.BotSessions
{
    public class BotSession
    {
        public BotFlow currentFlow { get; set; }
        public int currentBookingsPage { get; set; }
        public BotSession(BotFlow flow)
        {
            currentFlow = flow;
            currentBookingsPage = 1;
        }
    }
}