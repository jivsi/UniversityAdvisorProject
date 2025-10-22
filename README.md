# University Advisor

A comprehensive ASP.NET Core MVC web application that helps high school students find and explore universities worldwide. The application features university search with advanced filters, AI-powered chatbot assistance, and user authentication.

## Features

- **University Search Engine**: Search universities by program/major, country, city, tuition range, and degree type
- **Advanced Filters**: Filter by location, tuition costs, acceptance rates, and more
- **AI Chatbot Assistant**: Get answers about tuition costs, living expenses, programs, and admission requirements
- **User Authentication**: Secure registration and login with ASP.NET Identity
- **Modern UI**: Clean, responsive design inspired by professional university advisor platforms
- **Supabase Database**: PostgreSQL database with Row Level Security for data persistence

## Technology Stack

- **Backend**: ASP.NET Core 8.0 MVC
- **Database**: Supabase PostgreSQL
- **Authentication**: ASP.NET Core Identity
- **ORM**: Entity Framework Core
- **Frontend**: Razor Views, CSS3, Vanilla JavaScript
- **Deployment**: Docker on Render.com

## Project Structure

```
UniversityAdvisor/
├── Controllers/          # MVC Controllers
│   ├── HomeController.cs
│   ├── AccountController.cs
│   └── ChatController.cs
├── Models/              # Data models
│   ├── University.cs
│   ├── Program.cs
│   ├── UserProfile.cs
│   ├── Favorite.cs
│   └── SearchHistory.cs
├── ViewModels/          # View models for forms
│   ├── SearchViewModel.cs
│   ├── LoginViewModel.cs
│   └── RegisterViewModel.cs
├── Views/               # Razor views
│   ├── Home/
│   ├── Account/
│   └── Shared/
├── Services/            # Business logic
│   ├── UniversityService.cs
│   └── AIChatService.cs
├── Data/                # Database context
│   └── ApplicationDbContext.cs
└── wwwroot/             # Static files
    ├── css/
    └── js/
```

## Database Schema

The application uses Supabase PostgreSQL with the following tables:

- **universities**: University information (name, location, tuition, etc.)
- **programs**: Academic programs/majors offered by universities
- **user_profiles**: Extended user profile information
- **favorites**: User's saved universities
- **search_history**: Track user search queries

All tables have Row Level Security (RLS) enabled for data protection.

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK
- PostgreSQL (Supabase account)
- Git

### Local Development

1. **Clone the repository**
   ```bash
   git clone <your-repo-url>
   cd university-advisor
   ```

2. **Configure Database Connection**

   Update `appsettings.json` with your Supabase connection details:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=db.xxxxx.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD"
     },
     "Supabase": {
       "Url": "https://xxxxx.supabase.co",
       "Key": "your-anon-key"
     }
   }
   ```

3. **Restore Dependencies**
   ```bash
   cd UniversityAdvisor
   dotnet restore
   ```

4. **Run Migrations** (if needed)
   ```bash
   dotnet ef database update
   ```

5. **Run the Application**
   ```bash
   dotnet run
   ```

6. **Access the Application**

   Open your browser and navigate to `https://localhost:5001`

## Deployment on Render.com

### Step 1: Prepare Your Repository

1. Initialize Git repository:
   ```bash
   git init
   git add .
   git commit -m "Initial commit"
   ```

2. Push to GitHub/GitLab:
   ```bash
   git remote add origin <your-repo-url>
   git push -u origin main
   ```

### Step 2: Deploy on Render

