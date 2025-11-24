# UniversityFinder - Premium University Discovery Platform

A modern, premium ASP.NET Core MVC application for discovering universities across Europe. Built with .NET 8, SQLite, and featuring a beautiful glassmorphism UI with dark/light theme support.

## 🎨 Design Features

- **Premium UI/UX**: Glassmorphism design with smooth animations
- **Dark/Light Theme**: Automatic detection with manual toggle
- **Responsive Design**: Mobile-first, works on all devices
- **Real-time Search**: Autocomplete powered by Hipolabs API
- **City Metrics**: Safety, education, and quality of life scores from Teleport API
- **Advanced Filtering**: Filter by country, city, tuition, ranking, degree type, language

## 🚀 Quick Start

### Prerequisites

- **.NET 8.0 SDK** or later ([Download](https://dotnet.microsoft.com/download))
- **Visual Studio 2022**, **VS Code**, or **JetBrains Rider**
- No database installation required (uses SQLite)

### Installation

1. **Clone or navigate to the project directory**
   ```bash
   cd UniversityFinder
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the project**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open your browser**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - The application will automatically:
     - Create the SQLite database
     - Run migrations
     - Seed initial data (countries, subjects)

### Database

The application uses **SQLite** - no external database installation needed!

- Database file: `app.db` (created automatically in the project root)
- Migrations run automatically on startup
- All data persists in the SQLite file

## 📁 Project Structure

```
UniversityFinder/
├── Controllers/          # MVC Controllers
│   ├── HomeController.cs
│   ├── UniversityController.cs
│   └── AdminController.cs
├── Models/              # Entity models
│   ├── University.cs
│   ├── City.cs
│   ├── Country.cs
│   ├── CityQuality.cs   # Teleport API data
│   └── ...
├── Services/            # Business logic
│   ├── HeiApiService.cs
│   ├── HipolabsApiService.cs
│   ├── TeleportApiService.cs
│   └── ...
├── Views/               # Razor views
│   ├── Home/
│   ├── University/
│   └── Admin/
├── Data/                # EF Core DbContext
│   └── ApplicationDbContext.cs
├── wwwroot/             # Static files (CSS, JS, images)
└── Migrations/          # EF Core migrations
```

## 🔧 Development

### Running in Visual Studio

1. Open `UniversityFinder.sln` in Visual Studio 2022
2. Press `F5` to run with debugging
3. Or press `Ctrl+F5` to run without debugging

### Running in VS Code

1. Open the project folder in VS Code
2. Install the **C# Dev Kit** extension (recommended)
3. Press `F5` or run from terminal:
   ```bash
   dotnet run
   ```

### Running in JetBrains Rider

1. Open `UniversityFinder.sln` in Rider
2. Set `UniversityFinder` as the startup project
3. Press `Shift+F10` to run

### Database Migrations

Migrations run automatically on startup. To create a new migration manually:

```bash
dotnet ef migrations add MigrationName --context ApplicationDbContext
```

To apply migrations manually:

```bash
dotnet ef database update --context ApplicationDbContext
```

## 🎯 Features

### Homepage
- Full-screen hero section with animated gradients
- Search bar with live autocomplete (Hipolabs API)
- Stats cards (Universities, Countries, Programs)
- Feature highlights

### Universities List
- Card-based grid layout with hover animations
- Advanced filter sidebar:
  - Country & City
  - Tuition range (min/max)
  - Ranking range (min/max)
  - Degree type (Bachelor, Master, PhD)
  - Language of instruction
- Search with autocomplete
- Responsive design (1/2/3 columns)

### University Details
- Parallax hero section
- Floating info panel with stats
- Interactive tabs:
  - **Overview**: Description, website, basic info
  - **Programs**: All programs with details
  - **City Metrics**: Safety, education, healthcare scores (Teleport API)
  - **Safety**: Safety and quality of life metrics
- Favorite button (for logged-in users)

### Favorites
- Masonry-style card layout
- Smooth remove animations
- Empty state with call-to-action

### Admin Dashboard
- Premium sync dashboard
- Real-time progress tracking
- Animated progress bars
- Status chips (Running/Completed/Failed)
- Statistics grid

## 🔌 API Integrations

### Hipolabs API
- **Endpoint**: `http://universities.hipolabs.com`
- **Purpose**: University search and autocomplete
- **Caching**: 60 minutes in-memory cache
- **Usage**: Real-time autocomplete suggestions

### Teleport API
- **Endpoint**: `https://api.teleport.org/api`
- **Purpose**: City quality metrics (safety, education, healthcare, etc.)
- **Caching**: 24 hours in database
- **Usage**: City metrics tab on university details page

### HEI API
- **Endpoint**: `https://hei.api.uni-foundation.eu`
- **Purpose**: Sync universities and programs
- **Usage**: Admin sync dashboard

## 🎨 Design System

### Colors
- **Primary Gradient**: Purple (#667eea) → Pink (#764ba2)
- **Secondary Gradient**: Blue (#4facfe) → Cyan (#00f2fe)
- **Accent Gradient**: Green (#10b981) → Emerald (#059669)

### Typography
- **Headings**: Poppins (Bold, 700-900)
- **Body**: Inter (Regular, 400-600)

### Icons
- **Lucide Icons** (via CDN)

### Effects
- **Glassmorphism**: `backdrop-filter: blur(10px)`
- **Animations**: Fade-in, slide-up, hover lift
- **Transitions**: Smooth 0.3s cubic-bezier

## 🔐 Authentication

The application uses ASP.NET Core Identity with SQLite.

### Default Settings
- Email confirmation: **Disabled** (for easier development)
- Password requirements:
  - Minimum 6 characters
  - Requires digit, lowercase, uppercase
  - No special characters required

### Creating an Admin User

1. Register a new account
2. In the database, update the user's role (or use Identity UI)

## 📊 Database Schema

### Key Tables
- `Universities` - University information
- `Cities` - City data with coordinates
- `Countries` - Country information
- `CityQualities` - Teleport API metrics
- `Programs` - University programs
- `Subjects` - Subject areas
- `UserFavorites` - User's favorite universities
- `SyncStatuses` - HEI sync status tracking

## 🛠️ Configuration

### Connection String

Located in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

### Environment Variables

No external API keys required! All APIs used are public and free.

## 🚀 Deployment

### Production Considerations

1. **Database**: Consider migrating to SQL Server or PostgreSQL for production
2. **Caching**: Consider Redis for distributed caching
3. **CDN**: Use CDN for static assets (TailwindCSS, Lucide icons)
4. **HTTPS**: Ensure HTTPS is enabled
5. **Environment**: Set `ASPNETCORE_ENVIRONMENT=Production`

### Build for Production

```bash
dotnet publish -c Release -o ./publish
```

## 📝 Code Structure

### Services
- **HipolabsApiService**: University search API integration
- **TeleportApiService**: City quality metrics API integration
- **HeiApiService**: HEI API synchronization
- **UniversitySearchService**: Search logic
- **UserFavoriteService**: Favorites management

### Controllers
- **HomeController**: Landing page
- **UniversityController**: Universities list, details, search, favorites
- **AdminController**: Sync dashboard

### Models
- All models use EF Core annotations
- SQLite-compatible data types
- Navigation properties for relationships

## 🐛 Troubleshooting

### Database Issues

If you see "no such table" errors:
1. Delete `app.db` file
2. Restart the application (migrations run automatically)

### Build Errors

If build fails:
1. Clean solution: `dotnet clean`
2. Restore packages: `dotnet restore`
3. Rebuild: `dotnet build`

### Port Already in Use

If port 5000/5001 is in use:
- Change ports in `Properties/launchSettings.json`
- Or use: `dotnet run --urls "http://localhost:5002"`

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Entity Framework Core](https://docs.microsoft.com/ef/core)
- [TailwindCSS Documentation](https://tailwindcss.com/docs)
- [Lucide Icons](https://lucide.dev)

## 📄 License

This project is part of the UniversityFinder application.

## ✨ Credits

- **Design Inspiration**: Modern SaaS platforms, QS Rankings, Notion
- **Icons**: Lucide Icons
- **Fonts**: Google Fonts (Inter, Poppins)
- **CSS Framework**: TailwindCSS

---

**Built with ❤️ using ASP.NET Core MVC and modern web technologies**
