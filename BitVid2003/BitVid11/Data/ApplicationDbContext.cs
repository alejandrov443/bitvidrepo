using BitVid11.Models;
using Microsoft.EntityFrameworkCore;

namespace BitVid11.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<Character> Characters { get; set; }  // Existing
        public DbSet<Product> Products { get; set; }      // Added Products
        public DbSet<Order> Orders { get; set; }          // Added Orders table
        public DbSet<VideoJobs> VideoJobs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=localhost;database=bitdb;user=bitviduser;password=sunshine1!");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired();
                entity.Property(e => e.Email).IsRequired();
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.Phone).IsRequired();

                // Add this
                entity.Property(e => e.SubscriptionStatus)
                      .IsRequired()
                      .HasMaxLength(50)   // Optional: max length for the string
                      .HasDefaultValue("Free");
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.startmsg).HasMaxLength(50);
                entity.Property(e => e.CharacterName).HasMaxLength(100);
                entity.Property(e => e.status).HasMaxLength(100);
            });

            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
            });

            modelBuilder.Entity<Character>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Message).HasMaxLength(10000);
                entity.Property(e => e.VoiceUrl).HasMaxLength(500).HasColumnName("voiceurl");

                // One-to-many relationship with Products
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.Character)
                      .HasForeignKey(p => p.CharacterId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(500);
                entity.Property(p => p.Price).HasColumnType("decimal(10,2)").IsRequired();
                entity.Property(p => p.ImageFile).HasMaxLength(255);
                entity.Property(p => p.Description).HasMaxLength(2000);
            });

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                // UserId as int
                entity.Property(o => o.UserId)
                      .IsRequired()
                      .HasColumnType("int");

                entity.Property(o => o.FullName)
                      .IsRequired()
                      .HasMaxLength(200);
                entity.Property(o => o.AddressLine1)
                      .IsRequired()
                      .HasMaxLength(500);
                entity.Property(o => o.AddressLine2)
                      .HasMaxLength(500);
                entity.Property(o => o.City)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(o => o.State)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(o => o.PostalCode)
                      .IsRequired()
                      .HasMaxLength(20);
                entity.Property(o => o.Country)
                      .IsRequired()
                      .HasMaxLength(100);
                entity.Property(o => o.CreatedAt)
                      .IsRequired();
                entity.Property(o => o.StripeSessionId)
                      .HasMaxLength(255);
                entity.Property(o => o.PaymentStatus)
                      .HasMaxLength(50);

                // Relationship with Product
                entity.HasOne(o => o.Product)
                      .WithMany()
                      .HasForeignKey(o => o.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<VideoJobs>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.JobId).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Prompt).IsRequired().HasMaxLength(2000);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
                entity.Property(e => e.VideoPath).HasMaxLength(500);
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.GalleryType).HasMaxLength(50).HasDefaultValue("private");

                // <-- Important: Remove default in DB
                entity.Property(e => e.CreatedAt)
                      .IsRequired()
                      .HasDefaultValue(null);

                entity.Property(e => e.CompletedAt).IsRequired(false);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

        }
    }
}
