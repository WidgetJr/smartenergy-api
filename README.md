# SmartEnergy API

SmartEnergy API is a RESTful backend application built with ASP.NET Core and C# for managing household energy consumption.

The system provides functionality for user authentication, homes, spaces, electrical devices, energy readings, electricity tariffs, and consumption analysis.

The project was designed as the backend for the SmartEnergy platform, with a modular feature-based architecture and PostgreSQL persistence.

---

## Overview

SmartEnergy provides the backend infrastructure required to organize and monitor energy consumption inside a household.

The API allows users to structure their environment using homes, spaces, and devices while storing energy readings and electricity tariff information.

The main goal of the project is to provide a scalable backend capable of supporting energy monitoring applications while maintaining a clean separation between domain logic, infrastructure, and individual business features.

---

## Main Features

- User authentication and authorization
- JWT-based authentication
- Home management
- Space and room organization
- Device management
- Energy reading registration
- Energy reading history
- Energy consumption tracking
- Manual energy consumption registration
- Electricity tariff management
- Energy cost calculation support
- PostgreSQL database persistence
- RESTful API design
- Feature-based architecture
- Entity Framework Core integration
- Asynchronous database operations
- Dependency Injection
- Swagger / OpenAPI documentation
- Cloud database support

---

## Tech Stack

### Backend

- C#
- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- REST APIs
- JWT Authentication
- Dependency Injection
- Async / Await

### Database

- PostgreSQL
- Neon PostgreSQL
- Entity Framework Core Migrations
- Npgsql

### Development and Infrastructure

- Git
- GitHub
- Swagger / OpenAPI
- Render
- Neon

---

## Architecture

SmartEnergy API uses a feature-based project structure.

Instead of organizing the entire application exclusively around technical layers such as controllers, services, and repositories, related functionality is grouped by business feature.

This approach makes the application easier to navigate, maintain, and extend as new functionality is added.

```text
smartenergy-api/
│
├── Common/
│
├── Domain/
│
├── Features/
│   ├── Auth/
│   ├── Consumption/
│   ├── Devices/
│   ├── EnergyReadings/
│   ├── EnergyTariffs/
│   ├── Homes/
│   └── Spaces/
│
├── Infrastructure/
│
└── Program.cs
```

### Common

Contains shared functionality that can be reused across multiple application features.

This can include common abstractions, utilities, shared models, validation helpers, or application-wide components.

### Domain

Contains the core business entities and domain definitions used by SmartEnergy.

The domain layer represents the main concepts of the application independently from infrastructure concerns.

### Features

Contains the business functionality of the application.

Each feature is grouped into its own module so that related endpoints, services, DTOs, validation, and business logic can remain close together.

### Infrastructure

Contains infrastructure-related implementations such as database access, Entity Framework Core configuration, persistence, and external service integrations.

---

## Feature Modules

### Auth

Handles authentication-related operations.

Responsibilities include:

- User authentication
- Account registration
- Credential validation
- JWT generation
- Protected API access

---

### Homes

Handles household management.

A home represents the main environment where users organize their energy consumption information.

Home-related functionality can include:

- Creating homes
- Retrieving homes
- Updating home information
- Removing homes
- Associating spaces and devices with a home

---

### Spaces

Spaces represent individual areas inside a home.

Examples include:

```text
Living Room
Kitchen
Bedroom
Office
Garage
```

Using spaces allows devices and consumption information to be organized according to their physical location.

---

### Devices

Handles electrical device management.

A device represents an appliance, electronic device, or monitored energy consumer registered inside SmartEnergy.

Device information can be associated with a specific home or space.

Examples include:

```text
Air Conditioner
Computer
Television
Refrigerator
Lighting
Washing Machine
```

---

### Energy Readings

Handles energy measurement records.

Energy readings are used to store consumption-related information associated with registered devices.

Depending on the available data, readings may contain information related to:

```text
Power
Voltage
Current
Energy Consumption
Timestamp
```

These records can later be used to analyze historical consumption.

---

### Energy Tariffs

Handles electricity pricing information.

Tariffs allow SmartEnergy to associate energy consumption with electricity costs.

This makes it possible to estimate the monetary impact of energy usage using consumption data and configured electricity prices.

---

### Consumption

Contains the business logic required to calculate and analyze energy consumption.

This feature can combine information from:

- Homes
- Spaces
- Devices
- Energy readings
- Energy tariffs

The goal is to transform stored measurements into useful consumption information for the client application.

---

## Domain Model

At a high level, SmartEnergy follows the following structure:

```text
User
 │
 └── Home
      │
      ├── Space
      │    │
      │    └── Device
      │         │
      │         └── EnergyReading
      │
      └── EnergyTariff
```

