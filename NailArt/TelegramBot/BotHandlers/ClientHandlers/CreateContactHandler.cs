using Application.Clients;
using MediatR;
using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramBot.Abstractions;
using TelegramBot.BotFlows;
using TelegramBot.BotSessions;
using TelegramBot.Buttons;
using TelegramBot.DTO.Clients;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines.Clients;

namespace TelegramBot.BotHandlers
{
    public class CreateContactHandler : BaseOperationHandler<ContactProcess, Message>
    {
        private readonly IRedisService _redis;
        private readonly IMediator _mediator;

        public CreateContactHandler(IRedisService redis, IMediator mediator)
        {
            _redis = redis;
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
            long chatId = message.Chat.Id;

            var draft = await _redis.GetDraftAsync<ShareContactDraft>(userId);
            if (draft == null)
            {
                await botClient.SendMessage(
                    chatId,
                    "Не удалось найти черновик",
                    cancellationToken: token
                );
                return;
            }

            switch (process.State)
            {
                case ContactState.EnterName:
                {
                    draft.Name = message.Text;

                    if (process.EditType != null && process.EditType == EditContactType.Name)
                    {
                        process.EditType = null;
                        process.State = ContactState.WaitingConfirmationUser;

                        var clientConfirmationKeyboard =
                            ButtonBuilder.ContactCreatingConfirmationKeyoard();

                        await botClient.SendMessage(
                            message.Chat.Id,
                            $"Так выглядит ваше контакт:\n"
                                + $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n"
                                + $"Юзер нейм: {draft.UserName ?? "отсутствует"}",
                            replyMarkup: clientConfirmationKeyboard
                        );
                    }
                    else
                    {
                        process.State = ContactState.EnterPhone;
                        await botClient.SendMessage(
                            message.Chat.Id,
                            "Введите, пожалуйста, номер телефона"
                        );
                    }

                    await Task.WhenAll([
                        _redis.SaveProcessAsync(userId, process),
                        _redis.SaveDraftAsync(userId, draft),
                    ]);

                    break;
                }
                case ContactState.EnterPhone:
                {
                    draft.Phone = message.Text;

                    process.EditType = null;
                    process.State = ContactState.WaitingConfirmationUser;

                    var clientConfirmationKeyboard =
                        ButtonBuilder.ContactCreatingConfirmationKeyoard();

                    await Task.WhenAll([
                        _redis.SaveProcessAsync(userId, process),
                        _redis.SaveDraftAsync(userId, process),
                        botClient.SendMessage(
                            message.Chat.Id,
                            $"Так выглядит ваше контакт:\n"
                                + $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n"
                                + $"Юзер нейм: {draft.UserName ?? "отсутствует"}",
                            replyMarkup: clientConfirmationKeyboard,
                            cancellationToken: token
                        ),
                    ]);

                    break;
                }
            }
        }

        public override async Task ConfirmAsync(
            long userId,
            long chatId,
            ContactProcess process,
            ITelegramBotClient botClient,
            BotSession currentSession,
            CancellationToken token
        )
        {
            if (process.State != ContactState.WaitingConfirmationUser)
            {
                await botClient.SendMessage(chatId, "Сейчас подтверждение недоступно.");
                return;
            }

            var draft = await _redis.GetDraftAsync<ShareContactDraft>(userId);
            if (draft == null)
            {
                await botClient.SendMessage(chatId, $"Что-то пошло не так❌");
                return;
            }

            var result = await _mediator.Send(
                new CreateClientCommand(draft.Id, draft.Name!, draft.Phone!, draft.UserName),
                token
            );

            if (!result.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage ?? "Не удалось создать контакт ❌"
                );
                return;
            }

            currentSession.currentFlow = BotFlow.Menu;

            await Task.WhenAll([
                _redis.DeleteProcessAsync<ContactProcess>(userId),
                _redis.DeleteDraftAsync<ShareContactDraft>(userId),
                _redis.SaveCurrentSession(userId, currentSession),
                botClient.SendMessage(chatId, "Ваш контакт успешно добавлен ✅"),
            ]);
        }
    }
}
