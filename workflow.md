# Workflow

This file explains how the project works end to end, with focus on the code flow between Angular, ASP.NET Core, and SQL Server.

## 1. What this project is

HRManage is a full-stack HR management app.

- The Angular app is the user interface.
- The .NET API is the business logic and database gateway.
- SQL Server stores the persistent data.

The app has two main experiences:

- Admin / Manager dashboard
- Employee profile page

## 2. Main runtime flow

### Login flow

1. The user opens the Angular login page.
2. Angular sends the username and password to `POST /api/auth/login`.
3. The .NET backend checks the `Users` table.
4. The backend verifies the password with BCrypt.
5. If the credentials are valid, the backend creates a JWT token.
6. Angular stores the token and user info in `sessionStorage`.
7. Angular redirects the user according to the role:
   - `Admin` and `Manager` go to the dashboard
   - `Employee` goes to the profile page

### Protected route flow

1. Angular route guards check whether a session exists.
2. If no session exists, the user is redirected to `/login`.
3. If a protected page is opened after logout or session loss, the login page shows the sign-in-again message.
4. The token is sent in the `Authorization: Bearer ...` header for protected API calls.

## 3. Angular side

### Important files

- `hr-admin/src/app/services/auth.service.ts`
- `hr-admin/src/app/services/hr-api.ts`
- `hr-admin/src/app/guards/auth.guard.ts`
- `hr-admin/src/app/pages/auth/login/login.ts`
- `hr-admin/src/app/pages/admin/dashboard/dashboard.ts`
- `hr-admin/src/app/pages/admin/dashboard/dashboard.html`
- `hr-admin/src/app/pages/admin/dashboard/dashboard.scss`
- `hr-admin/src/app/pages/employee/profile/profile.ts`
- `hr-admin/src/app/pages/employee/profile/profile.html`
- `hr-admin/src/app/pages/employee/profile/profile.scss`
- `hr-admin/src/app/app.routes.ts`

### `auth.service.ts`

This file manages the session on the Angular side.

It is responsible for:

- logging in
- logging out
- saving the logged-in user in `sessionStorage`
- exposing the current user
- building the auth header for protected API calls

It also has helper methods that update the saved session after profile changes.

### `hr-api.ts`

This is the Angular API wrapper.

It centralizes all HTTP calls to the backend:

- employees
- departments
- contracts
- leaves
- auth endpoints

The components do not call `HttpClient` directly. They use `HrApi` instead.

This keeps the project cleaner because:

- URLs live in one place
- request shapes are reused
- authorization headers are added consistently

### `auth.guard.ts`

This file protects the routes.

It checks:

- whether the user is logged in
- whether the user is an admin or manager
- whether the employee is allowed on the profile page

If access is not allowed, the guard redirects to login.

### `login.ts`

This component:

- reads the login form
- sends credentials to the backend
- stores the session through `AuthService`
- redirects by role
- shows the sign-in-again message when needed

### `dashboard.ts`

This is the main admin / manager controller.

It handles:

- loading dashboard data
- searching employees
- opening the add/edit employee modal
- opening the employee detail panel
- deleting employees
- resetting passwords
- creating fallback user accounts

It talks to the backend only through `HrApi`.

### `dashboard.html`

This template renders:

- the sidebar
- the summary cards
- the employee list
- the add/edit modal
- the detail panel

The modal is used for both add and edit so the workflow stays consistent.

### `dashboard.scss`

This styles the dashboard layout, modal, detail panel, buttons, badges, and validation messages.

### `profile.ts`

This component handles the employee self-service page.

It lets the employee:

- update first name
- update last name
- update email
- update phone
- change their own password

### `profile.html`

This template shows:

- identity summary
- editable personal information
- password change section
- read-only HR fields

### `app.routes.ts`

This file maps the pages:

- `/login`
- `/admin/dashboard`
- `/employee/profile`

The route guards are attached here.

## 4. .NET side

### Important files

- `Program.cs`
- `Controllers/AuthController.cs`
- `Controllers/EmployeesController.cs`
- `DTOs/EmployeeDtos.cs`
- `Services/TokenService.cs`
- `Services/CredentialGeneratorService.cs`
- `Models/Employee.cs`
- `Models/User.cs`
- `Data/AppDbContext.cs`

### `Program.cs`

This file wires the API together.

It configures:

- SQL Server connection
- JWT authentication
- authorization
- controllers
- Swagger
- CORS for `http://localhost:4200`

So this is the startup file that defines how the API runs.

### `AuthController.cs`

This controller handles authentication and account management.

Main responsibilities:

- login
- get current employee data
- get user account by employee
- reset password for admin or manager
- create user account
- change the current employee password

The login action is anonymous.
The other actions require an authenticated user.

### `EmployeesController.cs`

This controller manages employee data.

