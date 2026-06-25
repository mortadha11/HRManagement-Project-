# Workflow

This file explains how the project works end to end — Angular frontend, ASP.NET Core backend, SQL Server database.

---

## Table of Contents

1. [What is Onion Architecture — and why it was chosen](#-what-is-onion-architecture--and-why-it-was-chosen)
2. [Architecture Layers of this Project](#-architecture-layers-of-this-project)
3. [Login Flow](#-login-flow)
4. [Forgot Password — Complete Deep-Dive](#-forgot-password--complete-deep-dive)
5. [Admin Dashboard Flow](#-admin-dashboard-flow)
6. [Employee Profile Flow](#-employee-profile-flow)
7. [Angular Services Reference](#-angular-services-reference)
8. [Angular Routes](#-angular-routes)
9. [Backend DI Registrations](#-backend-di-registrations)
10. [Database Model](#-database-model)
11. [Dev URLs](#-dev-urls)

---

## 🏛 What is Onion Architecture — and why it was chosen

### The core idea

Onion Architecture organizes a system into **concentric layers** (like the rings of an onion). Each layer can only depend on layers **closer to the centre** — never outward. The centre is the business core; the outside is external infrastructure.

```
        ┌──────────────────────────────────┐
        │         Presentation             │  ← HTTP, Controllers
        │   ┌──────────────────────────┐   │
        │   │      Infrastructure      │   │  ← DB, EF Core, JWT, BCrypt
        │   │   ┌──────────────────┐   │   │
        │   │   │   Application    │   │   │  ← DTOs, Use-Case Interfaces
        │   │   │  ┌────────────┐  │   │   │
        │   │   │  │   Domain   │  │   │   │  ← Entities, Domain Interfaces
        │   │   │  └────────────┘  │   │   │
        │   │   └──────────────────┘   │   │
        │   └──────────────────────────┘   │
        └──────────────────────────────────┘

        Dependency direction: always → inward
```

### Comparing it to other architectures

| Architecture | How it organises code | Main problem |
|---|---|---|
| **Monolith / no structure** | Everything in one folder or scattered | No separation of concern. Changing the DB breaks business logic |
| **Layered (3-tier)** | UI → Business Logic → Data Access | Dependencies flow top-down, so the DB layer bleeds into business logic |
| **MVC** | Model, View, Controller | Good for UI patterns but says nothing about where business rules live |
| **Clean / Onion** | Domain at the centre, infrastructure at the edge | Business rules never depend on DB or HTTP — easy to test and replace |

### Why Onion matters in practice

- **You can replace the database** without touching business logic (just swap the `Infrastructure/Repositories/` classes)
- **You can unit-test** `Domain` and `Application` logic without spinning up a database or web server
- **Interfaces decouple** the layers — the `Application` layer only knows `IUserRepository` (an interface), not EF Core
- **The `Domain` is pure C#** — no NuGet packages, no frameworks, just plain classes and interfaces

### What we had before

Before the migration the project was **monolithic**:

```
Controllers/          ← AuthController had login, password, user creation
Models/               ← Entity classes mixed with DB concerns
Data/                 ← AppDbContext
Services/             ← TokenService, CredentialGeneratorService
DTOs/                 ← Request/response bodies
```

Controllers called `AppDbContext` directly, called services directly, and contained all business rules inline.
Any change to the DB schema meant editing controllers. Any change to auth logic touched the same file as employee logic.

---

## 🗂 Architecture Layers of this Project

```
HRManagement.API/
│
├── Domain/                           ← Core — zero external dependencies
│   ├── Entities/
│   │   ├── Employee.cs               Pure C# class — no EF attributes
│   │   ├── User.cs                   Linked 1-to-1 with Employee
│   │   ├── Department.cs
│   │   ├── Contract.cs
│   │   └── Leave.cs
│   └── Interfaces/
│       ├── IUserRepository.cs        Contract for user data access
│       └── IEmployeeRepository.cs    Contract for employee data access
│
├── Application/                      ← Use cases — depends only on Domain
│   ├── DTOs/
│   │   └── AppDtos.cs                All request/response shapes
│   └── Interfaces/
│       ├── ITokenService.cs          Contract for JWT generation
│       └── ICredentialGeneratorService.cs  Contract for username/password generation
│
├── Infrastructure/                   ← Technical detail — depends on Application + Domain
│   ├── Data/
│   │   └── AppDbContext.cs           EF Core DbContext, relationship config, indexes
│   ├── Repositories/
│   │   ├── UserRepository.cs         Implements IUserRepository using EF Core
│   │   └── EmployeeRepository.cs     Implements IEmployeeRepository using EF Core
│   └── Services/
│       ├── TokenService.cs           Implements ITokenService using JwtSecurityToken
│       └── CredentialGeneratorService.cs  Implements ICredentialGeneratorService
│
├── Presentation/                     ← HTTP layer — depends on Application interfaces
│   └── Controllers/
│       ├── AuthController.cs         Login, forgot-password, change-password, account mgmt
│       ├── EmployeesController.cs    Employee CRUD, profile update, password reset
│       ├── DepartmentsController.cs  Department CRUD
│       ├── ContractsController.cs    Contract CRUD
│       ├── LeavesController.cs       Leave CRUD + status update
│       └── MigrateController.cs      Dev-only BCrypt migration helpers
│
├── Migrations/                       EF Core migration snapshots (auto-generated)
└── Program.cs                        DI wiring — binds interfaces → implementations
```

### The dependency rule in action

```
AuthController (Presentation)
    ↓ uses
ForgotPasswordRequest (Application.DTOs)
    ↓ data flows to
AppDbContext (Infrastructure.Data)
    ↓ queries
User (Domain.Entities)
```

`AuthController` never directly imports `TokenService` — it imports `ITokenService`.
If we swap `TokenService` for a different JWT library, only `Infrastructure/` changes.

---

## 🔐 Login Flow

1. User opens Angular app at `/` (landing page)
2. Clicks **Login** → navigated to `/login`
3. Angular sends `POST /api/auth/login` with `{ username, password }` — **no token needed**
4. `AuthController.Login()` queries `Users` table (with Employee JOIN)
5. BCrypt verifies the password hash against `Users.PasswordHash`
6. If valid: `TokenService.GenerateToken()` builds a JWT with claims: `sub`, `employeeId`, `role`, `unique_name`
7. Response returns: `{ token, userId, username, role, employeeId, fullName }`
8. Angular's `AuthService.login()` stores the session in `sessionStorage`
9. Guards redirect: Admin/Manager → `/admin/dashboard`, Employee → `/employee/profile`

---

## 🔑 Forgot Password — Complete Deep-Dive

### Why no email?

This is an internal HR system. Sending emails requires an SMTP server, email templates, and an external dependency. For an internal tool it is simpler and faster to generate the password on demand and display it directly on screen. An administrator can also see the Swagger endpoint if needed.

### The complete file-by-file journey

```
User browser
    │
    │  clicks "Forgot your password?" on login page
    ▼
login.html                            ← [Frontend / Angular template]
    Contains the anchor tag:
    <a routerLink="/forgot-password">Forgot your password?</a>
    Role: entry point — directs the user to the recovery page
    │
    │  Angular Router navigates
    ▼
app.routes.ts                         ← [Frontend / Angular routing]
    { path: 'forgot-password', loadComponent: () => import('./pages/auth/forgot-password/forgot-password') }
    Role: maps the URL /forgot-password to the ForgotPassword component (lazy-loaded)
    │
    ▼
forgot-password.html                  ← [Frontend / Angular template]
    Shows a single input field for the username.
    Three states rendered with *ngIf:
      state === 'form'    → shows the username input + button
      state === 'loading' → shows "Processing..." (button disabled)
      state === 'success' → shows the generated temporary password in a highlighted box
      state === 'error'   → shows an error message + retry button
    Role: the user interface for the entire recovery flow
    │
    │  user types username, clicks button
    ▼
forgot-password.ts                    ← [Frontend / Angular component class]
    export class ForgotPassword {
      submit() {
        this.state = 'loading';
        this.api.forgotPassword(username).subscribe({
          next: (res) => {
            this.tempPassword = res.tempPassword;
            this.state = 'success';
            this.cdr.detectChanges();   // ← forces re-render (Fetch API zone fix)
          }
        });
      }
    }
    Role: state machine + calls the API service + forces Angular re-render after async response
    Key detail: uses ChangeDetectorRef.detectChanges() because Angular's withFetch()
    provider can deliver HTTP responses outside the NgZone, which means the template
    would stay frozen without the manual detectChanges() call.
    │
    │  calls forgotPassword(username)
    ▼
hr-api.ts                             ← [Frontend / Angular HTTP service]
    forgotPassword(username: string): Observable<{ message: string; tempPassword: string | null }> {
      return this.http.post(`http://localhost:5037/api/auth/forgot-password`, { username });
    }
    Role: sends the HTTP POST to the backend — no Authorization header (anonymous endpoint)
    │
    │  HTTP POST /api/auth/forgot-password  { username: "admin" }
    ▼
AuthController.cs  (Presentation layer)   ← [Backend / ASP.NET Core controller]
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    Role: receives the HTTP request, validates the input, calls EF Core,
          always returns HTTP 200 (prevents username enumeration attacks)
    │
    │  queries DB using AppDbContext
    ▼
AppDbContext.cs  (Infrastructure.Data)    ← [Backend / EF Core]
    SELECT TOP(1) FROM [Users] WHERE Username = @username AND IsActive = 1
    Role: translates the LINQ query into SQL and executes against SQL Server
    │
    │  if user found
    ▼
GenerateTempPassword()  (in AuthController)  ← [Backend / inline helper]
    Uses Random to pick characters from upper/lower/digit/symbol sets.
    Guarantees at least one of each class. Shuffles the result.
    Returns a 12-character string like: "Kp7!mRxBn2@q"
    Role: generates a cryptographically-varied password that passes most complexity rules
    │
    ▼
BCrypt.Net.BCrypt.HashPassword(tempPassword)  ← [Backend / BCrypt library]
    Hashes the plain-text temporary password with a salt.
    Result stored in Users.PasswordHash column.
    Role: never store plain-text passwords — even temporary ones
    │
    ▼
UPDATE [Users] SET [PasswordHash] = @hash WHERE Id = @id   ← [SQL Server]
    The user's password in the database is now the hashed temporary password.
    │
    │  returns { message, tempPassword: "Kp7!mRxBn2@q" }
    ▼
AuthController returns HTTP 200 OK
    {
      "message": "Temporary password generated. Log in and change it immediately.",
      "tempPassword": "Kp7!mRxBn2@q"
    }
    │
    ▼
hr-api.ts  Observable<next>               ← [Frontend]
    The HTTP response body is received.
    │
    ▼
forgot-password.ts  next() callback
    this.tempPassword = "Kp7!mRxBn2@q";
    this.state = 'success';
    this.cdr.detectChanges();
    │
    ▼
forgot-password.html  re-renders
    The success <div *ngIf="state === 'success'"> becomes visible.
    The temporary password is displayed in a large indigo box.
    A clipboard copy button lets the user copy it without typos.
    │
    ▼
User copies the password and clicks "Go to Login"
    Navigates to /login, logs in with username + temporary password.
    Then navigates to their profile page and sets a permanent password.
```

### Security notes

| Decision | Reason |
|---|---|
| Always return HTTP 200 | Prevents attackers from discovering which usernames exist |
| Hash the temp password with BCrypt | Same security as a permanent password — never plain text in the DB |
| Show password on screen | Internal system — no SMTP server needed, admin can see Swagger if needed |
| Password complexity | Generator guarantees upper + lower + digit + symbol |
| No expiry | Simple internal tool — out of scope; add if needed |

---

## 📊 Admin Dashboard Flow

1. Dashboard loads on mount:
   - `GET /api/employees` → employee table
   - `GET /api/departments` → department counts / dropdown
   - `GET /api/contracts` → contract stats
   - `GET /api/leaves` → leave stats
2. **Add Employee** → `POST /api/employees`
   - Backend creates Employee + User rows in a transaction
   - Generates unique username (`firstname.lastname`)
   - Returns temporary password shown once in the UI
3. **Edit** → `PUT /api/employees/{id}`
4. **Delete** → `DELETE /api/employees/{id}` (blocked if has active subordinates)
5. **Reset Password** → `POST /api/employees/{id}/reset-password`

---

## 👤 Employee Profile Flow

1. Employee logs in → routed to `/employee/profile`
2. Profile reads `employeeId` from JWT claim via `AuthService`
3. Loads: `GET /api/auth/me/{employeeId}`
4. Edit profile → `PUT /api/employees/{id}/profile`
5. Change password → `POST /api/auth/change-password` (requires current password)

---

## 🛠 Angular Services Reference

### `auth.service.ts`
Manages the session in `sessionStorage`.

| Method / Property | Purpose |
|---|---|
| `login(username, password)` | Calls `/api/auth/login`, stores session |
| `logout()` | Clears sessionStorage |
| `currentUser` | Parsed session: token, role, employeeId, fullName |
| `authHeaders` | Builds `Authorization: Bearer ...` header |
| `isLoggedIn` | Boolean — session exists |
| `isAdmin` | Boolean — role === 'Admin' |
| `isManager` | Boolean — role === 'Manager' |

### `hr-api.ts`
All API calls, auto-attaches auth header via `this.options`.

| Method | Endpoint | Auth |
|---|---|---|
| `getEmployees()` | GET /api/employees | ✅ Bearer |
| `createEmployee(p)` | POST /api/employees | ✅ Bearer |
| `updateEmployee(id, p)` | PUT /api/employees/{id} | ✅ Bearer |
| `deleteEmployee(id)` | DELETE /api/employees/{id} | ✅ Bearer |
| `resetEmployeePassword(id, p)` | POST /api/employees/{id}/reset-password | ✅ Bearer |
| `getDepartments()` | GET /api/departments | ✅ Bearer |
| `getContracts()` | GET /api/contracts | ✅ Bearer |
| `getLeaves()` | GET /api/leaves | ✅ Bearer |
| `forgotPassword(username)` | POST /api/auth/forgot-password | ❌ None |
| `changePassword(p)` | POST /api/auth/change-password | ✅ Bearer |
| `getUserByEmployee(id)` | GET /api/auth/user/{id} | ✅ Bearer |
| `resetPassword(id, pw)` | POST /api/auth/reset-password | ✅ Bearer |
| `createAccount(p)` | POST /api/auth/create-account | ✅ Bearer |

---

## 🗺 Angular Routes

| URL | Component | Guard |
|---|---|---|
| `/` | `LandingComponent` | None |
| `/login` | `LoginComponent` | None |
| `/forgot-password` | `ForgotPassword` | None |
| `/admin/dashboard` | `Dashboard` | `adminGuard` (Admin or Manager) |
| `/employee/profile` | `EmployeeProfile` | `employeeGuard` (any logged-in) |
| `**` | redirect → `/` | None |

---

## ⚙ Backend DI Registrations (`Program.cs`)

```csharp
// Application interfaces → Infrastructure implementations
builder.Services.AddScoped<ITokenService,               TokenService>();
builder.Services.AddScoped<ICredentialGeneratorService,  CredentialGeneratorService>();

// Domain interfaces → Infrastructure repositories
builder.Services.AddScoped<IUserRepository,     UserRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
```

The controllers receive these via constructor injection.
Switching the JWT library means only changing `TokenService.cs` and the registration line — zero controller changes.

---

## 🗄 Database Model

| Table | Key relationships |
|---|---|
| `Employees` | FK → `Departments` (nullable, SetNull on delete), self-FK `ManagerId` (Restrict) |
| `Users` | 1-to-1 FK → `Employees` (cascade delete) |
| `Contracts` | FK → `Employees` (cascade delete) |
| `Leaves` | FK → `Employees` (cascade delete) |
| `Departments` | Has many `Employees` |

Unique indexes: `Employees.Email`, `Users.Username`, `Users.EmployeeId`
Decimal precision: `Employees.Salary` and `Contracts.Salary` → `(10, 2)`

---

## 🌐 Dev URLs

| Service | URL |
|---|---|
| Angular | `http://localhost:4200` |
| .NET API | `http://localhost:5037` |
| Swagger | `http://localhost:5037/swagger` |

---

## ⚠ Important Notes

- Old `Controllers/`, `Models/`, `Data/`, `Services/`, `DTOs/` folders **were deleted**. All code is in the Onion layers.
- Migrations in `Migrations/` use namespace `HRManagement.API.Infrastructure.Data` after the migration.
- Bootstrap Icons are loaded via **CDN** in each Angular page (local Mazer vendor copy is missing the CSS file).
- `MigrateController` is **dev-only** — remove before production deployment.
- Angular uses `withFetch()` (native Fetch API). This can deliver HTTP responses outside Angular's NgZone. Any component that makes HTTP calls must call `ChangeDetectorRef.detectChanges()` after state updates inside `.subscribe()` callbacks.
