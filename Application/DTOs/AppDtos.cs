namespace HRManagement.API.Application.DTOs;

// ── Employee requests ──────────────────────────────────────
public class CreateEmployeeRequest
{
    public string  FirstName    { get; set; } = "";
    public string  LastName     { get; set; } = "";
    public string  Email        { get; set; } = "";
    public string? Phone        { get; set; }
    public DateTime HireDate    { get; set; }
    public decimal? Salary      { get; set; }
    public int?     DepartmentId{ get; set; }
    public string?  JobTitle    { get; set; }
    public string?  JobLevel    { get; set; }
    public int?     ManagerId   { get; set; }
    public string   Role        { get; set; } = "Employee";
    public string?  Password    { get; set; }
}

public class UpdateEmployeeRequest
{
    public string  FirstName    { get; set; } = "";
    public string  LastName     { get; set; } = "";
    public string  Email        { get; set; } = "";
    public string? Phone        { get; set; }
    public DateTime HireDate    { get; set; }
    public decimal? Salary      { get; set; }
    public int?     DepartmentId{ get; set; }
    public string?  JobTitle    { get; set; }
    public string?  JobLevel    { get; set; }
    public int?     ManagerId   { get; set; }
    public bool     IsActive    { get; set; } = true;
    public string?  Role        { get; set; }
}

public class UpdateProfileRequest
{
    public string  FirstName    { get; set; } = "";
    public string  LastName     { get; set; } = "";
    public string  Email        { get; set; } = "";
    public string? Phone        { get; set; }
}

public class ResetEmployeePasswordRequest
{
    public string? NewPassword { get; set; }
}

// ── Employee responses ─────────────────────────────────────
public class CreateEmployeeResponse
{
    public int    EmployeeId        { get; set; }
    public string FirstName         { get; set; } = "";
    public string LastName          { get; set; } = "";
    public string Email             { get; set; } = "";
    public string Username          { get; set; } = "";
    public string TemporaryPassword { get; set; } = "";
    public string Role              { get; set; } = "";
}

// ── Auth requests ──────────────────────────────────────────
public class LoginRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class ForgotPasswordRequest
{
    public string Username { get; set; } = "";
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword     { get; set; } = "";
}

public class CreateAccountRequest
{
    public int    EmployeeId { get; set; }
    public string Username   { get; set; } = "";
    public string Password   { get; set; } = "";
    public string Role       { get; set; } = "Employee";
}

public class ResetPasswordRequest
{
    public int    EmployeeId  { get; set; }
    public string NewPassword { get; set; } = "";
}

// ── Task requests/responses ────────────────────────────────

public class CreateTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string PriorityLevel { get; set; } = "Low";
    public int EmployeeId { get; set; } // Assignee
}

public class UpdateTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Status { get; set; }
    public string? PriorityLevel { get; set; }
}

public class UpdateTaskStatusRequest
{
    public string Status { get; set; } = "Pending";
}

// ── Contract requests ──────────────────────────────────────

public class CreateContractRequest
{
    public int EmployeeId { get; set; }
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Salary { get; set; }
    public string? Position { get; set; }
    public int? WorkingHours { get; set; }
}

public class UpdateContractRequest
{
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Salary { get; set; }
    public string? Position { get; set; }
    public int? WorkingHours { get; set; }
    public string Status { get; set; } = "Active";
}

// ── Leave requests ─────────────────────────────────────────

public class CreateLeaveRequest
{
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
}

public class UpdateLeaveStatusRequest
{
    public string Status { get; set; } = "Approved"; // Approved or Rejected
}

// ── Outgoing DTOs ──────────────────────────────────────────

public class ContractDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Type { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Salary { get; set; }
    public string? Position { get; set; }
    public int? WorkingHours { get; set; }
    public string Status { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class LeaveDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = "";
    public string Type { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysRequested { get; set; }
    public string Status { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public int? ModeratedById { get; set; }
    public string? ModeratorName { get; set; }
    public DateTime CreatedAt { get; set; }
}
