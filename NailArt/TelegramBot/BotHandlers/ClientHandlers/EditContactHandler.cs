using Application.Clients;
using Application.Clients.DTO;
using Infrastructure.Responses;
using MediatR;
using StackExchange.Redis;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramBot.Abstractions;
using TelegramBot.BotFlows;
using TelegramBot.BotSessions;
using TelegramBot.Buttons;
using TelegramBot.DTO.Clients;
using TelegramBot.Interfaces;
using TelegramBot.StateMachines.Clients;

namespace TelegramBot.BotHandlers
{
    public class EditContactTypeHandler : BaseOperationHandler<ContactProcess, Message>
    {
        private readonly IRedisService _redis;
        private readonly IMediator _mediator;

        public EditContactTypeHandler(IRedisService redis, IMediator mediator)
        {
            _redis = redis;
            _mediator = mediator;
        }

        public override bool CanHandleAndConfirm(BotFlow flow) => flow == BotFlow.EditUser;

        public override async Task HandleAsync(
            long userId,
            Message message,
            ContactProcess process,
            ITelegramBotClient botClient,
            CancellationToken token
        )
        {
            var userExists = await _mediator.Send(new CheckClientByIdQuery(userId), token);
            if (!userExists)
            {
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Такого пользователя не существует",
                    cancellationToken: token
                );
                return;
            }

            var draft = await _redis.GetProcessAsync<EditContactDraft>(userId);
            if (draft == null)
            {
                await botClient.SendMessage(
                    message.Chat.Id,
                    "Что-то пошло не так",
                    cancellationToken: token
                );
                return;
            }

            switch (process.State)
            {
                case ContactState.EnterName:
                {
                    draft.Name = message.Text;
                    process.State = ContactState.WaitingConfirmationUser;

                    var confirmationKeyboard = ButtonBuilder.ContactEditingConfirmationKeyboard();

                    await Task.WhenAll([
                        _redis.SaveProcessAsync(userId, process),
                        _redis.SaveDraftAsync(userId, draft),
                        botClient.SendMessage(
                            message.Chat.Id,
                            $"Так выглядит ваше контакт:\n"
                                + $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n"
                                + $"Юзер нейм: {draft.UserName ?? "отсутствует"}",
                            replyMarkup: confirmationKeyboard,
                            cancellationToken: token
                        ),
                    ]);

                    break;
                }
                case ContactState.EnterPhone:
                {
                    draft.Phone = message.Text;
                    process.State = ContactState.WaitingConfirmationUser;

                    var confirmationKeyboard = ButtonBuilder.ContactEditingConfirmationKeyboard();

                    await Task.WhenAll([
                        _redis.SaveDraftAsync(userId, draft),
                        _redis.SaveProcessAsync(userId, process),
                        botClient.SendMessage(
                            message.Chat.Id,
                            $"Так выглядит ваше контакт:\n"
                                + $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n"
                                + $"Юзер нейм: {draft.UserName ?? "отсутствует"}",
                            replyMarkup: confirmationKeyboard,
                            cancellationToken: token
                        ),
                    ]);

                    break;
                }
                case ContactState.EnterUserName:
                {
                    draft.UserName = message.Text;
                    process.State = ContactState.WaitingConfirmationUser;

                    var confirmationKeyboard = ButtonBuilder.ContactEditingConfirmationKeyboard();

                    await Task.WhenAll([
                        _redis.SaveProcessAsync(userId, process),
                        _redis.SaveDraftAsync(userId, draft),
                        botClient.SendMessage(
                            message.Chat.Id,
                            $"Так выглядит ваше контакт:\n"
                                + $"Имя: {draft.Name}\nНомер телефона: {draft.Phone}\n"
                                + $"Юзер нейм: {draft.UserName ?? "отсутствует"}",
                            replyMarkup: confirmationKeyboard,
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
                await botClient.SendMessage(
                    chatId,
                    "Сейчас подтверждение недоступно.",
                    cancellationToken: token
                );
                return;
            }

            var draft = await _redis.GetDraftAsync<EditContactDraft>(userId);

            if (process.EditType == null)
            {
                await botClient.SendMessage(
                    chatId,
                    $"Что-то пошло не так❌",
                    cancellationToken: token
                );
                return;
            }

            Result<ClientResponse>? result = null;

            switch (process.EditType)
            {
                case EditContactType.Name:
                {
                    result = await _mediator.Send(
                        new EditClientNameCommand(userId, draft.Name),
                        token
                    );
                    break;
                }
                case EditContactType.Phone:
                {
                    result = await _mediator.Send(
                        new EditClientPhoneCommand(userId, draft.Phone),
                        token
                    );
                    break;
                }
                case EditContactType.UserName:
                {
                    result = await _mediator.Send(
                        new EditClientUserNameCommand(userId, draft.UserName),
                        token
                    );
                    break;
                }
            }

            if (!result!.IsSuccess)
            {
                await botClient.SendMessage(
                    chatId,
                    result.ErrorMessage ?? "Не удалось сохранить изменения ❌",
                    cancellationToken: token
                );
                return;
            }

            currentSession.currentFlow = BotFlow.Menu;

            await Task.WhenAll([
                _redis.GetDraftAsync<EditContactDraft>(userId),
                _redis.GetProcessAsync<ContactProcess>(userId),
                _redis.SaveCurrentSession(userId, currentSession),
                botClient.SendMessage(
                    chatId,
                    "Ваш контакт успешно обновлен✅",
                    cancellationToken: token
                ),
            ]);
        }
    }
}
