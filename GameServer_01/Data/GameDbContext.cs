using Microsoft.EntityFrameworkCore;

namespace GameServer_01.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Contents> Contents { get; set; } = null!;

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    base.OnModelCreating(modelBuilder);

        //    // 1) User.FirebaseUID를 기본키(PK)로 지정
        //    modelBuilder.Entity<User>()
        //        .HasKey(u => u.FirebaseUID);

        //    // 2) Contents → User 관계 설정 (Cascade Delete 적용)
        //    modelBuilder.Entity<Contents>()
        //        // Contents 엔티티는 User를 참조하는 별도의 네비게이션 프로퍼티가 없으므로,
        //        // 제네릭 버전 HasOne<TPrincipal>()을 사용합니다. 
        //        .HasOne<User>()
        //        // User 쪽에서 Contents 컬렉션에 대한 네비게이션 프로퍼티를 만들지 않았다면 WithMany()로 매핑
        //        .WithMany()
        //        // Contents.UID 컬럼이 User.FirebaseUID를 FK로 참조함을 지정
        //        .HasForeignKey(c => c.UID)
        //        // User가 삭제되면 Contents도 함께 삭제되도록 설정
        //        .OnDelete(DeleteBehavior.Cascade);

        //    // 3) UserInventory → User 관계 설정 (Cascade Delete 적용)
        //    modelBuilder.Entity<UserInventory>()
        //        .HasOne<User>()
        //        .WithMany()
        //        .HasForeignKey(ui => ui.UID)
        //        .OnDelete(DeleteBehavior.Cascade);
        //}
    }

    public class User
    {
        public int Id { get; set; }
        public string FirebaseUID { get; set; } = null!;
        public string? PlayerID { get; set; }
        public string Provider { get; set; } = null!;
        public string? DeviceId { get; set; }
        public string Email { get; set; } = null!;
        public ulong Gold { get; set; }
        public ulong Gem { get; set; }
        public uint Diamond { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastLoginAt { get; set; }   // ← 로그인 때마다 업데이트
    }

    public class Contents
    {
        public int Id { get; set; }
        public string UID { get; set; } = null!;
        public uint ChapterID { get; set; }
        public uint StageID { get; set; }
    }
}
