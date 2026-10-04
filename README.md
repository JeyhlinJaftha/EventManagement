# Event Management API — POE Part 2

## Project Overview
A RESTful ASP.NET Core Web API for managing run, walk and cycle events.

## Tech
ASP.NET Core Web API (.NET 8), EF Core Code-First, SQL Server, BCrypt, session-based auth, Swagger, xUnit, GitHub Actions.

## Setup
1. Edit `ConnectionStrings:DefaultConnection` in `EventManagement.Api/appsettings.json` if your SQL Server instance is not `localhost\SQLEXPRESS`.
2. In Package Manager Console (Default project: `EventManagement.Api`):
   ```
   Add-Migration InitialCreate
   Update-Database
   ```
3. Run the API and open Swagger at `/swagger`.

## Roles
**Organiser:** create/update/delete own events, create categories, view enrolments for own events, capture results.
**Participant:** view events and categories, enrol, view own results.
**Both:** view and update own profile.

## Authentication
Session-based. After login, `UserId` and `Role` are stored in the session. Passwords are hashed with BCrypt.
Unauthenticated requests return **401**; wrong role or non-owner returns **403**.

## Endpoints
| Method | Route | Access |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Public |
| GET/PUT | /api/profile | Logged in |
| GET | /api/events, /api/events/{id} | Logged in |
| POST/PUT/DELETE | /api/events | Organiser (owner) |
| GET | /api/events/{eventId}/categories | Logged in |
| POST | /api/events/{eventId}/categories | Organiser (owner) |
| POST | /api/enrolments | Participant |
| GET | /api/enrolments/event/{eventId} | Organiser (owner) |
| POST | /api/results | Organiser (owner) |
| GET | /api/results/mine | Participant |

## Running tests
```
dotnet test
```

## CI/CD Build Status
Insert screenshot of successful GitHub Actions build here.
