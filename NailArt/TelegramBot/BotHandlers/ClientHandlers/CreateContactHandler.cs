using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.BotFlows;
using TelegramBot.DTO.Clients;
using TelegramBot.Interfaces;
using TelegramBot.Buttons;
using TelegramBot.StateMachines.Clients;
using TelegramBot.Abstractions;
using Telegram.Bot.Types.ReplyMarkups;
namespace TelegramBot.BotHandlers
{
    public class CreateContactHandler : BaseOperationHandler<ContactProcess, Message>
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

        public override bool CanHandleAndConfirm(BotFlow flow) => flow == BotFlow.CreateUser;
        public override async Task HandleAsync(
            long userId,
            Message message,
            ContactProcess process,
            ITelegramBotClient botClient,
            CancellationToken token
        )
        {
            var cachedDraft = await _redis.StringGetAsync($"contact:creation_draft:{userId}");

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

                    if (process.EditType != null && process.EditType == EditContactType.Name)
                    {
                        process.EditType = null; 
                        process.State = ContactState.WaitingConfirmationUser;

                        var clientConfirmationKeyboard = ButtonBuilder.ContactCreatingConfirmationKeyoard();

                        await botClient.SendMessage(message.Chat.Id, $"Так выглядит ваше контакт:\n" +
                                 $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n" +
                                 $"Юзер нейм: {draft.UserName ?? "отсутствует"}", replyMarkup: clientConfirmationKeyboard);
                    } 
                    else
                    {
                        process.State = ContactState.EnterPhone;
                        await botClient.SendMessage(message.Chat.Id, "Введите, пожалуйста, номер телефона");
                    }                       

                    await Task.WhenAll([
                        _redis.StringSetAsync(
                         $"contact:process:{userId}",
                         JsonSerializer.Serialize(process)
                        ),
                        _redis.StringSetAsync(
                         $"contact:creation_draft:{userId}",
                         JsonSerializer.Serialize(draft)
                        ),
                    ]);

                    break;
                }
                case ContactState.EnterPhone:
                {
                    draft.Phone = message.Text;

                    process.EditType = null;
                    process.State = ContactState.WaitingConfirmationUser;

                    var clientConfirmationKeyboard = ButtonBuilder.ContactCreatingConfirmationKeyoard();

                        await Task.WhenAll([
                            _redis.StringSetAsync(
                            $"contact:process:{userId}",
                            JsonSerializer.Serialize(process)
                        ),
                        _redis.StringSetAsync(
                            $"contact:creation_draft:{userId}",
                            JsonSerializer.Serialize(draft)
                        ),
                        botClient.SendMessage(message.Chat.Id, $"Так выглядит ваше контакт:\n" +
                         $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n" +
                         $"Юзер нейм: {draft.UserName ?? "отсутствует"}", replyMarkup: clientConfirmationKeyboard)
                        ]);

                    break;
                }
            }
        }

        public override async Task ConfirmAsync(long userId, long chatId, ContactProcess process, ITelegramBotClient botClient, CancellationToken token)
        {
            if (process.State != ContactState.WaitingConfirmationUser)
            {
                await botClient.SendMessage(chatId, "Сейчас подтверждение недоступно.");
                return;
            }

            var cachedDraft = await _redis.StringGetAsync($"contact:creation_draft:{userId}");
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

            await Task.WhenAll([
                _redis.KeyDeleteAsync($"contact:creation_draft:{userId}"),
                _redis.KeyDeleteAsync($"contact:process:{userId}"),
                botClient.SendMessage(chatId, "Ваш контакт успешно добавлен ✅")
                ]);
        }
    }
}
