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
        
        public DbSet<CourseParticipation> CourseParticipations { get; set; }

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
            
            
            // EventParticipation ilişkisini yapılandır
            modelBuilder.Entity<EventParticipation>()
                .HasOne(ep => ep.User)
                .WithMany(u => u.EventParticipations)
                .HasForeignKey(ep => ep.UserId);

            modelBuilder.Entity<EventParticipation>()
                .HasOne(ep => ep.Event)
                .WithMany(e => e.EventParticipations)
                .HasForeignKey(ep => ep.EventId);
            
            // CourseParticipation ilişkisini yapılandır
            modelBuilder.Entity<CourseParticipation>()
                .HasOne(cp => cp.User)
                .WithMany(u => u.CourseParticipations)
                .HasForeignKey(cp => cp.UserId);

            modelBuilder.Entity<CourseParticipation>()
                .HasOne(cp => cp.Course)
                .WithMany(c => c.CourseParticipations)
                .HasForeignKey(cp => cp.CourseId);

            
        }
        
    }
}