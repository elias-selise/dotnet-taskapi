using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace dotnet_taskapi.Models
{
    public enum TaskPriority
    {
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum TaskStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3
    }

    public class TaskItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = string.Empty;

        [BsonElement("title")]
        public string Title { get; set; } = string.Empty;

        [BsonElement("description")]
        public string Description { get; set; } = string.Empty;

        [BsonElement("priority")]
        [BsonRepresentation(BsonType.String)]
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        [BsonElement("status")]
        [BsonRepresentation(BsonType.String)]
        public TaskStatus Status { get; set; } = TaskStatus.Pending;

        [BsonElement("assignee")]
        public string Assignee { get; set; } = string.Empty;

        [BsonElement("dueDate")]
        public DateTime DueDate { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [BsonElement("version")]
        public int Version { get; set; } = 0;
    }
}
