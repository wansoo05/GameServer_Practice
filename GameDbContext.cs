using Microsoft.EntityFrameworkCore;

namespace GameServer.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options) { }
        public DbSet<User> Users { get; set; } = null!;
    }

    public class User
    {
        public int Id { get; set; }
        public string FirebaseUid { get; set; } = null!;
        public string Email { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
