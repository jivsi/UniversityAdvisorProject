# Quick Start Guide

## What You Have

A complete University Advisor web application with:
- ✅ ASP.NET Core MVC (.NET 8.0)
- ✅ Supabase PostgreSQL database (already configured with sample data)
- ✅ User authentication (register/login)
- ✅ University search with filters
- ✅ AI chatbot for university info
- ✅ Modern responsive UI
- ✅ Ready for Render.com deployment

## 3-Step Deployment

### Step 1: Update Database Password

Open `UniversityAdvisor/appsettings.json` and replace the connection string:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=db.bmsswqwnmpxrtsernnpt.supabase.co;Database=postgres;Username=postgres;Password=YOUR_ACTUAL_PASSWORD"
}
```

### Step 2: Push to GitHub

```bash
git init
git add .
git commit -m "University Advisor app"
git remote add origin https://github.com/yourusername/university-advisor.git
git push -u origin main
```

### Step 3: Deploy on Render

1. Go to [render.com](https://render.com)
2. Click **New +** → **Web Service**
3. Connect your GitHub repo
4. Settings:
   - Runtime: **Docker**
   - Plan: **Free**
5. Add environment variable:
   ```
   ConnectionStrings__DefaultConnection = Host=db.bmsswqwnmpxrtsernnpt.supabase.co;Database=postgres;Username=postgres;Password=YOUR_PASSWORD
   ```
6. Click **Create Web Service**
7. Wait 5-10 minutes for build
8. Access your app at: `https://your-app.onrender.com`

## That's It!

Your app is now live with:
- 8 universities from around the world
- 40+ academic programs
- Working search and filters
- AI chatbot
- User authentication

## Test It

1. Visit your deployed URL
2. Try searching for "Computer Science"
3. Filter by country (Canada, USA, UK, etc.)
4. Click on a university to see details
5. Use the AI chatbot to ask about costs
6. Create an account to test authentication

## Need Help?

- **Detailed docs**: See README.md
- **Deployment issues**: Check DEPLOYMENT.md
- **Project overview**: Read PROJECT_SUMMARY.md

## What's in the Database

**Universities:**
- University of Toronto (Canada) - $45k-55k/year
- Harvard University (USA) - $51k-57k/year
- University of Oxford (UK) - $30k-40k/year
- Technical University of Munich (Germany) - $0-500/year
- And 4 more...

**Programs per university:**
- Computer Science, Medicine, Business
- Engineering, Psychology, Law
- Data Science, Architecture, Economics
- And more...

## Common Issues

**Build fails?** → Check all files are committed to Git

**Can't connect to database?** → Verify password in environment variables

**App shows errors?** → Check Render logs and environment variables

---

**You're all set! 🎉**
