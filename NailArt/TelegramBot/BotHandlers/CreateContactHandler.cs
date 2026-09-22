using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines;

namespace TelegramBot.BotHandlers
{
    public class CreateContactHandler : IOperationHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;

        public CreateContactHandler(
            IConnectionMultiplexer connectionMultiplexer,
            IMediator mediator
        )
        {
            _redis = connectionMultiplexer.GetDatabase();
            _mediator = mediator;
        }

        public bool CanHandle(BotFlow flow) => flow == BotFlow.CreateUser;

        public async Task HandleAsync(
            long userId,
            Message message,
            ContactProcess process,
            ITelegramBotClient botClient,
            CancellationToken token
        )
        {
            var cachedDraft = await _redis.StringGetAsync($"contact:draft:{userId}");

            if (cachedDraft.IsNullOrEmpty)
                return;

            var draft = JsonSerializer.Deserialize<ShareContactDraft>((string)cachedDraft!);

            if (draft == null)
                return;

            switch (process.State)
            {
                case ContactState.EnterName:
                {
                    draft.Name = message.Text;
                    process.State = ContactState.EnterPhone;

                    await Task.WhenAll([
                        _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)
                        ),
                        _redis.StringSetAsync(
                            $"contact:draft:{userId}",
                            JsonSerializer.Serialize(draft)
                        ),
                        botClient.SendMessage(message.Chat.Id, "Введите, пожалуйста, номер телефона")
                    ]);

                    break;
                }
                case ContactState.EnterPhone:
                {
                    draft.Phone = message.Text;
                    process.State = ContactState.WaitingConfirmationUser;

                    await Task.WhenAll([
                        _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)
                        ),
                        _redis.StringSetAsync(
                            $"contact:draft:{userId}",
                            JsonSerializer.Serialize(draft)
                        ),
                        botClient.SendMessage(message.Chat.Id, $"Так выглядит ваше контакт:\n" +
                         $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n" +
                         $"Юзер нейм: {draft.UserName ?? "отсутствует"}")
                    ]);

                    break;
                }
            }
        }

        public bool CanConfirm(BotFlow flow) => flow == BotFlow.CreateUser;
        public async Task ConfirmAsync(long userId, long chatId, ContactState state, ITelegramBotClient botClient, CancellationToken token)
        {
            if (state != ContactState.WaitingConfirmationUser)
            {
                await botClient.SendMessage(chatId, "Сейчас подтверждение недоступно.");
                return;
            }

            var cachedDraft = await _redis.StringGetAsync($"contact:draft:{userId}");
            if (cachedDraft.IsNullOrEmpty)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }

            var deserializedDraft = JsonSerializer.Deserialize<ShareContactDraft>((string)cachedDraft!);
            if (deserializedDraft == null)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }  

            var result = await _mediator.Send(new CreateClientCommand(
                            deserializedDraft.Id,
                            deserializedDraft.Name!,
                            deserializedDraft.Phone!,
                            deserializedDraft.UserName), token);

            if (!result.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage ?? "Не удалось создать контакт ❌");
                return;
            }

            await _redis.KeyDeleteAsync($"contact:draft:{userId}");
            await _redis.KeyDeleteAsync($"contact:process:{userId}");

            await botClient.SendMessage(
                chatId,
                "Ваш контакт успешно добавлен ✅");
        }
    }
}
