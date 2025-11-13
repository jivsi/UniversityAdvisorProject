# Troubleshooting Database Connection Issues

## Quick Checklist

1. ✅ **PostgreSQL is installed and running**
2. ✅ **Database `university_advisor` exists**
3. ✅ **Connection string is correct**
4. ✅ **Migrations have been run**

## Step-by-Step Fix

### 1. Verify PostgreSQL is Running

**Windows:**
```powershell
# Check if PostgreSQL service is running
Get-Service -Name postgresql*

# If not running, start it:
Start-Service -Name postgresql-x64-16  # Adjust version number if different
```

**Or use Services:**
- Press `Win + R`, type `services.msc`
- Look for "postgresql" service
- Right-click → Start (if stopped)

### 2. Test Connection Manually

Open PowerShell or Command Prompt and try:

```bash
psql -U postgres -h localhost -p 5432
```

Enter your password when prompted. If this fails, PostgreSQL might not be running or the password is wrong.

### 3. Create the Database (if it doesn't exist)

Once connected to PostgreSQL:

```sql
-- Check if database exists
\l

-- If it doesn't exist, create it:
CREATE DATABASE university_advisor;

-- Exit psql
\q
```

### 4. Verify Connection String

Your connection string in `appsettings.json` should be:
```
Host=localhost;Port=5432;Database=university_advisor;Username=postgres;Password=YOUR_PASSWORD
```

**Important:** If your password contains special characters like `%`, `&`, `#`, etc., they need to be URL-encoded:
- `%` becomes `%25`
- `&` becomes `%26`
- `#` becomes `%23`
- `@` becomes `%40`

### 5. Run Migrations

```bash
cd UniversityAdvisor
dotnet ef migrations add InitialPostgresMigration
dotnet ef database update
```

### 6. Test the Connection from Your App

Add this to test (temporary - remove after testing):

In `Program.cs`, you can add logging to see the exact error:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"Connection String: {connectionString?.Replace("Password=", "Password=***")}");
```

## Common Issues

### Issue: "No such host is known"
- PostgreSQL is not running
- Wrong hostname (should be `localhost` or `127.0.0.1`)

### Issue: "password authentication failed"
- Wrong password in connection string
- Password needs URL encoding for special characters

### Issue: "database does not exist"
- Database `university_advisor` hasn't been created
- Run: `CREATE DATABASE university_advisor;`

### Issue: "relation does not exist"
- Migrations haven't been run
- Run: `dotnet ef database update`

## Quick Test Script

Create a file `test-connection.ps1`:

```powershell
# Test PostgreSQL connection
$env:PGPASSWORD = "kokodekoko1Az%"
psql -U postgres -h localhost -p 5432 -d university_advisor -c "SELECT 1;"
```

If this works, your connection string should work too.

