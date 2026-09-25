namespace TelegramBot.StateMachines.Bookings
{ 
    public enum BookingState { SelectService, SelectYear, SelectMonth, SelectDay, SelectTime, WaitingForConfirmation };
}
