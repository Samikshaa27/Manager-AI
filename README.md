# Manager AI (Plan AI)

Manager AI is an advanced AI-powered multi-agent project planning and management application. It utilizes a robust C# ASP.NET Core backend coupled with a modern React frontend (built with Vite and Tailwind CSS).

## Features

- **AI Multi-Agent Pipeline:** Features specialized AI agents to handle different aspects of project planning:
  - `CategoryDetectorAgent`: Detects and categorizes project needs.
  - `TaskPlannerAgent`: Automatically breaks down projects into actionable tasks.
  - `RiskAgent`: Identifies potential project risks and mitigation strategies.
  - `OptimizerAgent`: Optimizes task flows and resource allocation.
  - `ResourceAgent`: Manages and suggests resource requirements.
  - `TeamAssignmentAgent`: Suggests optimal team assignments.
- **LLM Integration:** Powered by an OpenAI-compatible API (Groq) for rapid and intelligent text generation and processing.
- **Robust Backend:** Built on .NET 8, featuring Entity Framework Core with a Neon PostgreSQL database.
- **Secure Authentication:** Implements JWT-based authentication for secure user access and data protection.
- **Modern Frontend:** A responsive and fast React frontend utilizing Vite and Tailwind CSS.

## 📸 Product Screenshots

Here is a visual overview of **Manager AI**'s premium, dynamic user interface:

| 🚀 Landing & Portal | 📊 AI Executive Dashboard |
|---|---|
| ![Landing Page](./screenshots/screenshot1.png) | ![Dashboard](./screenshots/screenshot2.png) |

| 📋 Interactive Kanban Board | 📉 High-Impact Reports |
|---|---|
| ![Kanban](./screenshots/screenshot3.png) | ![Reports](./screenshots/screenshot4.png) |

| 👥 Dynamic Team Allocator |
|---|
| ![Team Allocation](./screenshots/screenshot5.png) |

## Architecture

### System Overview

Manager AI follows a clean separation of concerns with three main layers:

**Frontend → Backend API → Database**

#### Backend (ASP.NET Core)
- **Controllers** (`/Controllers`) – HTTP request handlers for authentication, projects, and tasks
- **Services** (`/Services`) – Business logic orchestration and LLM integration
- **Agents** (`/Agents`) – AI pipeline components that process project context sequentially
- **Models** (`/Models`) – Data transfer objects and entity models
- **Data** (`/Data`) – Entity Framework Core DbContext and relationships

#### Frontend (React + Vite)
- **Pages** – Auth, Dashboard, Project Management views
- **Components** – Reusable UI elements (cards, forms, charts)
- **Services** – API client for backend communication
- **Assets** – Images and styling

#### Database (PostgreSQL)
- **ProjectPlans** – Root entity containing project metadata
- **Phases** – Logical project phases (Planning, Execution, Testing, etc.)
- **ProjectTasks** – Individual tasks within phases
- **Risks** – Identified project risks with mitigation strategies
- **AppUser** – Users with JWT-based authentication
- **TeamMembers** – Team assignment tracking

### Multi-Agent Pipeline

The system uses a **sequential agent pipeline** where each agent modifies a shared `ProjectContext`:

```
User Input (Project Description, Budget, Team)
        ↓
ProjectOrchestrator
        ↓
CategoryDetectorAgent    → Identifies project type
        ↓
TaskPlannerAgent         → Breaks into phases & tasks
        ↓
RiskAgent                → Identifies risks
        ↓
OptimizerAgent           → Optimizes scheduling
        ↓
ResourceAgent            → Estimates resources
        ↓
TeamAssignmentAgent      → Suggests assignments
        ↓
ProjectPlan (saved to DB)
```

### Authentication & Authorization

- **JWT Bearer Tokens** – Issued on login, validated on each request
- **Role-Based Access Control (RBAC)** – Admin and Member roles
- **Protected Endpoints** – All project operations require authentication
- **Token Expiry** – Configurable expiration (default 8 hours)

### API Response Format

All endpoints return a consistent response structure:

```json
{
  "success": true,
  "data": { /* entity or null */ },
  "message": "human readable string"
}
```

