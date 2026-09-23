namespace TelegramBot.DTO.Bookings
{
    public class PaginationDTO
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public PaginationDTO(int currentPage, int pageSize, int totalPages)
        {
            CurrentPage = currentPage;
            PageSize = pageSize;
            TotalPages = totalPages;
        }
    }
}