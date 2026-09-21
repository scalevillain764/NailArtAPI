using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Drawing;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.DTO;
using TelegramBot.StateMachines;
using IBotHandler = TelegramBot.Interfaces.IBotHandler;
using TelegramBot.BotFlows;
namespace TelegramBot.BotHandlers
{
    public class CallbackQueryHandler : IBotHandler
    {
        private readonly IDatabase _redis;
        public CallbackQueryHandler(IConnectionMultiplexer connectionMultiplexer)
        {
            _redis = connectionMultiplexer.GetDatabase();
        }
        public bool CanHandle(Update update) => update.CallbackQuery is not null;
        public async Task HandleAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            var callback = update.CallbackQuery!;
            var userId = callback.From.Id;
            var chatId = callback.Message?.Chat.Id;

            if (chatId == null)
                return;

            var data = callback.Data;

            switch (data)
            {
                case "contact:add":
                    {
                        var cachedDraft = await _redis.StringGetAsync($"contact:{userId}");
                        ShareContactDraft? draft = null;
                        if(!cachedDraft.IsNullOrEmpty)
                        {
                            draft = JsonSerializer.Deserialize<ShareContactDraft>((string)cachedDraft!);

                            if (draft == null)
                                return;

                            string print = draft.State switch
                            {
                                ContactState.CreateEnterName => "имя",
                                ContactStateMachine.CreateEnterPhone => "номер телефона",
                                _ => throw new KeyNotFoundException("Что-то пошло не так")
                            };

                            await botClient.SendMessage(chatId, 
                                $"Похоже в прошлый раз вы не закончили создание контакта. Пожалуйста, введите {print}");
                        }
                        else
                        {
                            draft = new ShareContactDraft(userId, null, null, null, 
                                ContactStateMachine.CreateEnterName, BotFlow.CreateUser);

                            await botClient.SendMessage(chatId,
                                $"Пожалуйста, введите имя");
                        }

                        var serializedDraft = JsonSerializer.Serialize(draft);
                        await _redis.StringSetAsync($"contact:{userId}", serializedDraft);
                        break;
                    }
                case "contact:edit_name"
                {
                        var cachedDraft = await _redis.StringGetAsync($"contact:{userId}");
                        ShareContactDraft? draft = null;

                        if (!cachedDraft.IsNullOrEmpty)
                        {
                            draft = JsonSerializer.Deserialize<ShareContactDraft>((string)cachedDraft!);

                            if (draft == null)
                                return;
                       
                            await botClient.SendMessage(chatId,
                                $"Введите новое имя");
                        }
                        else
                            await botClient.SendMessage(chatId, $"Сначала создайте контакт");

                        draft.Flow = BotFlow.EditUser;
                        draft.State = EditContactStateMachine.EnterName;
                    }
            }
        }
    }
}
