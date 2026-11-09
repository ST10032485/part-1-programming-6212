# RaceDay

![CI](https://github.com/ST10032485/part-1-programming-6212/actions/workflows/validate_doc.yml/badge.svg)

RaceDay is a .NET 9 web application for managing race events and enrolments. It has an ASP.NET Core API (`RaceDay.Api`), an MVC web front end (`RaceDay.Web`) and a test project (`RaceDay.Tests`).

## Project structure

| Folder | Purpose |
|---|---|
| `RaceDay.Api` | REST API (events, enrolments, authentication) |
| `RaceDay.Web` | MVC web front end |
| `RaceDay.Tests` | Automated tests |
| `docs` | Project documentation |

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the container)
- Visual Studio 2022 or later (optional)

## Run locally

1. Start the API:
   ```
   cd RaceDay.Api
   dotnet run --urls http://0.0.0.0:5114
   ```
   Swagger is available at http://localhost:5114/swagger
2. In a second terminal, start the web app:
   ```
   cd RaceDay.Web
   dotnet run
   ```

## Run with Docker

The web app runs in a container. Start the API first (see above), then from the repository root run:

```
docker build -t raceday-web ./RaceDay.Web
docker run -p 8080:8080 -e ApiBaseUrl=http://host.docker.internal:5114/ raceday-web
```

Open http://localhost:8080/Account/Login

## Run the tests

```
dotnet test RaceDay.sln
```

## Continuous integration

GitHub Actions (`.github/workflows/validate_doc.yml`) runs on every push and pull request to `main`. It restores, builds the whole solution, runs the tests and builds the Docker image.

## Cloud storage

Azure Blob Storage setup: *(to be added)*
