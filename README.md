# GradLink

GradLink is a modern, full-stack web application designed to connect graduates with prospective employers. It features a sleek, Apple-inspired UI, real-time notifications, and a robust ASP.NET Core backend.

## Features
- **Graduate Portal**: Apply for jobs, track application status on a Kanban board, and update profile details.
- **Employer Portal**: Post job listings, review applicants, and change application statuses (e.g., Shortlisted, Rejected).
- **Real-Time Notifications**: Instant updates powered by SignalR whenever an application status changes or a profile is viewed.
- **Premium UI**: Built with vanilla CSS tokens, featuring a clean, responsive, glassmorphic design and subtle micro-animations.

## Technology Stack
- **Frontend**: Blazor WebAssembly (C#)
- **Backend**: ASP.NET Core Web API (C#)
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with JWT Bearer tokens
- **Real-time**: SignalR

## Getting Started

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer (opening `GradLink.slnx` needs SDK 9.0.200+ / Visual Studio 17.13+; with the .NET 8 SDK, build the projects directly)

### Setup
1. Clone the repository.
2. Open a terminal in the root directory.

### Running the API
1. Navigate to the API project:
   ```bash
   cd src/GradLink.API
   ```
2. Run the backend:
   ```bash
   dotnet run
   ```
   *Note: On first run, the SQLite database (`gradlink.db`) will be automatically created and seeded with sample data.*

### Running the Client
1. Open a new terminal and navigate to the Client project:
   ```bash
   cd src/GradLink.Client
   ```
2. Run the frontend:
   ```bash
   dotnet run
   ```
3. Open your browser and navigate to the URL provided in the terminal (e.g., `http://localhost:5134`).

## Sample Accounts (Seeded Data)
- **Graduate**: `alice@gradlink.com` / `Password@123`
- **Employer**: `techcorp@gradlink.com` / `Password@123`

The seed also creates sample applications and the notifications they would have produced. Seeding runs only against an empty database; delete `src/GradLink.API/gradlink.db` to reseed.

## Running the Tests
```bash
dotnet test
```
The integration tests in `tests/GradLink.API.Tests` start the API in-process against a temporary SQLite database, so they don't need the API running and never touch `gradlink.db`.
