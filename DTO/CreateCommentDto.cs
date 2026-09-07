
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.API.DTOs.Comment
{
    public class CreateCommentDto
    {
        [Required]
        [MaxLength(1000)]
        public string Content { get; set; } = "";

        [Required]
        public int TaskItemId { get; set; }
    }
}