The exact relationships and constraints are defined by the application's domain models and database configuration.

---

## Authentication

SmartEnergy API uses JSON Web Tokens (JWT) to protect authenticated resources.

A typical authentication flow is:

```text
Client
  │
  │ Login / Register
  ▼
SmartEnergy API
  │
  │ Validate credentials
  ▼
Generate JWT
  │
  ▼
Client
  │
  │ Authorization: Bearer <token>
  ▼
Protected API endpoints
```

Protected requests must send the generated token using the HTTP `Authorization` header.

Example:

```http
Authorization: Bearer <token>
```

JWT authentication allows the frontend and backend to remain independent while maintaining secure authenticated sessions.

---

## REST API

SmartEnergy exposes its functionality through RESTful HTTP endpoints.

The API is organized around resources such as:

```text
/auth
/homes
/spaces
/devices
/energy-readings
/energy-tariffs
/consumption
```

Depending on the resource, endpoints may support standard operations such as:

```http
GET
POST
PUT
PATCH
DELETE
```

The exact routes and supported operations can be reviewed through the API controllers/endpoints or Swagger documentation.

---

## API Documentation

Swagger / OpenAPI is used during development to inspect and test the available endpoints.

After starting the application locally, Swagger can normally be accessed through:

```text
https://localhost:<port>/swagger
```

or:

```text
http://localhost:<port>/swagger
```

Swagger can be used to:

- Explore API endpoints
- Inspect request models
- Inspect response models
- Test requests
- Test authentication
- Review HTTP status codes
- Debug frontend integration

---

## Database

SmartEnergy uses PostgreSQL as its relational database.

Entity Framework Core is used as the application's Object-Relational Mapper.

The project uses:

```text
ASP.NET Core
      │
      ▼
Entity Framework Core
      │
      ▼
Npgsql
      │
      ▼
PostgreSQL
```

For cloud environments, the database can be hosted using Neon PostgreSQL.

---

## Entity Framework Core

Entity Framework Core manages database communication and application persistence.

The project can use EF Core migrations to keep the database schema synchronized with the domain model.

To create a migration:

```bash
dotnet ef migrations add MigrationName
```

To apply migrations:

```bash
dotnet ef database update
```

To view existing migrations:

```bash
dotnet ef migrations list
```

---

## Getting Started

### Requirements

Before running the project locally, install:

- .NET SDK
- Git
- PostgreSQL or access to a PostgreSQL database

Verify the .NET installation:

```bash
dotnet --version
```

---

## Clone the Repository

Clone the repository:

```bash
git clone https://github.com/WidgetJr/smartenergy-api.git
```

Enter the project directory:

```bash
cd smartenergy-api
```

---

## Restore Dependencies

Restore the project's NuGet packages:

```bash
dotnet restore
```

---

## Database Configuration

The application requires a PostgreSQL connection string.

For local development, the configuration can be stored using development configuration or .NET User Secrets.

Example structure:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smartenergy;Username=postgres;Password=your_password"
  }
}
```

Production credentials should never be committed to the repository.

---

## Environment Variables

Sensitive configuration should be provided through environment variables or another secure secret-management mechanism.

Typical configuration may include:

```text
Database connection string
JWT signing key
JWT issuer
JWT audience
```

Example:

```env
ConnectionStrings__DefaultConnection=your_postgresql_connection_string

Jwt__Key=your_secure_jwt_key
Jwt__Issuer=SmartEnergy
Jwt__Audience=SmartEnergyClient
```

The exact variable names should match the configuration defined by the application.

---

## Run the Application

Start the API with:

```bash
dotnet run
```

ASP.NET Core will display the local application URL in the terminal.

For example:

```text
https://localhost:7000
http://localhost:5000
```

The actual ports depend on the local application configuration.

---

## Development Mode

You can explicitly run the application using the Development environment.

### Windows PowerShell

```powershell
$env:ASPNETCORE_ENVIRONMENT="Development"
dotnet run
```

### Linux / macOS

```bash
export ASPNETCORE_ENVIRONMENT=Development
dotnet run
```

Development mode can provide additional debugging information and Swagger documentation depending on the application configuration.

---

## Example API Request

A typical authenticated request can be made using an HTTP client.

Example:

```http
GET /api/devices HTTP/1.1
Host: localhost
Authorization: Bearer <your-jwt-token>
Content-Type: application/json
```

Example JSON request body for creating a resource:

```json
{
  "name": "Desktop Computer"
}
```

The actual required fields depend on the endpoint and DTO definitions used by the application.

---

## Error Handling

The API uses standard HTTP status codes to communicate request results.

Common responses may include:

| Status Code | Meaning |
|---|---|
| `200 OK` | Request completed successfully |
| `201 Created` | Resource created successfully |
| `204 No Content` | Request completed without response content |
| `400 Bad Request` | Invalid request or validation error |
| `401 Unauthorized` | Authentication is required or invalid |
| `403 Forbidden` | Authenticated user does not have access |
| `404 Not Found` | Requested resource does not exist |
| `500 Internal Server Error` | Unexpected server error |

---

## Security

The project follows common backend security practices including:

- JWT authentication
- Protected endpoints
- Separation of configuration and source code
- Environment-based secrets
- DTO usage
- Server-side request validation
- Database access through Entity Framework Core

Sensitive information such as database passwords and JWT secrets should never be stored directly in the public repository.

Recommended files and values to exclude include:

```text
Production connection strings
JWT signing secrets
API keys
Local development secrets
Environment files containing credentials
```

---

## Cloud Architecture

The backend can be deployed independently from the frontend.

The current architecture is based on cloud-hosted backend and database services.

```text
Client Application
        │
        │ HTTPS / REST
        ▼
