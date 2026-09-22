using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines;
namespace TelegramBot.BotHandlers
{
    public class EditContactHandler : IOperationHandler
    {
        private readonly IDatabase _redis;
        private readonly IMediator _mediator;
        public EditContactHandler(IConnectionMultiplexer connectionMultiplexer, IMediator mediator)
        {
            _redis = connectionMultiplexer.GetDatabase();
            _mediator = mediator;
        }
        public bool CanHandle(BotFlow flow) => flow == BotFlow.EditUser;
        public async Task HandleAsync(long userId, string message, ContactProcess process, CancellationToken token)
        {
            var cachedDraft = await _redis.StringGetAsync(
                    $"contact:draft:{userId}");

            if (cachedDraft.IsNullOrEmpty)
                return;

            var draft = JsonSerializer.Deserialize<ShareContactDraft>(
                (string)cachedDraft!);

            if (draft == null)
                return;

            switch (process.State)
            {
                case ContactState.EnterName:
                    {
                        draft.Name = message;
                        process.State = ContactState.WaitingConfirmationUser;

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:draft:{userId}",
                            JsonSerializer.Serialize(draft))
                            ]);

                        break;
                    }
                case ContactState.EnterPhone:
                    {
                        draft.Phone = message;
                        process.State = ContactState.EnterUserName;

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:draft:{userId}",
                            JsonSerializer.Serialize(draft))
                            ]);

                        break;
                    }
                case ContactState.EnterUserName:
                    {
                        draft.UserName = message;
                        process.State = ContactState.WaitingConfirmationUser;

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:draft:{userId}",
                            JsonSerializer.Serialize(draft))
                            ]);

                        break;
                    }

            }
        }
        public bool CanConfirm(BotFlow flow) => flow == BotFlow.EditUser;

        public async Task ConfirmAsync(long userId, long chatId, ITelegramBotClient botClient, CancellationToken token)
        {
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