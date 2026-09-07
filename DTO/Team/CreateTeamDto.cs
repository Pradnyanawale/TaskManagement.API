using System.ComponentModel.DataAnnotations;

namespace TaskManagement.API.DTO.Team
{
    public class CreateTeamDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = "";

        [Required]
        public int ManagerId { get; set; }
    }
}