Main responsibilities:

- list employees
- read one employee
- create employee
- update employee
- update employee profile
- delete employee
- reset employee password
- list by department
- list subordinates

It also enforces role rules and ownership rules.

### `DTOs/EmployeeDtos.cs`

This file defines request and response shapes used by the API.

Examples:

- `CreateEmployeeRequest`
- `UpdateEmployeeRequest`
- `CreateEmployeeResponse`
- `UpdateProfileRequest`
- `ResetEmployeePasswordRequest`

DTOs keep the API contract clear and avoid putting raw entity models directly on the wire.

### `TokenService.cs`

This service creates JWT tokens after login.

It puts claims into the token such as:

- user ID
- employee ID
- role
- username

The Angular app uses this token to call protected endpoints.

### `CredentialGeneratorService.cs`

This service generates account credentials.

It is used for:

- unique usernames
- temporary passwords

The admin create flow can also send a manual password from Angular.

### `Employee.cs` and `User.cs`

These are the main database models.

`Employee` stores the HR profile data:

- name
- email
- phone
- hire date
- salary
- department
- manager
- role-linked employee details

`User` stores the auth data:

- username
- password hash
- role
- account status
- login timestamps

The relation is one employee to one user account.

### `AppDbContext.cs`

This is the EF Core database context.

It maps the tables and relationships between:

- employees
- users
- departments
- contracts
- leaves

## 5. Database flow

### Employee creation

1. Angular sends employee data to `POST /api/employees`.
2. Backend validates the request.
3. Backend creates a row in `Employees`.
4. Backend creates a linked row in `Users`.
5. Backend returns the username and password used for the new account.

### Employee update

1. Angular sends the edited employee fields to `PUT /api/employees/{id}`.
2. Backend checks validity and uniqueness.
3. Backend updates the employee row.
4. If the employee has a linked user, the backend can also keep the user active and adjust the role if admin rules allow it.

### Profile update

1. Employee updates personal details in the profile page.
2. Angular sends the new values to `PUT /api/employees/{id}/profile`.
3. Backend verifies the current user is allowed to update that employee.
4. Backend updates only the allowed fields.

### Password change

1. Employee enters current password and a new password.
2. Angular sends it to `POST /api/auth/change-password`.
3. Backend checks the JWT employee ID.
4. Backend verifies the current password with BCrypt.
5. Backend stores the new hashed password.

### Password reset by admin or manager

1. Admin or manager opens the employee detail panel.
2. Angular sends a reset request.
3. Backend either generates a temporary password or accepts the manual one.
4. Backend stores the new hashed password.

## 6. How the pieces talk to each other

### Angular to .NET

Angular components do not access the database directly.

They call `HrApi`, which sends HTTP requests to the .NET API.

Example:

- profile page saves edits through `HrApi.updateEmployeeProfile`
- dashboard saves employee data through `HrApi.createEmployee` or `HrApi.updateEmployee`

### .NET to SQL Server

The API uses Entity Framework Core.

Controllers query and save through `AppDbContext`, and EF Core handles the SQL Server work behind the scenes.

### JWT flow

The login endpoint returns a token.

Angular stores it.
The token goes into the header.
The backend validates it on protected endpoints.

## 7. Main user workflows

### Admin workflow

1. Log in as admin.
2. Open dashboard.
3. Create employee in the modal.
4. Either use a suggested password or type one manually.
5. Edit employee in the same modal.
6. Open detail panel to see account info.
7. Reset password if needed.
8. Delete employee when allowed.

### Employee workflow

1. Log in as employee.
2. Open profile page.
3. Update personal information.
4. Change own password.
5. Log out.

## 8. What to read first in the code

If you want the shortest learning path, read in this order:

1. `Program.cs`
2. `Controllers/AuthController.cs`
3. `Controllers/EmployeesController.cs`
4. `hr-admin/src/app/services/auth.service.ts`
5. `hr-admin/src/app/services/hr-api.ts`
6. `hr-admin/src/app/pages/auth/login/login.ts`
7. `hr-admin/src/app/pages/admin/dashboard/dashboard.ts`
8. `hr-admin/src/app/pages/employee/profile/profile.ts`

That order shows the full chain from startup to UI behavior to database actions.

## 9. File map

### Root / backend startup

- `Program.cs`: boots the API, registers SQL Server, JWT auth, controllers, Swagger, and CORS.
- `appsettings.json`: holds the database connection string and JWT settings used by `Program.cs`.
- `workflow.md`: this guide.
- `README.md`: short project overview and run instructions.

### Auth and token flow

