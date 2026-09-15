using Domain;
using Microsoft.EntityFrameworkCore;
using System.Net.NetworkInformation;
using BookingStatus = Domain.Enums.BookingStatus;
namespace Infrastructure
{
    public class AppDbContext : DbContext
    {
        public DbSet<Booking> bookings { get; set; }
        public DbSet<User> users { get; set; }
        public DbSet<Service> services { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.bookings);

            builder.Entity<Booking>()
               .HasOne(b => b.Service)
               .WithMany(s => s.bookings);

            builder.Entity<Booking>()
                .HasQueryFilter(x => x.Status == BookingStatus.Completed 
                || x.Status == BookingStatus.Booked);
        }
    }
}