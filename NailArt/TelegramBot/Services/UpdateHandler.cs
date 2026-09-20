using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Polling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramBot.Interfaces;
namespace TelgramBot.Services
{
    public class UpdateHandler : IMUpdateHandler
    {
        private readonly IEnumerable<IBotHandler> _handlers;
        public UpdateHandler(IEnumerable<IBotHandler> handlers)
        {
            _handlers = handlers;
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var handler = _handlers.FirstOrDefault(x => x.CanHandle(update));

            if (handler == null)
                return;

            await handler.HandleAsync(botClient, update, cancellationToken).ConfigureAwait(false);
        }

        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, HandleErrorSource source, CancellationToken cancellationToken)
        {
            // log later
            return Task.CompletedTask;
        }
    }
}