- `Controllers/AuthController.cs`: receives login requests, verifies passwords, generates password-change responses, and exposes account-related endpoints.
- `Services/TokenService.cs`: generates the JWT token after successful login.
- `Models/User.cs`: stores the username, hashed password, and role that the token is built from.
- `hr-admin/src/app/services/auth.service.ts`: stores the token in `sessionStorage`, exposes the current session, and creates the `Authorization` header.
- `hr-admin/src/app/guards/auth.guard.ts`: checks whether the token/session exists before allowing route access.
- `hr-admin/src/app/pages/auth/login/login.ts`: sends credentials to the API and routes the user after login.

### Employee data flow

- `Controllers/EmployeesController.cs`: does employee CRUD and the profile/password endpoints.
- `DTOs/EmployeeDtos.cs`: defines the request and response objects for employee create/update/profile/password work.
- `Models/Employee.cs`: stores the employee profile data in the database.
- `Data/AppDbContext.cs`: maps `Employee`, `User`, and the other tables to SQL Server.
- `hr-admin/src/app/services/hr-api.ts`: sends employee requests from Angular to the API.
- `hr-admin/src/app/pages/admin/dashboard/dashboard.ts`: loads the employee table, opens the modal, saves add/edit, deletes employees, and opens detail panels.
- `hr-admin/src/app/pages/employee/profile/profile.ts`: loads the current employee profile, saves personal changes, and changes the employee password.

### Supporting business files

- `Services/CredentialGeneratorService.cs`: creates usernames and temporary passwords for employee accounts.
- `Models/Department.cs`: department data used by the dashboard and profile display.
- `Models/Contract.cs`: contract data shown on the dashboard.
- `Models/Leave.cs`: leave request data shown on the dashboard.
- `Controllers/DepartmentsController.cs`: exposes department data to Angular.
- `Controllers/ContractsController.cs`: exposes contract data to Angular.
- `Controllers/LeavesController.cs`: exposes leave data to Angular.

### Angular page files

- `hr-admin/src/app/app.routes.ts`: defines application routes and attaches guards.
- `hr-admin/src/app/pages/admin/dashboard/dashboard.html`: renders the dashboard table, modal, and detail panel.
- `hr-admin/src/app/pages/admin/dashboard/dashboard.scss`: styles the dashboard screen.
- `hr-admin/src/app/pages/employee/profile/profile.html`: renders the profile page and password form.
- `hr-admin/src/app/pages/employee/profile/profile.scss`: styles the profile page.
- `hr-admin/src/app/pages/auth/login/login.html`: renders the login form.
- `hr-admin/src/app/pages/auth/login/login.scss`: styles the login page.

## 10. Token generation and usage, step by step

This is the exact chain for JWT.

1. `login.ts` sends username and password to `AuthController.Login`.
2. `AuthController.Login` finds the matching `User` row in the database.
3. `AuthController.Login` verifies the password using BCrypt.
4. `AuthController.Login` updates `LastLoginAt` on the user row.
5. `AuthController.Login` calls `TokenService.GenerateToken(user)`.
6. `TokenService` builds the JWT claims:
   - user ID
   - employee ID
   - role
   - username
7. `TokenService` signs the token with the JWT key from configuration.
8. `AuthController.Login` returns the token in the JSON response.
9. `auth.service.ts` stores the token and user info in `sessionStorage`.
10. `hr-api.ts` reads the token from `auth.service.ts` and sends it in the `Authorization` header.
11. `Program.cs` validates the token on protected endpoints.
12. `auth.guard.ts` uses the stored session to block or allow route navigation.

## 11. What each controller is for

- `AuthController.cs`: login, current user lookup, account creation, account password changes, and admin reset actions.
- `EmployeesController.cs`: employee CRUD, employee profile update, employee password reset, and subordinate lookups.
- `DepartmentsController.cs`: returns department data for dropdowns and summaries.
- `ContractsController.cs`: returns contract data for dashboard stats.
- `LeavesController.cs`: returns leave data for dashboard stats and approvals.

## 12. What each Angular service is for

- `auth.service.ts`: session state, login/logout, current user, auth headers.
- `hr-api.ts`: one place for all backend calls.

## 13. What each page does

- Login page: authenticates and redirects by role.
- Admin dashboard: manage employees and account state.
- Employee profile: self-service profile updates and password change.

## 14. Very short dependency chain

- `login.ts` -> `AuthController.cs` -> `TokenService.cs`
- `dashboard.ts` / `profile.ts` -> `hr-api.ts` -> API controllers
- API controllers -> `AppDbContext.cs` -> SQL Server
- `auth.service.ts` -> `sessionStorage` -> route guards / headers

## 15. Current local URLs

- Angular: `http://localhost:4200`
- API: `http://localhost:5037`
- Swagger: `http://localhost:5037/swagger`

## 16. Short version

In simple terms:

- Angular handles screens and user interaction.
- .NET handles validation, auth, and database rules.
- SQL Server stores the data.
- `HrApi` connects the screens to the backend.
- JWT keeps the session protected.
