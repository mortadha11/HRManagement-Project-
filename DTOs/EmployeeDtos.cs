
namespace HRManagement.API.DTOs
{
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

    public class CreateEmployeeResponse
    {
        public int     EmployeeId    { get; set; }
        public string  FirstName     { get; set; } = "";
        public string  LastName      { get; set; } = "";
        public string  Email         { get; set; } = "";
        public string  Username      { get; set; } = "";
        public string  TemporaryPassword { get; set; } = "";
        public string  Role          { get; set; } = "";
    }

    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = "";
        public string LastName  { get; set; } = "";
        public string Email     { get; set; } = "";
        public string? Phone    { get; set; }
    }

    public class ResetEmployeePasswordRequest
    {
        public string? NewPassword { get; set; }
    }
}
