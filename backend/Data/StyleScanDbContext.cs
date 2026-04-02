using Microsoft.EntityFrameworkCore;
using StyleScan.Backend.Models.Domain;

namespace StyleScan.Backend.Data
{
    public class StyleScanDbContext : DbContext
    {
        public StyleScanDbContext(DbContextOptions<StyleScanDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Avatar> Avatars { get; set; }
        public DbSet<Look> Looks { get; set; }
        public DbSet<Clothing> Clothings { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<LookClothing> LookClothings { get; set; }
        public DbSet<UserPreference> UserPreferences { get; set; }
        public DbSet<UserUsageRecord> UserUsageRecords { get; set; }
        public DbSet<TryOnPreviewHistory> TryOnPreviewHistories { get; set; }
        // Adicionar outros DbSets conforme necessário

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(user => user.Email).IsUnique();
                entity.Property(user => user.Email).HasMaxLength(255).IsRequired();
                entity.Property(user => user.PasswordHash).IsRequired();
                entity.Property(user => user.FirstName).HasMaxLength(120).IsRequired();
                entity.Property(user => user.LastName).HasMaxLength(120).IsRequired();
                entity.Property(user => user.PublicProfileSlug).HasMaxLength(160);
                entity.Property(user => user.DateOfBirth).HasColumnType("date");
                entity.Property(user => user.AccountPlan).HasMaxLength(32).HasDefaultValue(AccountPlanType.Free).IsRequired();
                entity.Property(user => user.SubscriptionStatus).HasMaxLength(32).HasDefaultValue("inactive").IsRequired();
                entity.Property(user => user.SubscriptionProvider).HasMaxLength(64);
                entity.Property(user => user.SubscriptionReference).HasMaxLength(120);
                entity.Property(user => user.PendingAccountPlan).HasMaxLength(32);
                entity.Property(user => user.LastPaymentId).HasMaxLength(120);
                entity.Property(user => user.LastPaymentStatus).HasMaxLength(32);
                entity.Property(user => user.LastPaymentStatusDetail).HasMaxLength(120);
            });

            modelBuilder.Entity<UserUsageRecord>(entity =>
            {
                entity.Property(record => record.MetricType).HasMaxLength(64).IsRequired();
                entity.Property(record => record.PeriodKey).HasMaxLength(32).IsRequired();
                entity.HasIndex(record => new { record.UserId, record.MetricType, record.PeriodKey }).IsUnique();
            });

            modelBuilder.Entity<UserPreference>(entity =>
            {
                entity.ToTable("UserPreference");
                entity.Property(preference => preference.PreferenceKey).HasMaxLength(120).IsRequired();
                entity.Property(preference => preference.PreferenceValue).HasMaxLength(255).IsRequired();
                entity.HasIndex(preference => new { preference.UserId, preference.PreferenceKey, preference.PreferenceValue }).IsUnique();
            });

            modelBuilder.Entity<TryOnPreviewHistory>(entity =>
            {
                entity.Property(history => history.Style).HasMaxLength(120).IsRequired();
                entity.Property(history => history.Occasion).HasMaxLength(120).IsRequired();
                entity.Property(history => history.BoardId).HasMaxLength(64);
                entity.Property(history => history.Mode).HasMaxLength(32).IsRequired();
                entity.Property(history => history.ImageUrl).HasMaxLength(500).IsRequired();
                entity.Property(history => history.ProductNames).HasColumnType("text[]");
                entity.Property(history => history.ProductCategories).HasColumnType("text[]");
                entity.HasIndex(history => new { history.UserId, history.AvatarId, history.CreatedAt });
            });

            modelBuilder.Entity<Avatar>(entity =>
            {
                entity.Property(avatar => avatar.Name).HasMaxLength(120).IsRequired();
                entity.Property(avatar => avatar.ModelUrl).HasMaxLength(500).IsRequired();
                entity.Property(avatar => avatar.PhotoUrl).HasMaxLength(500);
                entity.Property(avatar => avatar.PhotoUrls).HasColumnType("text[]");
                entity.Property(avatar => avatar.GeneratedAvatarImageUrl).HasMaxLength(500);
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.Property(store => store.Name).HasMaxLength(150).IsRequired();
            });

            modelBuilder.Entity<Clothing>(entity =>
            {
                entity.Property(clothing => clothing.Name).HasMaxLength(150).IsRequired();
                entity.Property(clothing => clothing.Category).HasMaxLength(80).IsRequired();
                entity.Property(clothing => clothing.Sizes).HasColumnType("text[]");
            });

            modelBuilder.Entity<Look>(entity =>
            {
                entity.Property(look => look.Note).HasMaxLength(220);
                entity.Property(look => look.OccasionTags).HasColumnType("text[]");
                entity.Property(look => look.HeroImageUrl).HasMaxLength(500);
                entity.Property(look => look.HeroPreviewMode).HasMaxLength(32);
                entity.Property(look => look.ShareSlug).HasMaxLength(160);
            });

            // Configurar chave primária composta para LookClothing
            modelBuilder.Entity<LookClothing>()
                .HasKey(lc => new { lc.LookId, lc.ClothingId });

            // Configurar relacionamento muitos-para-muitos entre Look e Clothing
            modelBuilder.Entity<LookClothing>()
                .HasOne(lc => lc.Look)
                .WithMany(l => l.LookClothings)
                .HasForeignKey(lc => lc.LookId);

            modelBuilder.Entity<LookClothing>()
                .HasOne(lc => lc.Clothing)
                .WithMany(c => c.LookClothings)
                .HasForeignKey(lc => lc.ClothingId);

            // Configurar relacionamento um-para-muitos entre User e Avatar
            modelBuilder.Entity<Avatar>()
                .HasOne(a => a.User)
                .WithMany(u => u.Avatars)
                .HasForeignKey(a => a.UserId);

            // Configurar relacionamento um-para-muitos entre User e Look
            modelBuilder.Entity<Look>()
                .HasOne(l => l.User)
                .WithMany(u => u.Looks)
                .HasForeignKey(l => l.UserId);

            // Configurar relacionamento um-para-muitos entre Avatar e Look
            modelBuilder.Entity<Look>()
                .HasOne(l => l.Avatar)
                .WithMany(a => a.Looks)
                .HasForeignKey(l => l.AvatarId)
                .OnDelete(DeleteBehavior.Restrict); // Evitar exclusão em cascata de looks ao deletar avatar

            modelBuilder.Entity<UserUsageRecord>()
                .HasOne(record => record.User)
                .WithMany(user => user.UsageRecords)
                .HasForeignKey(record => record.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserPreference>()
                .HasOne(preference => preference.User)
                .WithMany(user => user.Preferences)
                .HasForeignKey(preference => preference.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TryOnPreviewHistory>()
                .HasOne(history => history.User)
                .WithMany()
                .HasForeignKey(history => history.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TryOnPreviewHistory>()
                .HasOne(history => history.Avatar)
                .WithMany()
                .HasForeignKey(history => history.AvatarId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configurar relacionamento um-para-muitos entre Store e Clothing
            modelBuilder.Entity<Clothing>()
                .HasOne(c => c.Store)
                .WithMany(s => s.Clothings)
                .HasForeignKey(c => c.StoreId);
        }
    }
}
