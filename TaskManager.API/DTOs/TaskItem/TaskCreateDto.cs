using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.TaskItem
{
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = null!;

        [MaxLength(2000)]
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime DueDate { get; set; }
        public bool IsCompleted { get; set; }
    }
}
