using Application.Clients;
using Application.Clients.DTO;
using MediatR;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO;
using TelegramBot.Interfaces;
using Infrastructure.Responses;
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
        public bool CanHandleAndConfirm(BotFlow flow) => flow == BotFlow.EditUser;
        public async Task HandleAsync(long userId, Message message, ContactProcess process, ITelegramBotClient botClient, CancellationToken token)
        {
            var userExists = await _mediator.Send(new CheckClientByIdQuery(userId), token);
            if(!userExists)
            {
                await botClient.SendMessage(message.Chat.Id, "Такого пользователя не существует");
                return;
            }

            var serializedDraft = await _redis.StringGetAsync($"contact:edit_draft:{userId}");
            if(serializedDraft.IsNullOrEmpty)
            {
                await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так");
                return;
            }

            var draft = JsonSerializer.Deserialize<EditContactDraft>((string)serializedDraft!);
            if (draft == null)
            {
                await botClient.SendMessage(message.Chat.Id, "Что-то пошло не так");
                return;
            }

            switch (process.State)
            {
                case ContactState.EnterName:
                    {
                        draft.Name = message.Text;
                        process.State = ContactState.WaitingConfirmationUser;

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:edit_draft:{userId}",
                            JsonSerializer.Serialize(draft))
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
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:edit_draft:{userId}",
                            JsonSerializer.Serialize(draft))
                            ]);

                        break;
                    }
                case ContactState.EnterUserName:
                    {
                        draft.UserName = message.Text;
                        process.State = ContactState.WaitingConfirmationUser;

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)),

                            _redis.StringSetAsync(
                            $"contact:edit_draft:{userId}",
                            JsonSerializer.Serialize(draft))
                            ]);

                        break;
                    }
            }
        }
        public async Task ConfirmAsync(long userId, long chatId, ContactProcess process, ITelegramBotClient botClient, CancellationToken token)
        {
            if (process.State != ContactState.WaitingConfirmationUser)
            {
                await botClient.SendMessage(chatId, "Сейчас подтверждение недоступно.");
                return;
            }

            var cachedDraft = await _redis.StringGetAsync($"contact:edit_draft:{userId}");
            if (cachedDraft.IsNullOrEmpty)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }

            var deserializedDraft = JsonSerializer.Deserialize<EditContactDraft>((string)cachedDraft!);
            if (deserializedDraft == null)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }

            if(process.EditType == null)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }

            Result<ClientResponse>? result = null;

            switch(process.EditType)
            {
                case EditType.Name:
                    {
                        result = await _mediator.Send(new EditClientNameCommand(userId, deserializedDraft.Name), token);
                        break;
                    }
                case EditType.Phone:
                    {
                        result = await _mediator.Send(new EditClientPhoneCommand(userId, deserializedDraft.Phone), token);
                        break;
                    }
                case EditType.UserName:
                    {
                        result = await _mediator.Send(new EditClientUserNameCommand(userId, deserializedDraft.UserName), token);
                        break;
                    }
            }

            if (!result!.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage ?? "Не удалось сохранить изменения ❌");
                return;
            }

            await _redis.KeyDeleteAsync($"contact:edit_draft:{userId}");
            await _redis.KeyDeleteAsync($"contact:process:{userId}");

            await botClient.SendMessage(
                chatId,
                "Ваш контакт успешно обновлен✅");
        }
    }
}