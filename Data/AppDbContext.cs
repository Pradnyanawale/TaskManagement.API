using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Models;

namespace TaskManagement.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Team> Teams => Set<Team>();
        public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==========================================
            // TEAM -> MANAGER
            // One User can manage many Teams
            // ==========================================
            modelBuilder.Entity<Team>()
                .HasOne(t => t.Manager)
                .WithMany()
                .HasForeignKey(t => t.ManagerId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // TEAM -> TEAM MEMBERS
            // ==========================================
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.TeamMembers)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);


            // ==========================================
            // TEAM MEMBER -> USER
            // ==========================================
            modelBuilder.Entity<TeamMember>()
                .HasOne(tm => tm.User)
                .WithMany()
                .HasForeignKey(tm => tm.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // PREVENT DUPLICATE TEAM MEMBERS
            // Same user cannot be added twice
            // to the same team
            // ==========================================
            modelBuilder.Entity<TeamMember>()
                .HasIndex(tm => new

                {
                    tm.TeamId,
                    tm.UserId
                })
                .IsUnique();


            // ==========================================
            // TASK -> ASSIGNED USER
            // ==========================================
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedTo)
                .WithMany(u => u.AssignedTasks)
                .HasForeignKey(t => t.AssignedToId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // TASK -> CREATED BY USER
            // ==========================================
            modelBuilder.Entity<TaskItem>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(t => t.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // COMMENT -> USER
            // ==========================================
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);


            // ==========================================
            // COMMENT -> TASK
            // ==========================================
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.TaskItem)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}