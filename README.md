# IT Asset Allocation (ITS) Module
### Full-Stack Build Specification for Employee Management System (EMS)

An enterprise-grade, clean-architecture solution for managing IT hardware allocation lifecycles across an organization with role-based dashboards, SignalR real-time updates, strict status state machines, and validation strategy pipelines.

---

## 🏗️ Architecture & Technology Stack

### Backend (.NET 8 Web API / C# 12)
- **Architecture**: Layered / Clean Architecture (`Its.Domain` → `Its.Application` → `Its.Infrastructure` → `Its.Api`)
- **Database & ORM**: PostgreSQL 15+ via Entity Framework Core 8 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Design Patterns**:
  - **Repository & Unit of Work**: Encapsulates DB operations and guarantees single-transaction consistency for complex flows (Assign, Return, Transfer, Replace).
  - **State Pattern**: `RequestStateMachine` and `AssetStateMachine` validate state transitions and throw `ConflictException` (HTTP 409) for illegal operations.
  - **Strategy Pattern**: `IAssetValidationRule` pipeline (`AvailabilityRule`, `ConditionRule`, `WarrantyRule`, `EmployeeActiveRule`) executes 4-point eligibility checks on hardware allocation.
  - **Observer / Pub-Sub**: SignalR WebSockets broadcasting instant in-app alerts to user-specific groups (`user-{userId}`) and role channels.
  - **Result Pattern**: Clean `Result<T>` with standard ProblemDetails mapping.
- **Authentication**: JWT Bearer with Refresh Token rotation, BCrypt password hashing, and policy-based permission claims (`[HasPermission("Asset.Assign")]`).
- **Logging & Auditing**: Serilog console & daily rolling file sink, structured DB audit log entries (`AuditLogs` table).
- **Unit Testing**: xUnit + FluentAssertions covering validation strategies and state transitions.

### Frontend (React 18 + Vite + TypeScript)
- **Styling**: Tailwind CSS adhering strictly to the **Section 15 White UI Design System**:
  - Page Background: `#F8FAFC`, Surfaces & Cards: `#FFFFFF`, Borders: `#E2E8F0`
  - Primary Accent: `#2563EB`, Active Sidebar Item: `#EFF6FF` with blue accent bar
  - Soft status badges: `#DCFCE7` (Green), `#DBEAFE` (Blue), `#EDE9FE` (Purple), `#FEF3C7` (Amber), `#FEE2E2` (Red)
- **State & Data Fetching**: TanStack React Query + Axios with JWT interceptors and automatic 401 refresh token retry queue.
- **Realtime**: `@microsoft/signalr` integration with live floating toast alerts and topbar unread notification badges.
- **Charts**: Recharts (Donut chart for Assets by Category, Bar chart for Asset Status Distribution).
- **Icons**: Lucide React.

---

## 👥 Seed Accounts & Demo Credentials

All seed accounts are pre-populated on startup:

| Role | Username | Password | Full Name | Employee Code | Designation |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ITS Manager** | `manager` | `Manager@123` | Sairaj Kadlag | `EMP001` | ITS Operations Manager |
| **ITS Executive** | `exec1` | `Exec@123` | Priya Sharma | `EMP002` | ITS Executive |
| **ITS Executive** | `exec2` | `Exec@123` | Amit Verma | `EMP003` | ITS Senior Executive |
| **ITS Executive** | `exec3` | `Exec@123` | Neha Gupta | `EMP004` | ITS Executive |
| **Employee** | `emp005` | `Emp@123` | Rahul Sharma | `EMP005` | Senior Software Engineer |
| **Employee** | `emp006` | `Emp@123` | Ananya Patel | `EMP006` | Frontend Engineer |
| **Employee** | `emp007` | `Emp@123` | Rohan Mehta | `EMP007` | HR Specialist |

*(Quick switch buttons are built right into the Login screen and topbar profile menu for instant testing.)*

---

## 📁 Solution Structure

