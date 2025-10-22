# University Advisor - Project Summary

## Overview

A complete ASP.NET Core MVC web application for helping students find universities worldwide. The application includes advanced search, filtering, AI chatbot assistance, and user authentication.

## Architecture

### Technology Stack
- **Framework**: ASP.NET Core 8.0 MVC
- **Database**: Supabase PostgreSQL with Row Level Security
- **ORM**: Entity Framework Core 8.0
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor Views, CSS3, JavaScript
- **Deployment**: Docker on Render.com

### Design Pattern
- **MVC (Model-View-Controller)**: Clean separation of concerns
- **Service Layer**: Business logic abstraction
- **Repository Pattern**: Via Entity Framework DbContext
- **Dependency Injection**: Built-in ASP.NET Core DI

## Project Structure

```
university-advisor/
├── UniversityAdvisor.sln              # Visual Studio solution file
├── Dockerfile                         # Docker containerization
├── render.yaml                        # Render.com deployment config
├── README.md                          # Comprehensive documentation
├── DEPLOYMENT.md                      # Quick deployment guide
│
└── UniversityAdvisor/                 # Main application
    ├── Program.cs                     # Application entry point
    ├── appsettings.json               # Configuration
    │
    ├── Models/                        # Domain models
    │   ├── University.cs              # University entity
    │   ├── Program.cs                 # Academic program entity
    │   ├── UserProfile.cs             # User profile entity
    │   ├── Favorite.cs                # User favorites
    │   ├── SearchHistory.cs           # Search tracking
    │   └── ApplicationUser.cs         # Identity user
    │
    ├── ViewModels/                    # Data transfer objects
    │   ├── SearchViewModel.cs         # Search page data
    │   ├── RegisterViewModel.cs       # Registration form
    │   └── LoginViewModel.cs          # Login form
    │
    ├── Controllers/                   # Request handlers
    │   ├── HomeController.cs          # Home & Search
    │   ├── AccountController.cs       # Auth operations
    │   └── ChatController.cs          # AI chat API
    │
    ├── Views/                         # UI templates
    │   ├── Shared/
    │   │   └── _Layout.cshtml         # Master layout
    │   ├── Home/
    │   │   ├── Index.cshtml           # Homepage
    │   │   ├── Search.cshtml          # Search page
    │   │   └── Details.cshtml         # University details
    │   └── Account/
    │       ├── Login.cshtml           # Login page
    │       └── Register.cshtml        # Registration page
    │
    ├── Services/                      # Business logic
    │   ├── IUniversityService.cs      # University service interface
    │   ├── UniversityService.cs       # University operations
    │   ├── IAIChatService.cs          # Chat service interface
    │   └── AIChatService.cs           # AI chatbot logic
    │
    ├── Data/
    │   └── ApplicationDbContext.cs    # EF Core context
    │
    └── wwwroot/                       # Static files
        ├── css/
        │   └── site.css               # Main stylesheet
        └── js/
            └── site.js                # JavaScript
```

## Key Features

### 1. University Search Engine
- Full-text search across universities and programs
- Multiple filter options:
  - Country and city selection
  - Tuition range (min/max)
  - Degree type (Bachelor/Master/PhD)
  - Sorting options (name, tuition, acceptance rate)
- Real-time filtering with dynamic city dropdown
- Paginated results with cards

### 2. AI Chatbot Assistant
- Context-aware responses about universities
- Answers questions about:
  - Tuition costs and fees
  - Living expenses
  - Available programs
  - Location information
  - Acceptance rates
  - Student population
- Simple natural language processing
- Real-time chat interface

### 3. User Authentication
- Registration with email/password
- Secure login/logout
- Password requirements enforcement
- Session management
- Remember me functionality
- Future: User profiles, saved searches, favorites

