namespace TaskManagement.API.Models
{
    public class TaskItem
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string Status { get; set; } = "To Do";

        public string Priority { get; set; } = "Medium";

        public DateTime Deadline { get; set; }

        public int AssignedToId { get; set; }

        public User? AssignedTo { get; set; }

        public int CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Comment> Comments { get; set; }
            = new List<Comment>();
    }
}