## Tech Stack

### Backend
- **Framework:** ASP.NET Core (.NET 8)
- **Database:** PostgreSQL (hosted on Neon)
- **ORM:** Entity Framework Core (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **AI Integration:** OpenAI API client (configured for Groq)
- **Authentication:** JWT Bearer tokens (`Microsoft.AspNetCore.Authentication.JwtBearer`)
- **API Documentation:** Swagger / OpenAPI

### Frontend
- **Framework:** React 19
- **Build Tool:** Vite
- **Styling:** Tailwind CSS
- **HTTP Client:** Axios
- **Routing:** React Router v7
- **Animations:** Framer Motion
- **Icons:** Lucide React

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (for frontend development)
- PostgreSQL database (or use the provided Neon connection string)
- Groq / OpenAI API Key

## Getting Started

### 1. Clone the repository

```bash
git clone https://github.com/Samikshaa27/Manager-AI.git
cd Manager-AI
```

### 2. Setup the Backend

1. Restore .NET dependencies:
   ```bash
   dotnet restore
   ```

2. Configure environment variables. Create `appsettings.Development.json` in the project root (this file is in `.gitignore` and will not be committed):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your-neon-postgresql-connection-string"
     },
     "OpenAI": {
       "ApiKey": "your-groq-api-key"
     },
     "Auth": {
       "JwtSecret": "your-secure-jwt-secret-min-32-characters",
       "JwtExpiryHours": 8
     }
   }
   ```

3. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

4. Run the backend:
   ```bash
   dotnet run
   ```
   
   The API will be available at `http://localhost:5000`. Access the Swagger UI at `/swagger` when running in development mode.

### 3. Setup the Frontend

1. Navigate to the frontend directory:
   ```bash
   cd frontend
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Create `.env` file in the frontend directory:
   ```
   VITE_API_URL=http://localhost:5000
   ```

4. Run the frontend development server:
   ```bash
   npm run dev
   ```
   
   The frontend will be available at `http://localhost:5173`.

## 🔒 Security & Environment Variables

To keep your credentials secure, **never** commit live passwords, database connection strings, or API keys to a public repository. The application supports reading configurations from environment variables and files in the following order of precedence:

1. **Environment Variables** (highest priority) – Use `__` for nesting (e.g., `ConnectionStrings__DefaultConnection`)
2. **appsettings.Development.json** (development only)
3. **appsettings.json** (contains non-sensitive defaults)

### Local Development Setup

1. Create `appsettings.Development.json` in the project root (already in `.gitignore`):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "your-neon-connection-string"
     },
     "OpenAI": {
       "ApiKey": "your-groq-api-key"
     },
     "Auth": {
       "JwtSecret": "your-jwt-secret-min-32-chars"
     }
   }
   ```

### Production Deployment (Render / Vercel)

Set the following environment variables in your deployment platform settings (e.g., Render Web Service dashboard, Vercel project settings):

| Variable Name | Description | Format |
| :--- | :--- | :--- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=ep-xxx.neon.tech;Database=neondb;Username=user;Password=pass;SSL Mode=Require;` |
| `OpenAI__ApiKey` | Groq/OpenAI API Key | `gsk_xxxxxxxxxxxx` |
| `Auth__JwtSecret` | JWT secret key (min 32 characters) | `your-secure-32-character-secret` |
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` |

---

## Deployment

The project is configured for deployment with a `render.yaml` file, supporting platforms like Render, Railway, or similar PaaS providers. It also includes a `Dockerfile` for containerized deployment.

### Quick Deploy to Render

1. Push your repository to GitHub
2. Connect your GitHub repo to [Render](https://render.com)
3. Create a new Web Service from your GitHub repo
4. Set environment variables in Render dashboard
5. Deploy!

### Docker Deployment

```bash
docker build -t manager-ai .
docker run -p 5000:5000 \
  -e ConnectionStrings__DefaultConnection="your-db-string" \
  -e OpenAI__ApiKey="your-api-key" \
  -e Auth__JwtSecret="your-jwt-secret" \
  manager-ai
```

## License

This project is open-source and available under the MIT License.