1. **Create a Render Account**
   - Go to [render.com](https://render.com)
   - Sign up or log in

2. **Create a New Web Service**
   - Click "New +" → "Web Service"
   - Connect your GitHub/GitLab repository
   - Select your repository

3. **Configure the Service**
   - **Name**: university-advisor
   - **Region**: Oregon (or your preferred region)
   - **Branch**: main
   - **Runtime**: Docker
   - **Plan**: Free

4. **Add Environment Variables**

   In the Render dashboard, add these environment variables:

   ```
   ASPNETCORE_ENVIRONMENT = Production
   ASPNETCORE_URLS = http://+:8080
   ConnectionStrings__DefaultConnection = Host=db.xxxxx.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD
   ```

5. **Deploy**
   - Click "Create Web Service"
   - Render will automatically build and deploy your application
   - Wait for the deployment to complete (5-10 minutes)

6. **Access Your Application**
   - Your app will be available at: `https://university-advisor.onrender.com`

### Step 3: Database Setup

The Supabase database is already configured and includes:
- Sample universities (University of Toronto, Harvard, Oxford, etc.)
- Sample programs (Computer Science, Medicine, Business, etc.)
- RLS policies for secure data access

## Using the Application

### For Students

1. **Browse Universities**
   - Visit the homepage
   - Click "Search" or enter a program/university name

2. **Filter Results**
   - Use the sidebar filters to refine your search
   - Filter by country, city, tuition range, degree type

3. **View Details**
   - Click on any university to see detailed information
   - View programs, tuition costs, acceptance rates

4. **Chat with AI**
   - On the university details page, use the AI chatbot
   - Ask about costs, programs, location, admission requirements

5. **Create Account**
   - Register to save favorite universities
   - Track your search history

### Example Questions for AI Chatbot

- "How much does tuition cost?"
- "What are the living expenses?"
- "What programs do they offer?"
- "Where is the university located?"
- "What is the acceptance rate?"

## API Endpoints

- `GET /` - Homepage
- `GET /Home/Search` - Search universities
- `GET /Home/Details/{id}` - University details
- `GET /Home/GetCities?country={country}` - Get cities by country
- `POST /Chat/SendMessage` - Send message to AI chatbot
- `GET /Account/Login` - Login page
- `POST /Account/Login` - Process login
- `GET /Account/Register` - Registration page
- `POST /Account/Register` - Process registration
- `POST /Account/Logout` - Logout

## Customization

### Adding More Universities

Connect to your Supabase database and insert data:

```sql
INSERT INTO universities (name, country, city, description, tuition_fee_min, tuition_fee_max, living_cost_monthly)
VALUES ('New University', 'USA', 'Boston', 'Description...', 40000, 50000, 1800);
```

### Modifying the AI Chatbot

Edit `Services/AIChatService.cs` to customize responses:

```csharp
if (lowerMessage.Contains("your-keyword"))
{
    return "Your custom response";
}
```

### Styling Changes

Edit `wwwroot/css/site.css` to customize the appearance.

## Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ASPNETCORE_ENVIRONMENT` | Development/Production | Yes |
| `ASPNETCORE_URLS` | URL binding (http://+:8080) | Yes |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Yes |

## Security Features

- **ASP.NET Identity**: Secure user authentication and authorization
- **Password Hashing**: All passwords are hashed using BCrypt
- **Row Level Security**: Supabase RLS policies protect user data
- **HTTPS**: SSL/TLS encryption for data in transit
- **Anti-Forgery Tokens**: CSRF protection on all forms

## Performance Optimizations

- **Database Indexing**: Indexes on frequently queried columns
- **Lazy Loading**: Entity Framework lazy loading for related data
- **Static File Caching**: Browser caching for CSS/JS files
- **Responsive Images**: Optimized image loading

## Troubleshooting

### Database Connection Issues

- Verify your Supabase connection string is correct
- Check that your IP is allowed in Supabase settings
- Ensure the database password is correct

### Build Errors

- Run `dotnet restore` to restore NuGet packages
- Check that you have .NET 8.0 SDK installed
- Verify all project files are present

### Deployment Issues on Render

- Check the build logs in Render dashboard
- Verify environment variables are set correctly
- Ensure Dockerfile is in the root directory

## Future Enhancements

- [ ] Advanced AI chatbot with OpenAI integration
- [ ] User dashboard with saved universities
- [ ] Application tracking system
- [ ] Compare universities side-by-side
- [ ] Email notifications for deadlines
- [ ] Social features (reviews, ratings)
- [ ] Mobile app version

## Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License.

## Support

For issues, questions, or contributions, please open an issue on GitHub.

## Acknowledgments

- Supabase for database hosting
- Render.com for application hosting
- Pexels for stock images
- ASP.NET Core team for the framework
