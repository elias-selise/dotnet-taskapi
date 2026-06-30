using dotnet_taskapi.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskStatus = dotnet_taskapi.Models.TaskStatus;

namespace dotnet_taskapi.Services
{
    public interface ITaskRepository
    {
        Task<List<TaskItem>> GetAllAsync(TaskStatus? status = null, TaskPriority? priority = null);
        Task<TaskItem?> GetByIdAsync(string id);
        Task<TaskItem> CreateAsync(CreateTaskDto dto);
        Task<TaskItem?> UpdateAsync(string id, UpdateTaskDto dto);
        Task<bool> DeleteAsync(string id);
        Task<bool> CheckTitleExistsAsync(string title, string? excludeId = null);
        Task<List<TaskItem>> GetByAssigneeAsync(string assignee);
    }

    public class TaskRepository : ITaskRepository
    {
        private static readonly SemaphoreSlim FileLock = new(1, 1);
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public TaskRepository(IWebHostEnvironment environment)
        {
            var dataDirectory = Path.Combine(environment.ContentRootPath, "Data");
            Directory.CreateDirectory(dataDirectory);
            _filePath = Path.Combine(dataDirectory, "tasks.json");
        }

        public async Task<List<TaskItem>> GetAllAsync(TaskStatus? status = null, TaskPriority? priority = null)
        {
            var tasks = await ReadTasksAsync();
            if (status.HasValue || priority.HasValue)
            {
                tasks = tasks.Where(t =>
                    (status.HasValue && t.Status == status.Value) ||
                    (priority.HasValue && t.Priority == priority.Value)).ToList();
            }

            return tasks;
        }

        public async Task<TaskItem?> GetByIdAsync(string id)
        {
            var tasks = await ReadTasksAsync();
            return tasks.FirstOrDefault(t => t.Id == id);
        }

        public async Task<TaskItem> CreateAsync(CreateTaskDto dto)
        {
            await FileLock.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var task = new TaskItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Title = dto.Title,
                    Description = dto.Description,
                    Priority = dto.Priority,
                    Status = TaskStatus.Pending,
                    Assignee = dto.Assignee,
                    DueDate = dto.DueDate,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Version = 0
                };

                var tasks = await ReadTasksFileAsync();
                tasks.Add(task);
                await WriteTasksFileAsync(tasks);

                return task;
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<TaskItem?> UpdateAsync(string id, UpdateTaskDto dto)
        {
            await FileLock.WaitAsync();
            try
            {
                var tasks = await ReadTasksFileAsync();
                var existingTask = tasks.FirstOrDefault(t => t.Id == id);

                if (existingTask == null)
                    return null;

                existingTask.Title = dto.Title ?? existingTask.Title;
                existingTask.Description = dto.Description ?? existingTask.Description;
                existingTask.Priority = dto.Priority ?? existingTask.Priority;
                existingTask.Status = dto.Status ?? existingTask.Status;
                existingTask.Assignee = dto.Assignee ?? existingTask.Assignee;
                existingTask.DueDate = dto.DueDate ?? existingTask.DueDate;
                existingTask.UpdatedAt = DateTime.UtcNow;
                existingTask.Version++;

                await WriteTasksFileAsync(tasks);

                return existingTask;
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            await FileLock.WaitAsync();
            try
            {
                var tasks = await ReadTasksFileAsync();
                var removedCount = tasks.RemoveAll(t => t.Id == id);

                if (removedCount == 0)
                    return false;

                await WriteTasksFileAsync(tasks);
                return true;
            }
            finally
            {
                FileLock.Release();
            }
        }

        public async Task<bool> CheckTitleExistsAsync(string title, string? excludeId = null)
        {
            var tasks = await ReadTasksAsync();

            return tasks.Any(t =>
                string.Equals(t.Title, title, StringComparison.OrdinalIgnoreCase) &&
                (string.IsNullOrEmpty(excludeId) || t.Id != excludeId));
        }

        public async Task<List<TaskItem>> GetByAssigneeAsync(string assignee)
        {
            var tasks = await ReadTasksAsync();
            return tasks.Where(t => t.Assignee == assignee).ToList();
        }

        private async Task<List<TaskItem>> ReadTasksAsync()
        {
            await FileLock.WaitAsync();
            try
            {
                return await ReadTasksFileAsync();
            }
            finally
            {
                FileLock.Release();
            }
        }

        private async Task<List<TaskItem>> ReadTasksFileAsync()
        {
            if (!File.Exists(_filePath))
                return new List<TaskItem>();

            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<TaskItem>>(stream, _jsonOptions) ?? new List<TaskItem>();
        }

        private async Task WriteTasksFileAsync(List<TaskItem> tasks)
        {
            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, tasks, _jsonOptions);
        }
    }
}
