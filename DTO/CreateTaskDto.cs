using System.ComponentModel.DataAnnotations;

namespace TaskManagement.API.DTOs.Task
{
    public class CreateTaskDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = "";

        [MaxLength(1000)]
        public string Description { get; set; } = "";

        [Required]
        public string Status { get; set; } = "To Do";

        [Required]
        public string Priority { get; set; } = "Medium";

        [Required]
        public DateTime Deadline { get; set; }

        [Required]
        public int AssignedToId { get; set; }
    }
}
