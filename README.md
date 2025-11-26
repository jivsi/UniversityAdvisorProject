# UniversityFinder - Official Bulgarian Higher Education Platform

A modern ASP.NET Core MVC application for discovering and analyzing accredited universities in Bulgaria. Built with .NET 8, Supabase, and featuring a beautiful glassmorphism UI with dark/light theme support. Uses official data from NACID (National Agency for Evaluation and Accreditation) and NSI (National Statistical Institute).

## 🎨 Design Features

- **Premium UI/UX**: Glassmorphism design with smooth animations
- **Dark/Light Theme**: Automatic detection with manual toggle
- **Responsive Design**: Mobile-first, works on all devices
- **Official Data Sources**: RVU (NACID Register) for universities, NSI for statistics
- **Accreditation Tracking**: All universities verified through NACID
- **Regional Analytics**: Education statistics by Bulgarian regions
- **Advanced Filtering**: Filter by region, city, accreditation status, field of study

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

The application uses **Supabase** (PostgreSQL) for application data and **SQLite** for Identity.

- Application data (universities, statistics) stored in Supabase
- Identity/authentication uses local SQLite database
- Migrations run automatically on startup

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
│   ├── RvuService.cs (TODO: Automate RVU scraping/import pipeline)
│   ├── NsiService.cs (TODO: Integrate NSI statistical data feed)
│   ├── HeiApiService.cs (Legacy - deprecated)
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
- Search bar for accredited Bulgarian universities
- Stats cards (Universities, Regions, Programs)
- Feature highlights focused on Bulgarian higher education

### Universities List
- Card-based grid layout with hover animations
- Advanced filter sidebar:
  - Region & City (Bulgarian regions)
  - Accreditation status
  - Field of study
  - Degree type (Bachelor, Master, PhD)
- Search with autocomplete
- Responsive design (1/2/3 columns)
- All universities verified through NACID

### University Details
- Parallax hero section
- Floating info panel with stats
- Interactive tabs:
  - **Overview**: Description, website, accreditation info
  - **Programs**: All programs with details
  - **Education Statistics**: NSI data (enrollment, graduates, regional analytics)
  - **Accreditation**: NACID accreditation details
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

## 🔌 Data Sources

### RVU (NACID Register of Higher Education Institutions)
- **Source**: Official Bulgarian National Agency for Evaluation and Accreditation
- **Purpose**: Primary source for accredited universities in Bulgaria
- **Status**: Primary data source
- **TODO**: Automate RVU scraping/import pipeline

### NSI (National Statistical Institute)
- **Source**: Official Bulgarian National Statistical Institute
- **Purpose**: Statistical and analytical educational data (enrollment, graduates, regional breakdowns)
- **Status**: Statistical enrichment layer
- **TODO**: Integrate NSI statistical data feed

### Legacy APIs (Deprecated)
- **HEI API**: Legacy European university data (deprecated)
- **Hipolabs API**: Legacy international university search (deprecated)

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
- `Universities` - Accredited Bulgarian universities (RVU/NACID data)
- `Cities` - Bulgarian cities with coordinates
- `Countries` - Country information (Bulgaria-focused)
- `HigherEducationStatistics` - NSI statistical data
- `Programs` - University programs
- `Subjects` - Subject areas
- `UserFavorites` - User's favorite universities
- `SyncStatuses` - Data sync status tracking

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
