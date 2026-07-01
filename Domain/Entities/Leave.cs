namespace HRManagement.API.Domain.Entities;

public class Leave
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    
    public string Type { get; set; } = string.Empty; // Vacation, Sick Leave, Unpaid, Other
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRequested { get; set; } // Calculated excluding weekends
    
    public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
    public string? Reason { get; set; }
    
    // Workflow tracking
    public DateTime? ModeratedAt { get; set; }
    public int? ModeratedById { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Employee? Employee { get; set; }
}
