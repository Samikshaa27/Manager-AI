# Manager AI - System Architecture

## Overview

Manager AI is a full-stack web application for AI-powered project planning and management. It follows a three-tier architecture with a clear separation of concerns:

```
┌─────────────────┐
│   React SPA     │ (Frontend - Vite + Tailwind)
└────────┬────────┘
         │ HTTP/REST
┌────────▼─────────────────────────┐
│  ASP.NET Core Web API (.NET 8)   │ (Backend)
│  ├─ Controllers                  │
│  ├─ Services & Orchestration     │
│  └─ Multi-Agent Pipeline         │
└────────┬────────────────────────┘
         │ SQL
┌────────▼─────────────────────────┐
│  PostgreSQL Database             │ (Neon)
│  ├─ ProjectPlans                 │
│  ├─ Phases & Tasks               │
│  ├─ Risks & Resources            │
│  └─ Users & Authentication       │
└─────────────────────────────────┘
```

---

## Backend Architecture

### Layers

#### 1. **Controllers** (`Controllers/`)
HTTP request handlers that:
- Validate incoming requests
- Call business logic services
- Return standardized API responses
- Handle authentication/authorization

**Key Controllers:**
- `AuthController` – User registration, login, JWT token generation
- `ProjectsController` – CRUD operations for project plans
- `TasksController` – Task management and status updates
- `DashboardController` – Analytics and progress tracking

#### 2. **Services** (`Services/`)
Business logic orchestration layer:

- **ProjectOrchestrator** – Coordinates the multi-agent pipeline
  - Initializes `ProjectContext` with user input
  - Runs agents sequentially
  - Returns finalized `ProjectPlan`
  
- **LlmService** – Wrapper for Groq/OpenAI API
  - Generates tasks via LLM
  - Estimates project costs
  - Handles JSON parsing and error recovery

#### 3. **Agents** (`Agents/`)
The AI pipeline components (each implements `IAgent`):

```csharp
public interface IAgent
{
    string AgentName { get; }
    Task ExecuteAsync(ProjectContext context);
}
```

**Sequential Pipeline:**

1. **CategoryDetectorAgent**
   - Analyzes project description
   - Returns category (Software, Solar, Healthcare, etc.)
   - Uses keyword matching for fast classification
   
2. **TaskPlannerAgent**
   - Calls LLM to generate detailed tasks
   - Organizes tasks into logical phases
   - Assigns priority and duration to each task
   
3. **RiskAgent**
   - Identifies potential project risks
   - Suggests mitigation strategies
   - Rates risk severity
   
4. **OptimizerAgent**
   - Analyzes task dependencies
   - Optimizes task scheduling
   - Identifies critical path
   
5. **ResourceAgent**
   - Estimates human resources needed
   - Calculates equipment and tools required
   - Provides resource summary
   
6. **TeamAssignmentAgent**
   - Suggests team member assignments
   - Matches skills to tasks
   - Provides assignment recommendations

**Data Flow Through Pipeline:**
```
ProjectContext (Input)
    ↓ Agent 1 (adds category)
    ↓ Agent 2 (adds tasks & phases)
    ↓ Agent 3 (adds risks)
    ↓ Agent 4 (adds dependencies)
    ↓ Agent 5 (adds resources)
    ↓ Agent 6 (adds assignments)
ProjectContext (Output with complete ProjectPlan)
```

#### 4. **Models** (`Models/`)
Domain entities and DTOs:

- **ProjectPlan** – Root aggregate
  - `Id`, `ProjectName`, `Description`, `Category`
  - `Phases`, `Risks`, `CriticalPathTaskIds`
  - `EstimatedCostMin/Max`, `TotalDurationDays`
  - `AgentLog` (trace of agent execution)

- **Phase** – Logical project phases
  - `Name`, `Description`, `ProgressPercent`
  - `Tasks` (collection)

- **ProjectTask** – Individual work items
  - `Name`, `Description`, `DurationDays`
  - `Priority`, `Status`, `Phase`
  - `AssignedTo`, `IsOnCriticalPath`

- **Risk** – Identified risks
  - `Title`, `Description`, `Severity`
  - `MitigationStrategy`

- **AppUser** – Authentication
  - `Email`, `PasswordHash`, `Role` (Admin/Member)
  - `Projects` (collection)

- **ProjectContext** – Shared pipeline context
  - Initialized with user input
  - Passed through each agent
  - Accumulates results via `AgentLog`

