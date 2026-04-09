using System.ComponentModel.DataAnnotations;

namespace TaskManager.API.DTOs.Comment
{
    public class CommentCreateDto
    {
        [Required(ErrorMessage = "Content is required")]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = null!;
        public DateTime CreatedTime { get; set; } = DateTime.Now;
    }
}
