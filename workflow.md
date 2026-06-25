# Workflow

This file explains how the project works end to end — Angular frontend, ASP.NET Core backend, SQL Server database.

---

## 1. Architecture Overview

HRManage uses **Onion Architecture** inside a single `.csproj`. Layers are enforced by folder convention:

```
HRManagement.API/
│
├── Domain/                  ← Core — no external dependencies
│   ├── Entities/            Employee, User, Department, Contract, Leave
│   └── Interfaces/          IUserRepository, IEmployeeRepository
│
├── Application/             ← Use cases — depends only on Domain
│   ├── DTOs/                AppDtos.cs (all request/response shapes)
│   └── Interfaces/          ITokenService, ICredentialGeneratorService
│
├── Infrastructure/          ← Technical layer — EF Core, SQL, crypto
│   ├── Data/                AppDbContext.cs
│   ├── Repositories/        UserRepository, EmployeeRepository
│   └── Services/            TokenService, CredentialGeneratorService
│
└── Presentation/            ← Thin HTTP layer — only routing concerns
    └── Controllers/         AuthController, EmployeesController,
                             DepartmentsController, ContractsController,
                             LeavesController, MigrateController
```

**Dependency rule:** Each layer only depends on layers closer to the centre.
- Presentation → Application interfaces
- Infrastructure → implements Application + Domain interfaces
- Domain → depends on nothing

---

## 2. Login Flow

1. User opens the Angular app at `/` (landing page).
2. Clicks **Login** → navigated to `/login`.
3. Angular sends `POST /api/auth/login` with `{ username, password }`.
4. Backend finds the `User` row by username (must be `IsActive = true`).
5. BCrypt verifies the password hash.
6. If valid: backend returns a **JWT token** + user info (role, employeeId, fullName).
7. Angular stores the session object in `sessionStorage` via `AuthService`.
8. Angular route guards redirect:
   - `Admin` or `Manager` → `/admin/dashboard`
   - `Employee` → `/employee/profile`

---

## 3. Forgot Password Flow (Self-Service)

No email server required. The temporary password is shown directly on-screen.

1. User is on the login page and clicks **"Forgot your password?"**.
2. Angular navigates to `/forgot-password`.
3. User types their **username** and clicks **Generate Temporary Password**.
4. Angular calls `POST /api/auth/forgot-password` (no auth token needed).
5. Backend finds the user by username.
   - If not found: returns `200 OK` with `tempPassword: null` (no enumeration leak).
   - If found: generates a secure 12-character random password.
6. Backend hashes it with BCrypt, saves it to `Users.PasswordHash`.
7. Backend returns `{ tempPassword: "..." }` in the response.
8. Angular displays the temporary password on screen with a **clipboard copy button**.
9. User logs in with the temporary password.
10. User changes their password from the profile page.

**Security note:** The endpoint always returns HTTP 200 whether or not the username exists, to prevent username enumeration attacks.

---

## 4. Protected Route Flow

1. Angular route guards check `AuthService.isLoggedIn` (sessionStorage key present).
2. If no session → redirect to `/login?reason=sign-in-again`.
3. All API calls send the JWT as `Authorization: Bearer <token>`.
4. The .NET backend validates the JWT on every protected endpoint.
5. Role-based authorization (`[Authorize(Roles = "Admin,Manager")]`) is enforced at the controller level.

---

## 5. Admin Dashboard Flow

1. Dashboard component loads on mount:
   - `GET /api/employees` → employee table
   - `GET /api/departments` → department counts / dropdown
   - `GET /api/contracts` → contract stats
   - `GET /api/leaves` → leave stats
2. **Add Employee** opens the slide-in CRUD panel:
   - Submits `POST /api/employees`
   - Backend creates an `Employee` row + a `User` row atomically (transaction)
   - Generates a unique username (`firstname.lastname`)
   - Returns the temporary password — shown once in the success panel
3. **Edit** opens the same panel pre-filled → `PUT /api/employees/{id}`
4. **Delete** calls `DELETE /api/employees/{id}`
   - Blocked if the employee has active subordinates
5. **Reset Password** (admin action) → `POST /api/employees/{id}/reset-password`
   - Returns a new temporary password shown in the UI
