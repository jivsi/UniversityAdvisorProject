# Quick Deployment Guide

## Summary

This is a University Advisor web application built with:
- **ASP.NET Core MVC** (Backend)
- **Supabase PostgreSQL** (Database)
- **Docker** (Containerization)
- **Render.com** (Hosting)

## What's Included

✅ Complete ASP.NET Core MVC application with:
  - University search with filters (country, city, tuition, degree type)
  - Individual user accounts with ASP.NET Identity
  - AI chatbot for university information
  - Modern responsive UI

✅ Supabase database with:
  - 8 sample universities (Harvard, Oxford, Toronto, etc.)
  - 40+ sample programs across multiple fields
  - Row Level Security enabled
  - Ready-to-use schema

✅ Deployment files:
  - Dockerfile for containerization
  - render.yaml for Render configuration
  - .gitignore for version control

## Deployment Steps

### 1. Update Database Connection

Open `UniversityAdvisor/appsettings.json` and update the connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.bmsswqwnmpxrtsernnpt.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD_HERE"
  }
}
```

**Note**: Replace `YOUR_PASSWORD_HERE` with your actual Supabase database password.

### 2. Push to GitHub

```bash
git init
git add .
git commit -m "Initial commit - University Advisor"
git remote add origin https://github.com/yourusername/university-advisor.git
git push -u origin main
```

### 3. Deploy on Render.com

1. Go to [render.com](https://render.com) and sign in
2. Click **"New +"** → **"Web Service"**
3. Connect your GitHub repository
4. Configure:
   - **Name**: university-advisor
   - **Runtime**: Docker
   - **Plan**: Free
   - **Build Command**: (leave empty, uses Dockerfile)
   - **Start Command**: (leave empty, uses Dockerfile)

5. Add Environment Variables:
   ```
   ASPNETCORE_ENVIRONMENT = Production
   ASPNETCORE_URLS = http://+:8080
   ConnectionStrings__DefaultConnection = Host=db.bmsswqwnmpxrtsernnpt.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD
   ```

6. Click **"Create Web Service"**

### 4. Wait for Deployment

- Render will build your Docker container (~5-10 minutes)
- Once complete, your app will be live at: `https://your-app-name.onrender.com`

## Testing Locally (Optional)

If you have .NET 8.0 SDK installed:

```bash
cd UniversityAdvisor
dotnet restore
dotnet run
```

Visit: `https://localhost:5001`

## Features to Test

1. **Homepage**: Modern hero section with search
2. **Search**: Filter universities by multiple criteria
3. **Details**: View university information with AI chatbot
4. **Register/Login**: Create account and sign in
5. **AI Chat**: Ask about costs, programs, locations

## Sample Data

The database includes:
- University of Toronto (Canada)
- Harvard University (USA)
- Dalhousie University (Canada)
- University of Oxford (UK)
- Technical University of Munich (Germany)
- University of Melbourne (Australia)
- ETH Zurich (Switzerland)
- University of Tokyo (Japan)

Each has programs in: Computer Science, Medicine, Business, Engineering, Psychology, Law, Data Science, etc.

## Troubleshooting

**Build fails on Render?**
- Check that all files are committed to Git
- Verify Dockerfile is in root directory
- Check Render build logs for specific errors

**Database connection fails?**
- Verify connection string is correct
- Check Supabase database is running
- Ensure password has no special characters that need escaping

**App starts but shows errors?**
- Check environment variables in Render dashboard
- Verify `ASPNETCORE_URLS` is set to `http://+:8080`
- Check application logs in Render

## Next Steps

Once deployed:
1. Test the search functionality
2. Create a user account
3. Try the AI chatbot on university detail pages
4. Add more universities to the database
5. Customize the styling to match your brand

## Support

For issues or questions, check:
- README.md for detailed documentation
- Render logs for deployment errors
- Supabase dashboard for database issues
