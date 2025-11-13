# University Advisor Project

A comprehensive web application for searching and discovering universities, with AI-powered assistance for students looking to study abroad.

## Features

- 🔍 **University Search**: Search universities by profession, country, city, and tuition cost
- 🤖 **AI Assistant**: Get personalized answers about living costs, tuition fees, rent prices, and student life
- 👤 **User Accounts**: Create an account to save favorite universities and track your search history
- ⭐ **Ratings & Reviews**: Rate and review universities to help other students
- 🌍 **European Universities**: Focus on universities across Europe with detailed information

## Tech Stack

- **.NET 8.0** - ASP.NET Core MVC
- **PostgreSQL** - Database (via Entity Framework Core + Npgsql)
- **ASP.NET Identity** - User authentication and authorization
- **OpenAI API** - AI-powered chat assistance (optional)
- **Bootstrap 5** - Modern, responsive UI

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) (version 12 or higher)
- [Entity Framework Core Tools](https://docs.microsoft.com/en-us/ef/core/cli/dotnet) (for migrations)

## Setup Instructions

### 1. Clone the Repository

```bash
git clone <repository-url>
cd UniversityAdvisorProject
```

### 2. Set Up PostgreSQL

1. Install PostgreSQL on your machine
2. Create a new database:
   ```sql
   CREATE DATABASE university_advisor;
   ```
3. Note your PostgreSQL credentials (host, port, username, password)

### 3. Configure Connection String

Update `UniversityAdvisor/appsettings.json` with your PostgreSQL connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=university_advisor;Username=postgres;Password=yourpassword"
  }
}
```

For production, use environment variables or User Secrets:
```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=university_advisor;Username=postgres;Password=yourpassword"
```

### 4. Run Database Migrations

```bash
cd UniversityAdvisor
dotnet ef migrations add InitialPostgresMigration
dotnet ef database update
```

### 5. (Optional) Configure OpenAI API

To enable AI chat features, add your OpenAI API key:

**Using User Secrets (recommended for development):**
```bash
dotnet user-secrets set "OpenAI:ApiKey" "your-openai-api-key-here"
```

**Or add to appsettings.Development.json:**
```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key-here"
  }
}
```

> **Note**: The application will work without an OpenAI API key, but will use a simpler rule-based chat system instead.

### 6. Restore Dependencies and Run

```bash
dotnet restore
dotnet run
```

The application will be available at `https://localhost:5001` or `http://localhost:5000`.

## Project Structure

```
UniversityAdvisor/
├── Controllers/          # MVC controllers
│   ├── AccountController.cs
│   ├── HomeController.cs
│   ├── UniversitiesController.cs
│   ├── AIAdvisorController.cs
│   └── ChatController.cs
├── Data/                # Database context
│   └── ApplicationDbContext.cs
├── Models/              # Entity models
│   ├── University.cs
│   ├── Program.cs
│   ├── ApplicationUser.cs
│   ├── Rating.cs
│   └── ...
├── Services/            # Business logic services
│   ├── IUniversityService.cs
│   ├── UniversityService.cs
│   ├── IUniversityApiService.cs
│   ├── UniversityApiService.cs
│   ├── IAIChatService.cs
│   └── AIChatService.cs
├── Views/               # Razor views
│   ├── Home/
│   ├── Universities/
│   ├── Account/
│   └── AIAdvisor/
├── ViewModels/          # View models
└── wwwroot/             # Static files (CSS, JS)
```

## Database Migrations

### Create a New Migration

```bash
dotnet ef migrations add MigrationName
```

### Apply Migrations

```bash
dotnet ef database update
```

### Rollback a Migration

```bash
dotnet ef database update PreviousMigrationName
```

## Environment Variables

For production deployment, set these environment variables:

- `ConnectionStrings__DefaultConnection` - PostgreSQL connection string
- `OpenAI__ApiKey` - OpenAI API key (optional)
- `ASPNETCORE_ENVIRONMENT` - Set to `Production` for production

## Running in Visual Studio or Rider

1. Open `UniversityAdvisor.sln` in Visual Studio or Rider
2. Ensure PostgreSQL is running
3. Update the connection string in `appsettings.json` or User Secrets
4. Run the project (F5 or Ctrl+F5)

## API Integration

The application integrates with the [HipoLabs Universities API](http://universities.hipolabs.com/) to fetch university data. This is a free, public API that doesn't require authentication.

For AI features, the application uses the OpenAI API (requires API key).

## User Roles

- **Regular User**: Can search universities, save favorites, and leave ratings
- **Admin**: (Seeded automatically) Can access admin features (if implemented)

To assign the Admin role to a user, use the following in the application startup or a separate script:

```csharp
var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
var user = await userManager.FindByEmailAsync("admin@example.com");
if (user != null)
{
    await userManager.AddToRoleAsync(user, "Admin");
}
```

## Troubleshooting

### Database Connection Issues

- Ensure PostgreSQL is running: `pg_isready` or check services
- Verify connection string format matches PostgreSQL requirements
- Check firewall settings if connecting to a remote database

### Migration Errors

- Ensure the database exists before running migrations
- If migrations fail, you may need to drop and recreate the database:
  ```bash
  dropdb university_advisor
  createdb university_advisor
  dotnet ef database update
  ```

### OpenAI API Issues

- Verify your API key is correct
- Check your OpenAI account has available credits
- The application will fall back to rule-based responses if the API fails

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

This project is licensed under the MIT License.

## Support

For issues and questions, please open an issue on the repository.