6. **Create Account** (legacy fallback for employees without an account)
   → `POST /api/auth/create-account`

---

## 6. Employee Profile Flow

1. Employee logs in → routed to `/employee/profile`.
2. The profile page reads `employeeId` from the JWT claims (via `AuthService`).
3. Loads employee details: `GET /api/auth/me/{employeeId}`.
4. **Edit profile** → `PUT /api/employees/{id}/profile`
   - Updates: firstName, lastName, email, phone
5. **Change password** → `POST /api/auth/change-password`
   - Requires: currentPassword, newPassword (min 6 chars)
   - Backend verifies the current password with BCrypt before updating

---

## 7. Angular Services

### `auth.service.ts`
- `login(username, password)` — calls `POST /api/auth/login`, stores session
- `logout()` — clears sessionStorage
- `currentUser` — parsed session object (token, role, employeeId, fullName)
- `authHeaders` — builds `Authorization: Bearer ...` header for API calls
- `isLoggedIn`, `isAdmin`, `isManager` — boolean helpers for guards/templates

### `hr-api.ts`
Centralized service for all API calls. Uses `auth.authHeaders` automatically.

| Method | API call |
|--------|----------|
| `getEmployees()` | `GET /api/employees` |
| `createEmployee(payload)` | `POST /api/employees` |
| `updateEmployee(id, payload)` | `PUT /api/employees/{id}` |
| `deleteEmployee(id)` | `DELETE /api/employees/{id}` |
| `resetEmployeePassword(id)` | `POST /api/employees/{id}/reset-password` |
| `getDepartments()` | `GET /api/departments` |
| `getContracts()` | `GET /api/contracts` |
| `getLeaves()` | `GET /api/leaves` |
| `forgotPassword(username)` | `POST /api/auth/forgot-password` |
| `changePassword(payload)` | `POST /api/auth/change-password` |
| `getUserByEmployee(id)` | `GET /api/auth/user/{id}` |
| `resetPassword(employeeId, newPassword)` | `POST /api/auth/reset-password` |
| `createAccount(...)` | `POST /api/auth/create-account` |

---

## 8. Angular Routes

| Path | Component | Guard |
|------|-----------|-------|
| `/` | LandingComponent | None |
| `/login` | LoginComponent | None |
| `/forgot-password` | ForgotPassword | None |
| `/admin/dashboard` | Dashboard | `adminGuard` (Admin or Manager) |
| `/employee/profile` | EmployeeProfile | `employeeGuard` (any logged-in user) |
| `**` | → `/` | None |

---

## 9. Backend Service Registrations (`Program.cs`)

```csharp
// Application interfaces → Infrastructure implementations
builder.Services.AddScoped<ITokenService,               TokenService>();
builder.Services.AddScoped<ICredentialGeneratorService,  CredentialGeneratorService>();

// Domain interfaces → Infrastructure repositories
builder.Services.AddScoped<IUserRepository,     UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```

---

## 10. Database Model

| Table | Key relationships |
|-------|------------------|
| `Employees` | FK → `Departments` (nullable), self-FK `ManagerId` |
| `Users` | 1-to-1 FK → `Employees` (cascade delete) |
| `Contracts` | FK → `Employees` (cascade delete) |
| `Leaves` | FK → `Employees` (cascade delete) |
| `Departments` | Has many `Employees` |

Unique indexes: `Employees.Email`, `Users.Username`, `Users.EmployeeId`

---

## 11. Dev URLs

| Service | URL |
|---------|-----|
| Angular | `http://localhost:4200` |
| .NET API | `http://localhost:5037` |
| Swagger | `http://localhost:5037/swagger` |

---

## 12. Important Notes

- The old `Controllers/`, `Models/`, `Data/`, `Services/`, `DTOs/` folders are kept as **stub files** pointing to their new locations. Do not add code back to them.
- All new code goes in the `Domain/`, `Application/`, `Infrastructure/`, or `Presentation/` folders.
- Bootstrap Icons are loaded via **CDN** in each page's `<link>` tags (not from the local mazer vendor folder which lacks the CSS).
- The `MigrateController` exists only for dev-time password migration — remove it before production.
