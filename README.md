# University Finder

A comprehensive ASP.NET Core MVC application to help students find universities across Europe by subject, location, and other criteria. Features include subject-based search, AI-powered cost of living assistant, user favorites, and search history.

## Features

- **Subject-Based Search**: Search for universities by field of study (Psychology, Engineering, Medicine, etc.)
- **Location Filtering**: Filter by country and city
- **AI Chatbot**: Get information about living costs and expenses in European cities
- **User Accounts**: Save favorite universities and track search history
- **Database-Driven**: All data stored in SQL Server database (no hardcoding)
- **HEI API Integration**: Sync university data from the HEI API Project

## Prerequisites

- .NET 8.0 SDK
- SQL Server (LocalDB or full SQL Server instance)
- OpenAI API Key (for AI chatbot feature)

## Setup Instructions

1. **Clone the repository** (if applicable)

2. **Configure the database connection string** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Your connection string here"
   }
   ```

3. **Add your OpenAI API key** in `appsettings.json`:
   ```json
   "OpenAI": {
     "ApiKey": "your-openai-api-key-here"
   }
   ```
   
   For development, you can also use User Secrets:
   ```bash
   dotnet user-secrets set "OpenAI:ApiKey" "your-api-key"
   ```

4. **Run the application**:
   ```bash
   dotnet run
   ```

5. **Seed the database**: The application will automatically seed countries and subjects on first run.

6. **Sync universities** (Admin only):
   - Create an admin user and assign the "Administrator" role
   - Navigate to `/Admin/Sync` to sync universities from the HEI API

## Project Structure

- **Models/**: Database entities (Country, City, University, Subject, UniversityProgram, etc.)
- **Data/**: ApplicationDbContext and migrations
- **Repositories/**: Data access layer with repository pattern
- **Services/**: Business logic (HeiApiService, OpenAiService, DataSeeder)
- **Controllers/**: MVC controllers (UniversityController, ChatController, AdminController)
- **Views/**: Razor views for UI
- **ViewModels/**: View models for complex views

## Database Schema

The application uses the following main entities:
- **Country**: European countries
- **City**: Cities with universities
- **University**: Higher education institutions
- **Subject**: Fields of study
- **UniversityProgram**: Programs/degrees offered by universities
- **CostOfLiving**: Living expenses by city
- **UserFavorites**: User's saved universities
- **SearchHistory**: User search queries

## API Integration

### HEI API
The application integrates with the HEI API Project (https://hei.api.uni-foundation.eu) to fetch university data. Use the admin sync page to populate the database.

### OpenAI API
The AI chatbot uses OpenAI's GPT-3.5-turbo model to answer questions about living costs. Ensure you have a valid API key configured.

## Usage

1. **Search for Universities**: 
   - Enter a subject (e.g., "Psychology", "Engineering") in the search bar
   - Optionally filter by country, city, or degree type
   - View results with program details

2. **View University Details**:
   - Click on any university to see detailed information
   - View all programs offered
   - Add to favorites (requires login)

3. **AI Assistant**:
   - Click the chatbot button in the bottom-right corner
   - Ask questions about living costs in European cities
   - The assistant uses cost of living data from the database

4. **User Features** (requires login):
   - Save favorite universities
   - View search history
   - Personalized recommendations

## Development Notes

- Email confirmation is disabled for easier development (can be enabled in `Program.cs`)
- The application automatically runs migrations and seeds data on startup
- All data is stored in the database - no hardcoded values
- Repository pattern is used for data access
- Services are registered with dependency injection

## License

This project is provided as-is for educational purposes.

