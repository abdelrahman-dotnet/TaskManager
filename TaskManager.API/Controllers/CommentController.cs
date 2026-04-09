using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using TaskManager.API.DTOs.Comment;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Bussiness.Repositories;
using TaskManager.Data.Entities;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommentController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CommentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        [HttpGet("tasks/{taskId}/comments")]
        public async Task<IActionResult> GetByTask(int taskId)
        {
            var comments = await _unitOfWork.commentRepository.GetByTaskIdAsync(taskId);
            if (comments == null || !comments.Any()) 
                return NotFound();
            var result = comments.Select(c => new CommentReadDto
            {
                Id = c.Id,
                Content = c.Content,
                CreatedAt = c.CreatedAt,
                TaskItemId = c.TaskItemId,
                UserId = c.UserId,
            });
            return Ok(result);
        }
        [HttpPost("tasks/{taskId}/comments")]
        public async Task<IActionResult> Creat(int taskId, [FromBody] CommentCreateDto dto)
        {
           if(!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(e=>e.Errors).Select(e=>e.ErrorMessage));
           var task = await _unitOfWork.taskRepository.GetByIdAsync(taskId);
            if (task == null)
                return NotFound();
            /// add get user id from claims to put it in comment 
            var coment = new Comment
            {
                Content = dto.Content,
                CreatedAt = DateTime.Now,
                TaskItemId = taskId,
            };
            await _unitOfWork.commentRepository.AddAsync(coment);
            await _unitOfWork.Complete();
            var read = new CommentReadDto
            {
                Id = coment.Id,
                Content = coment.Content,
                CreatedAt = DateTime.Now,
                TaskItemId = taskId,
                UserId = coment.UserId,
            };
            return CreatedAtAction(nameof(GetByTask),new {TaskId = taskId},read);
        }
        [HttpPut]
        public async Task<IActionResult> UpdateComment(int id,CommentCreateDto dto)
        {
            var comment= await _unitOfWork.commentRepository.GetByIdAsync(id);
            if (comment == null)
                return NotFound();
            comment.Content = dto.Content;
            _unitOfWork.commentRepository.Update(comment);
            await _unitOfWork.Complete();
            return Ok(new CommentReadDto
            {
                Id = comment.Id,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                TaskItemId = comment.TaskItemId,
                UserId = comment.UserId,
            });
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var comment = await _unitOfWork.commentRepository.GetByIdAsync(id);
            if (comment == null)
                return NotFound();
            // wiil add authorization here
            _unitOfWork.commentRepository.Delete(comment);   
            await _unitOfWork.Complete();
            return NoContent();
        }
    }
}
