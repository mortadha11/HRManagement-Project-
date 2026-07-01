using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRManagement.API.Domain.Entities;

public class EmployeeTask
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending"; // Pending, In Progress, Completed

    public DateTime? DueDate { get; set; }
    
    // Priority / Importance (Red=High, Yellow=Medium, Blue=Low)
    [MaxLength(20)]
    public string PriorityLevel { get; set; } = "Low";
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // The manager who created/assigned the task
    [Required]
    public int ManagerId { get; set; }
    
    [ForeignKey(nameof(ManagerId))]
    public Employee Manager { get; set; } = null!;

    // The employee assigned to the task
    [Required]
    public int EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee Assignee { get; set; } = null!;
}