SmartEnergy API
ASP.NET Core
        │
        │ Entity Framework Core
        ▼
PostgreSQL
```

The API can be hosted using Render while PostgreSQL can be hosted using Neon.

This separation allows the client, backend, and database to be deployed and scaled independently.

---

## Design Principles

The project was developed with several software engineering principles in mind.

### Separation of Concerns

Business functionality, domain models, and infrastructure responsibilities are separated into dedicated areas.

### Feature-Based Organization

Related functionality is grouped by business capability instead of placing every controller, service, or model into large global folders.

### Dependency Injection

ASP.NET Core Dependency Injection is used to manage application dependencies and reduce coupling.

### Asynchronous Programming

Database and network operations can use asynchronous APIs to avoid unnecessary blocking operations.

### DTO-Based Communication

API contracts can use Data Transfer Objects rather than exposing database entities directly.

### RESTful Design

Resources are exposed using HTTP endpoints and standard HTTP methods.

---

## Project Goals

SmartEnergy was created to demonstrate and practice the integration of several backend development concepts within a real application.

The project focuses on:

- Backend development with C# and ASP.NET Core
- REST API design
- PostgreSQL database development
- Entity Framework Core
- Authentication and authorization
- Modular software architecture
- Cloud database integration
- Cloud deployment
- Energy consumption modeling
- Business logic implementation
- Frontend and backend integration

---

## Current Scope

The current repository focuses on the SmartEnergy backend API.

Implemented areas of the project include the backend modules for:

```text
Authentication
Homes
Spaces
Devices
Energy Readings
Energy Tariffs
Consumption
```

Hardware-based energy monitoring is not currently part of the completed API implementation.

Energy data can therefore be managed through the software functionality implemented by the platform without requiring dedicated SmartEnergy hardware.

---

## Roadmap

Possible future improvements include:

- Real-time energy monitoring
- Historical consumption dashboards
- Daily, weekly, and monthly reports
- More advanced energy cost calculations
- Consumption alerts
- Budget limits
- Device status tracking
- Real-time communication using WebSockets or SignalR
- Advanced analytics
- Energy consumption predictions
- Additional authentication options
- User roles and permissions
- Notifications
- Automated testing
- Integration testing
- Docker support
- CI/CD pipelines
- API versioning
- Rate limiting
- IoT device integration
- ESP32-based energy monitoring
- Automatic device measurements

The ESP32 / IoT integration is considered a future extension and is not currently presented as a completed feature of the project.

---

## Related SmartEnergy Components

SmartEnergy is designed as a multi-component application.

```text
SmartEnergy Client
        │
        │ REST API
        ▼
SmartEnergy API
        │
        ▼
PostgreSQL Database
```

The API is responsible for application logic, authentication, data persistence, energy information, and communication between the database and client applications.

---

## Repository Purpose

This repository contains the backend implementation of SmartEnergy.

It demonstrates practical experience with:

```text
C#
ASP.NET Core
.NET
REST APIs
PostgreSQL
Entity Framework Core
JWT
Software Architecture
Cloud Deployment
Git
GitHub
```

The project is intended both as a functional application and as a software development portfolio project.

---

## Author

**Joseph Paiz**

Computer Engineering student with interests in backend development, cloud applications, databases, IT infrastructure, and software engineering.

Technologies used across personal and academic projects include:

- C#
- .NET
- ASP.NET Core
- PostgreSQL
- SQL
- REST APIs
- JavaScript
- React
- Git
- GitHub

---

## License

This project is currently intended for educational, development, and portfolio purposes.

Unless a separate license file is included in the repository, all rights are reserved by the author.
