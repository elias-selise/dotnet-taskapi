using dotnet_taskapi.Models;
using dotnet_taskapi.Services;
using Microsoft.AspNetCore.Mvc;
using TaskStatus = dotnet_taskapi.Models.TaskStatus;

namespace dotnet_taskapi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskRepository _repository;

        public TasksController(ITaskRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<ActionResult<List<TaskItem>>> GetAll()
        {
            var tasks = await _repository.GetAllAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItem>> GetById(string id)
        {
            var task = await _repository.GetByIdAsync(id);

            return Ok(new
            {
                id = task.Id,
                title = task.Title,
                description = task.Description,
                priority = task.Priority.ToString(),
                status = task.Status.ToString(),
                assignee = task.Assignee,
                dueDate = task.DueDate,
                createdAt = task.CreatedAt,
                updatedAt = task.UpdatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult<TaskItem>> Create([FromBody] CreateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var exists = await _repository.CheckTitleExistsAsync(dto.Title);
            if (exists)
                return Conflict(new { message = "A task with this title already exists." });

            var task = await _repository.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItem>> Update(string id, [FromBody] UpdateTaskDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _repository.UpdateAsync(id, dto);
            if (updated == null)
                return NotFound(new { message = "Task not found." });

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(string id)
        {
            var deleted = await _repository.DeleteAsync(id);
            if (!deleted)
                return NotFound(new { message = "Task not found." });

            return NoContent();
        }

        [HttpGet("assignee/{assignee}")]
        public ActionResult<List<TaskItem>> GetByAssignee(string assignee)
        {
            var tasks = _repository.GetByAssigneeAsync(assignee).Result;

            return Ok(tasks);
        }

        [HttpGet("summary")]
        public async Task<ActionResult> GetSummary()
        {
            var allTasks = await _repository.GetAllAsync();

            var summary = new
            {
                total = allTasks.Count,
                pending = allTasks.Count(t => t.Status == TaskStatus.Pending),
                inProgress = allTasks.Count(t => t.Status == TaskStatus.InProgress),
                completed = allTasks.Count(t => t.Status == TaskStatus.Completed),
                highPriority = allTasks.Count(t => t.Priority == TaskPriority.High),
                overdue = allTasks.Count(t => t.DueDate < DateTime.UtcNow && t.Status != TaskStatus.Completed)
            };

            return Ok(summary);
        }
    }
}
