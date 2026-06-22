# HRManagement API

Full-stack HR management application built with ASP.NET Core, Entity Framework Core, SQL Server, and Angular.

## What this project does now

- Authenticates users with JWT and BCrypt.
- Routes users by role:
  - `Admin` and `Manager` go to the dashboard.
  - `Employee` goes to the profile page.
- Manages employees from the admin dashboard.
- Stores and reads data from SQL Server.
- Exposes API endpoints for employees, departments, contracts, leaves, and auth.
- Lets an employee edit their personal fields from the profile page.

## Current workflow

### Login
1. User opens the Angular app.
2. User logs in with username and password.
3. Backend verifies the password with BCrypt.
4. Backend returns a JWT token and the user role.
5. Angular stores the session in `sessionStorage`.
6. Guards send the user to the right page.

### Admin dashboard
1. Dashboard loads employees, departments, contracts, and leaves from the API.
2. The employee table shows active employees only.
3. `Add Employee` opens a modal form.
4. Saving a new employee creates:
   - a row in `Employees`
   - a linked row in `Users`
   - a generated username
   - a password that can be suggested from the employee name and phone, or typed manually
5. `Edit` opens the same modal and updates the employee record.
6. `Delete` removes the employee from the database, and the API blocks deleting a manager who still has active subordinates.
7. `Reset password` accepts either a typed password or a generated one for the linked user account.

### Employee profile
1. The employee logs in.
2. The profile page loads the employee by `employeeId` from the token.
3. The employee can update first name, last name, email, and phone.
4. The employee can also change their own password by confirming the current password first.

## Backend structure

- `Program.cs` configures SQL Server, JWT, Swagger, CORS, and JSON handling.
- `Data/AppDbContext.cs` defines the EF Core database model and relationships.
- `Models/` contains `Employee`, `Department`, `Contract`, `Leave`, and `User`.
- `Controllers/AuthController.cs` handles login, current user info, account creation, and password reset.
- `Controllers/EmployeesController.cs` handles employee CRUD and the employee/password workflow.
- `Controllers/DepartmentsController.cs`, `ContractsController.cs`, `LeavesController.cs` expose the other HR entities.
- `Services/TokenService.cs` builds JWT tokens.
- `Services/CredentialGeneratorService.cs` generates unique usernames and temporary passwords.
- `DTOs/EmployeeDtos.cs` defines request/response payloads for employee creation and update.

## Frontend structure

- `src/app/pages/auth/login/` contains the login screen.
- `src/app/pages/admin/dashboard/` contains the admin/manager dashboard.
- `src/app/pages/employee/profile/` contains the employee profile screen.
- `src/app/services/auth.service.ts` stores the session user and JWT.
- `src/app/services/hr-api.ts` wraps the API calls with the Bearer token.
- `src/app/guards/auth.guard.ts` protects routes by login state and role.

## Database model

Main relationships:

- `Employee` belongs to an optional `Department`.
- `Employee` can have one `Manager` and many `Subordinates`.
- `Employee` can have many `Contracts`.
- `Employee` can have many `Leaves`.
- `User` is linked one-to-one with `Employee`.

## Important notes

- The admin create flow is now the recommended path. It creates the employee and the user account together.
- Manual account creation still exists in the dashboard detail panel as a fallback for older employees with no account.
- The admin add and edit form now opens as a modal instead of living inline on the page.
- The project currently uses local development URLs:
  - API: `http://localhost:5037`
  - Angular: `http://localhost:4200`
- The Angular dashboard stylesheet is a little larger than the default budget because the page is doing a lot of work in one place.

## Run locally

### Backend
```powershell
cd C:\Users\MORTADHA2\HRManagement\HRManagement.API
dotnet run
```

### Frontend
```powershell
cd C:\Users\MORTADHA2\HRManagement\HRManagement.API\hr-admin
npm start
```

## What is still worth improving

- Split the dashboard into smaller pages or components.
- Remove the fallback manual account creation once you no longer need legacy support.
- Add more role checks on any remaining sensitive endpoints.
- Add tests for employee creation, password reset, and route guards.
- Clean up any remaining encoding artifacts in old files if you see them in the editor.