#### 5. **Data** (`Data/`)
Entity Framework Core DbContext:

- **AppDbContext** – Main DbContext
  - Configures all entity relationships
  - Handles JSON serialization for `AgentLog`
  - Manages owned types (e.g., `ResourceSummary`)
  - Cascade deletes for data integrity

**Key Relationships:**
```
ProjectPlan (1) ─→ (Many) Phase ─→ (Many) ProjectTask
ProjectPlan (1) ─→ (Many) Risk
ProjectPlan (Many) ← (1) AppUser
```

#### 6. **Helpers** (`Helpers/`)
Utility functions:

- **ApiResponse<T>** – Standard response wrapper
  ```csharp
  {
    "success": true/false,
    "data": { ... },
    "message": "Human-readable message"
  }
  ```

---

## Frontend Architecture

### Structure

```
frontend/
├── src/
│   ├── pages/           # Full-page components
│   │   ├── Auth         # Login/Register
│   │   ├── Dashboard    # Project overview
│   │   └── Projects     # Project detail/management
│   ├── components/      # Reusable UI components
│   │   ├── Header
│   │   ├── Sidebar
│   │   └── Cards
│   ├── services/        # API clients
│   │   ├── api.js       # Axios configuration
│   │   └── projectService.js
│   ├── lib/             # Utilities
│   ├── App.jsx          # Root component
│   └── main.jsx         # Entry point
├── package.json
└── vite.config.js
```

### Key Technologies

- **React 19** – Component framework
- **Vite** – Build tool with HMR
- **Tailwind CSS** – Utility-first styling
- **Axios** – HTTP client
- **React Router v7** – Client-side routing
- **Framer Motion** – Animations

### State Management

- React hooks (`useState`, `useContext`)
- Local component state for forms
- Context API for auth state
- No external state manager (simple approach)

### API Integration

Axios client configured with:
- Base URL from `VITE_API_URL` environment variable
- JWT token in `Authorization` header
- Error handling and token refresh logic
- Timeout configuration

---

## Authentication & Authorization

### Flow

```
User Registration/Login
    ↓
AuthController validates credentials
    ↓
JWT token generated (8 hours default)
    ↓
Token returned to frontend
    ↓
Stored in memory/localStorage
    ↓
Included in all API requests
    ↓
Middleware validates token
    ↓
Access granted/denied based on role
```

### JWT Structure

- **Header** – Algorithm (HS256)
- **Payload**
  - `sub` (user ID)
  - `name` (user name)
  - `email`
  - `role` (Admin/Member)
  - `jti` (token ID)
  - `exp` (expiration time)
- **Signature** – HMAC-SHA256 with secret

### Role-Based Access Control

- **Admin** – Can generate plans, assign tasks
- **Member** – Can view plans, update task status
- **Public** – Can access `/api/auth/register` and login

Enforced via `[Authorize(Roles = "Admin")]` attributes on controller actions.

---

## Database Schema

### Entities & Relationships

```sql
-- Users
AppUser
  id (UUID, PK)
  email (unique)
  passwordHash
  role (Admin/Member)
  createdAt

-- Projects
ProjectPlan
  id (UUID, PK)
  userId (FK → AppUser)
  projectName
  category
  description
  totalDurationDays
  estimatedCostMin/Max
  createdAt
  agentLog (JSON array)

-- Phases
Phase
  id (UUID, PK)
  projectPlanId (FK → ProjectPlan, cascade delete)
  name
  description
  progressPercent

-- Tasks
ProjectTask
  id (UUID, PK)
  phaseId (FK → Phase, cascade delete)
  name
  description
  durationDays
  priority
  status
  assignedUserId (FK → AppUser, nullable)
  assignedTo
  isOnCriticalPath

-- Risks
Risk
  id (UUID, PK)
  projectPlanId (FK → ProjectPlan, cascade delete)
  title
  description
  severity
  mitigationStrategy

-- Resources (Owned by ProjectPlan)
ResourceSummary
  skills (JSON array)
  equipment (JSON array)
  tools (JSON array)
```

### Cascade Behavior

- Deleting a **ProjectPlan** cascades to:
  - All **Phases** and their **Tasks**
  - All **Risks**
  - **ResourceSummary**
- User data is preserved (soft foreign key)

---

## Configuration Management

### Precedence Order

1. **Environment Variables** (highest priority)
   - Format: `Category__Key` (e.g., `ConnectionStrings__DefaultConnection`)
   - Useful for secrets in production

