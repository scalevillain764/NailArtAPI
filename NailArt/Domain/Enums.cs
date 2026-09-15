namespace Domain.Enums 
{ 
    public enum BookingStatus { Booked, Cancelled, Completed };
    public enum UserRole { Client, Admin };
    public enum ErrorType { NotFound, Unauthorized, Validation, Conflict, Forbidden };
}