### 4. Modern UI/UX
- Responsive design (mobile, tablet, desktop)
- Clean, professional aesthetic
- Inspired by the reference design
- Smooth transitions and hover effects
- Accessible color contrast
- Hero section with background image
- Card-based layouts

### 5. Database Features
- 8 pre-populated universities
- 40+ academic programs
- Normalized schema
- Indexed for performance
- Row Level Security (RLS)
- Secure connections

## Database Schema

### universities
- id, name, country, city, description
- website_url, logo_url
- tuition_fee_min, tuition_fee_max
- living_cost_monthly, acceptance_rate
- student_count, founded_year
- created_at, updated_at

### programs
- id, university_id (FK)
- name, degree_type, duration_years
- language, description
- created_at

### user_profiles
- id (FK to auth.users)
- email, full_name, country, interests
- created_at, updated_at

### favorites
- id, user_id (FK), university_id (FK)
- created_at

### search_history
- id, user_id (FK)
- search_query, filters_applied (JSONB)
- created_at

## Sample Data Included

Universities from:
- 🇨🇦 Canada: Toronto, Dalhousie
- 🇺🇸 USA: Harvard
- 🇬🇧 UK: Oxford
- 🇩🇪 Germany: TU Munich
- 🇦🇺 Australia: Melbourne
- 🇨🇭 Switzerland: ETH Zurich
- 🇯🇵 Japan: Tokyo

Programs include:
- Computer Science, Medicine, Business Administration
- Engineering, Psychology, Law
- Data Science, International Relations
- Architecture, Economics

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | / | Homepage |
| GET | /Home/Search | Search universities |
| GET | /Home/Details/{id} | University details |
| GET | /Home/GetCities?country={} | Get cities by country |
| POST | /Chat/SendMessage | AI chat message |
| GET | /Account/Register | Registration form |
| POST | /Account/Register | Process registration |
| GET | /Account/Login | Login form |
| POST | /Account/Login | Process login |
| POST | /Account/Logout | Logout user |

## Security Features

✅ ASP.NET Core Identity for authentication
✅ Password hashing (PBKDF2)
✅ Anti-forgery tokens on forms
✅ Row Level Security on database
✅ HTTPS enforcement in production
✅ SQL injection prevention (EF Core)
✅ XSS protection (Razor encoding)
✅ Secure session management

## Deployment

### Local Development
```bash
cd UniversityAdvisor
dotnet restore
dotnet run
```
Access at: https://localhost:5001

### Docker Build
```bash
docker build -t university-advisor .
docker run -p 8080:8080 university-advisor
```

### Render.com Deployment
1. Push to GitHub
2. Connect repository to Render
3. Set environment variables
4. Deploy (automatic via render.yaml)

## Configuration

Required environment variables:
- `ASPNETCORE_ENVIRONMENT`: Development/Production
- `ASPNETCORE_URLS`: http://+:8080
- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string

## Performance Considerations

- Database indexes on frequently queried columns
- Entity Framework lazy loading
- Static file caching
- Responsive images
- Minimal JavaScript dependencies
- CSS minification in production

## Future Enhancements

Potential features to add:
- [ ] OpenAI integration for advanced AI chat
- [ ] University comparison tool
- [ ] Application deadline reminders
- [ ] Document upload for applications
- [ ] User review and rating system
- [ ] Scholarship information
- [ ] Virtual campus tours
- [ ] Admin panel for data management
- [ ] Email notifications
- [ ] Social sharing features

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome)

## Dependencies

### NuGet Packages
- Microsoft.AspNetCore.Identity.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.Design (8.0.0)
- Npgsql.EntityFrameworkCore.PostgreSQL (8.0.0)
- Microsoft.AspNetCore.Authentication.JwtBearer (8.0.0)
- Supabase (1.4.0)

## License

MIT License - Free to use and modify

## Contact & Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Check README.md for detailed documentation
- Review DEPLOYMENT.md for deployment help

---

**Built with ❤️ using ASP.NET Core and Supabase**
