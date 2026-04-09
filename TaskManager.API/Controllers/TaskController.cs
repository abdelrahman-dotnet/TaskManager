using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TaskManager.API.DTOs.Comment;
using TaskManager.API.DTOs.TaskItem;
using TaskManager.Bussiness.Interfaces;
using TaskManager.Data.Entities;

namespace TaskManager.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TaskController> _logger;

        public TaskController(IUnitOfWork unitOfWork, ILogger<TaskController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tasks = await _unitOfWork.taskRepository.GetAllAsync();
            var dto = tasks.Select(t => new TaskReadDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedDate = t.CreatedDate,
                DueDate = t.DueDate,
                IsCompleted = t.IsCompleted,
                UserId = t.UserId,
            });
            return Ok(dto);
        }
        [HttpGet("{WithIncludes}")]
        public async Task<IActionResult> GetTasksWithComments()
        {
            var tasks = await _unitOfWork.taskRepository.GetAllWithIncludeAsync(t => t.Comments);
            var dto = tasks.Select(t => new TaskReadDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                CreatedDate = t.CreatedDate,
                DueDate = t.DueDate,
                IsCompleted = t.IsCompleted,
                UserId = t.UserId,
                Comments = t.Comments.Select(c => new CommentReadDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    UserId = c.UserId,
                }).ToList(),
            });
            return Ok(dto);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest("invalid id");
            var task = await _unitOfWork.taskRepository.GetByIdAsync(id);
            if (task == null)
                return NotFound("Task Not Found");
            var dto = new TaskReadDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                UserId = task.UserId,
            };
            return Ok(dto);
        }
        [HttpGet("{withComments}")]
        public async Task<IActionResult> GetByIdWithIncludes(int id)
        {
            var task = await _unitOfWork.taskRepository.GetByIdWithIncludesAsync(id,t=>t.Comments);
            if (task == null)
                return BadRequest("Task Not Found");
            var dto = new TaskReadDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CreatedDate = task.CreatedDate,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted,
                UserId = task.UserId,
                Comments = task.Comments.Select(t => new CommentReadDto
                {
                    Id = t.Id,
                    Content = t.Content,
                    CreatedAt = t.CreatedAt,
                    UserId = t.UserId,
                }).ToList()
            };
            return Ok(dto);
        }
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDto dto)
        {
            _logger.LogInformation("Create Task Started");
            if (!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(v=>v.Errors).Select(e=>e.ErrorMessage).ToList());
            // will take id from claim here
            var entity = new TaskItem
            {
                Title = dto.Title,
                Description = dto.Description,
                CreatedDate = dto.CreatedDate,
                DueDate = dto.DueDate,
                IsCompleted = dto.IsCompleted,
            };
            await _unitOfWork.taskRepository.AddAsync(entity);
            await _unitOfWork.Complete();

            var read = new TaskReadDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                CreatedDate = entity.CreatedDate,
                DueDate = entity.DueDate,
                IsCompleted = entity.IsCompleted,
            };
            return Ok(read);
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id ,[FromBody]TaskUpdateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList());
            var existing = await _unitOfWork.taskRepository.GetByIdAsync(id);
            if (existing == null)
                return NotFound("Task Not Found");
            //will do authorization here for admin and for task is not completed
            existing.Description = dto.Description;
            existing.DueDate= dto.DueDate;
            existing.Title = dto.Title; 
            _unitOfWork.taskRepository.Update(existing);
           await _unitOfWork.Complete();
            var read = new TaskReadDto
            {
                Id = existing.Id,
                Title = existing.Title,
                Description = existing.Description,
                DueDate = existing.DueDate,
                IsCompleted = existing.IsCompleted,
                UserId = existing.UserId,
            };
            return Ok(read);
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _unitOfWork.taskRepository.GetByIdAsync (id);
            if (existing == null)
                return NotFound("Task Not Found");
            // will add authorization here in the future
            _unitOfWork.taskRepository.Delete (existing);
            await _unitOfWork.Complete();
            return Ok("Task Deleted Succsfully");
        }
    }
}
