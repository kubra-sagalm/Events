using Events.Models;
using Microsoft.EntityFrameworkCore;

namespace Events.DbContext
{
    public class ApplicationDbContext : Microsoft.EntityFrameworkCore.DbContext // DbContext sınıfından türetiyoruz
    {
        public DbSet<User> Users { get; set; } // Kullanıcı tablosu
        public DbSet<Event> Events { get; set; }
        
        public DbSet<Course> Courses { get; set; }
        
        public DbSet<EventParticipation> EventParticipations { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Temel yapılandırma çağrısı

            // Email için benzersizlik kısıtlaması
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Event ile User arasındaki ilişkiyi yapılandırma
            modelBuilder.Entity<Event>()
                .HasOne(e => e.User)
                .WithMany(u => u.Events)
                .HasForeignKey(e => e.UserId);
            
            modelBuilder.Entity<Course>()
                .HasOne(c => c.User)
                .WithMany(c => c.Courses)
                .HasForeignKey(c => c.UserId);
            
            modelBuilder.Entity<EventParticipation>().HasNoKey(); 

            
        }

        
      
    }
}