namespace Domain.Enums 
{ 
    public enum BookingStatus { Pending, Cancelled, Completed };
    public enum UserRole { Client, Admin };
    public enum ErrorType { NotFound, Unauthorized, Validation, Conflict, Forbidden };
}