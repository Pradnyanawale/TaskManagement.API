namespace TaskManagement.API.Models
{
    public class TeamMember
    {
        public int Id { get; set; }

        public int TeamId { get; set; }

        public Team? Team { get; set; }

        public int UserId { get; set; }

        public User? User { get; set; }
    }
}