```
ITS/
├── ItsModule.sln
├── Its.Domain/                  # Pure Domain Entities, Enums, State Machines, Domain Exceptions
│   ├── Common/                  # BaseEntity
│   ├── Entities/                # Department, Employee, User, Role, Permission, ITAsset, AssetRequest, etc.
│   ├── Enums/                   # AssetStatus, AssetCondition, RequestStatus, AllocationStatus, etc.
│   ├── Exceptions/              # DomainException, NotFoundException, ConflictException, etc.
│   └── State/                   # RequestStateMachine, AssetStateMachine
├── Its.Application/             # Business Services, DTOs, Validation Strategies, Interfaces
│   ├── Common/                  # Result<T>, PagedResult<T>, QueryParameters
│   ├── DTOs/                    # Auth, Dashboard, Asset, Request, Repair, Replacement, Report DTOs
│   ├── Interfaces/              # IUnitOfWork, IRepository, IAssetService, IRequestService, etc.
│   ├── Services/                # AuthService, DashboardService, AssetService, AllocationService, etc.
│   ├── Strategies/              # AvailabilityRule, ConditionRule, WarrantyRule, ValidationPipeline
│   └── Validators/              # FluentValidation Request DTO Validators
├── Its.Infrastructure/          # Data Access, PostgreSQL EF Core, Security, SignalR Publisher, Seed
│   ├── Data/                    # ApplicationDbContext, DbInitializer
│   ├── Notifications/           # SignalR NotificationPublisher & Hub Context
│   ├── Repositories/            # Generic Repository<T> & UnitOfWork
│   ├── Security/                # JwtTokenService, BCrypt PasswordHasher, AuditService
│   └── Seed/                    # DatabaseSeeder (Roles, 27 Permissions, 25 Assets, Sample Requests)
├── Its.Api/                     # Web API Host, Controllers, SignalR Hubs, Middleware, Swagger
│   ├── Controllers/             # Auth, Dashboard, Employees, Assets, Requests, Repairs, Reports, etc.
│   ├── Hubs/                    # NotificationHub (/hubs/notifications)
│   ├── Middleware/              # ExceptionHandlingMiddleware, CorrelationIdMiddleware
│   ├── Services/                # CurrentUserService, PermissionAuthorizationHandler
│   ├── Program.cs               # DI container setup, Serilog, CORS, Swagger JWT
│   └── appsettings.json         # Connection string & JWT configuration
├── Its.Tests/                   # xUnit Unit Tests (Validation Pipeline & State Transitions)
├── frontend/                    # React 18 + Vite + TypeScript Frontend Application
│   ├── src/
│   │   ├── api/                 # Axios client with interceptors & services
│   │   ├── components/          # AppLayout, Sidebar, Topbar, Badge, Card, Button, Modal, FormField
│   │   ├── context/             # AuthContext, SignalRContext
│   │   ├── pages/               # Dashboards, Requests, Tasks, Assets, Employees, Reports, Notifications
│   │   ├── types/               # TypeScript interfaces & enums
│   │   ├── App.tsx              # Router definitions & ProtectedRoute role guards
│   │   └── index.css            # White UI theme CSS variables
│   ├── tailwind.config.js       # Design tokens from Section 15
│   └── package.json
├── ITS_Module.postman_collection.json # Ready-to-import Postman Collection
├── ITS.http                     # VS Code / Visual Studio REST Client test file
└── README.md
```

---

## 🚀 How to Run

### 1. Prerequisites
- **.NET 8 SDK** (or .NET 9 SDK)
- **Node.js** (v18+) & **npm**
- **PostgreSQL 15+** running locally on port 5432 (or Docker container)

*(If using Docker for PostgreSQL)*:
```bash
docker run --name postgres-its -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=its_db -p 5432:5432 -d postgres:15
```

### 2. Configure Database Connection
Check `Its.Api/appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=its_db;Username=postgres;Password=postgres"
}
```

### 3. Run Backend API
```bash
cd C:\Users\sairajk\.gemini\antigravity\scratch\ITS
dotnet run --project Its.Api/Its.Api.csproj --urls "http://localhost:5000"
```
- **Swagger Documentation**: Open [http://localhost:5000/swagger](http://localhost:5000/swagger)
- **SignalR Hub**: Accessible at `http://localhost:5000/hubs/notifications`

### 4. Run Frontend App
In a new terminal:
```bash
cd C:\Users\sairajk\.gemini\antigravity\scratch\ITS\frontend
npm run dev
```
- Open [http://localhost:5173](http://localhost:5173) in your browser.

---

## 🧪 Running Unit Tests
```bash
dotnet test C:\Users\sairajk\.gemini\antigravity\scratch\ITS\ItsModule.sln
```
*All 21 xUnit tests verifying Strategy validation rules (Availability, Working Condition, Warranty expiry threshold) and State Machine transitions pass with 100% success.*

---

## 📋 End-to-End Business Flow Walkthrough

1. **Submit Request (Employee)**:
   - Login as `emp005` (`Emp@123`).
   - Click **[Request Asset]** → Select category `Laptop` → Specify reason → Submit.
   - Status becomes `Submitted` and an instant notification reaches the Manager.

2. **Validate & Assign Task (Manager)**:
   - Login as `manager` (`Manager@123`).
   - On the Manager Dashboard, see the incoming request in the Pending queue.
   - Click **[Assign Task]** → Select ITS Executive `Priya Sharma (exec1)` → Submit.
   - Status transitions to `AssignedToExecutive`. SignalR pushes alert to Executive.

3. **Check Asset & Allocate (Executive)**:
   - Login as `exec1` (`Exec@123`).
   - On Executive Dashboard, open task queue → Click **[Check Asset]**.
   - The system executes the **Strategy Pipeline**:
     - *Availability*: `[ok]`
     - *Working Condition*: `[ok]`
     - *Warranty*: `[ok]` (Flags devices near expiry or expired)
   - Click **[Assign]** on matching available asset (e.g. `LAP-0002`).
   - Single atomic transaction:
     - Creates active `AssetAllocation`
     - Updates asset status to `Assigned` with employee custodian
     - Transitions request status to `Allocated`
     - Records timeline history & audit log entry
     - SignalR triggers real-time alert to employee: *"New HP ProBook 450 (LAP-0002) has been assigned to you."*

4. **Return & Transfer Flows**:
   - Return closes active allocation (`Returned`) and restocks asset to `Available` (or `Damaged` / `UnderMaintenance`).
   - Transfer creates new active allocation for target employee in the same atomic transaction while preserving complete audit history.
