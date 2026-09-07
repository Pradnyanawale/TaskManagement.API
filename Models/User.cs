namespace TaskManagement.API.Models
{
    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; } = "";

        public string Email { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string Role { get; set; } = "User";

        public ICollection<TaskItem> AssignedTasks { get; set; }  
            = new List<TaskItem>();

        public ICollection<Comment> Comments { get; set; } 
            = new List<Comment>(); 
    }
}