2. **appsettings.Development.json** (development only)
   - Local secrets not committed to Git
   - Overrides base settings

3. **appsettings.json** (lowest priority)
   - Base configuration with non-sensitive defaults
   - Committed to repository

### Key Configuration Sections

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=...;Database=...;..."
  },
  "OpenAI": {
    "ApiKey": "gsk_..."
  },
  "Auth": {
    "JwtSecret": "secure-32-character-string",
    "JwtExpiryHours": 8
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:5173", "https://..."]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "PlanAI": "Debug"
    }
  }
}
```

---

## Deployment

### Docker Build

Multi-stage Dockerfile:
1. **Stage 1** – Build React frontend (Node 20)
2. **Stage 2** – Build .NET backend (SDK 8.0)
3. **Stage 3** – Final image (ASP.NET Core 8.0 runtime)
   - Copies built frontend to `wwwroot`
   - Runs single .NET application serving both

### Render Deployment

Configuration via `render.yaml`:
- Specifies Dockerfile and context
- Sets environment variables
- Configures build and start commands
- Enables automatic deployments from GitHub

### Environment Setup

**Required Environment Variables:**
- `ConnectionStrings__DefaultConnection` – PostgreSQL URI
- `OpenAI__ApiKey` – Groq API key
- `Auth__JwtSecret` – 32+ character secret
- `ASPNETCORE_ENVIRONMENT` – "Production"

---

## Error Handling

### API Responses

**Success:**
```json
{
  "success": true,
  "data": { /* entity */ },
  "message": "Operation successful"
}
```

**Failure:**
```json
{
  "success": false,
  "data": null,
  "message": "Detailed error message"
}
```

### HTTP Status Codes

- `200 OK` – Success
- `201 Created` – Resource created
- `400 Bad Request` – Validation errors
- `401 Unauthorized` – Missing/invalid token
- `403 Forbidden` – Insufficient permissions
- `404 Not Found` – Resource not found
- `500 Internal Server Error` – Unhandled exceptions

### Logging

- Logs to console and configured sinks
- Includes user ID and request context
- LLM calls logged for debugging
- Database errors logged with full stack trace

---

## Development Workflow

### Adding a New Feature

1. **Backend**
   - Create Model if needed
   - Create Controller action
   - Call Service/Agent logic
   - Handle errors gracefully
   - Return ApiResponse

2. **Frontend**
   - Create Page or Component
   - Call API via service
   - Handle loading/error states
   - Update UI with response

3. **Database**
   - Modify Entity if needed
   - Create migration: `dotnet ef migrations add FeatureName`
   - Review generated migration
   - Apply: `dotnet ef database update`

4. **Testing**
   - Add unit tests for agents
   - Test API endpoints manually (Swagger)
   - Test frontend flows
   - Commit with clear message

---

## Performance Considerations

### Database
- Connection pooling (Npgsql retry logic)
- Eager loading with `.Include()` in queries
- Indexed fields (ID, UserId, Status)

### LLM Calls
- Cached categorization results
- Timeout limits on API calls
- Fallback parsing strategies for JSON

### Frontend
- Code splitting via Vite
- Lazy loading for heavy components
- Optimized CSS with Tailwind purging

### Caching
- HTTP client reuse via factory pattern
- EF Core query caching (Level 2)
- Minimal frontend re-renders with React keys

---

## Security

### Authentication
- Passwords hashed with BCrypt.Net
- JWT tokens with 8-hour expiry
- Token validation on every protected endpoint

### Data Protection
- Environment variables for secrets
- CORS policy limits frontend origins
- SQL injection prevented via EF Core parameterization
- XSS protection via React auto-escaping

### Deployment
- HTTPS enforced in production
- Secrets never committed to Git
- `.gitignore` protects sensitive files
- Deployment secrets managed by platform (Render, Vercel)

---

## Monitoring & Diagnostics

### Logging
- ILogger injected in services
- Structured logging with semantic keys
- Log levels configurable per namespace

### Swagger UI
- Available at `/swagger` in development
- Documents all API endpoints
- Schema includes security requirements
- Request/response examples

### Agent Traces
- Each agent adds log entry to `AgentLog`
- Persisted with ProjectPlan for audit trail
- Frontend can display execution trace

---

## Future Enhancements

- Real-time collaboration (WebSockets)
- Advanced filtering and search
- Project templates
- Integration with Jira/Azure DevOps
- Mobile app
- Advanced analytics and BI
