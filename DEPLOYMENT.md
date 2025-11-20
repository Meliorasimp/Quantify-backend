# Deployment Guide

## Prerequisites

Before deploying, ensure you have:

- MonsterASP.NET hosting account
- Database `db32938` already created (✓ Complete)
- Frontend deployed and URL available

## Environment Variables Required

Set these environment variables on your hosting platform:

### Required Variables

```
ASPNETCORE_ENVIRONMENT=Production
JWT_KEY=<your-secret-jwt-key>
```

### Optional Variables (if different from defaults)

```
Redis__ConnectionString=<redis-connection-string>
```

## Configuration Steps

### 1. Update Production Configuration

After deploying your frontend, update `appsettings.Production.json`:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-frontend-url.com",
      "http://your-frontend-url.com"
    ]
  }
}
```

### 2. Database Migrations

Your database migrations need to be run on the production database. You have two options:

**Option A: Run migrations from deployed app**

- Ensure your hosting platform allows database access
- Migrations will run automatically on first deployment if configured

**Option B: Generate SQL script and run manually**

```powershell
dotnet ef migrations script -o migrate.sql
```

Then execute the SQL script through the database management interface.

### 3. Publish Your Application

Build and publish the application:

```powershell
dotnet publish -c Release -o ./publish
```

The files in the `./publish` folder are ready for deployment.

### 4. Deploy to MonsterASP.NET

Follow your hosting provider's deployment instructions:

- Upload the contents of the `./publish` folder
- Configure environment variables in the hosting control panel
- Set `ASPNETCORE_ENVIRONMENT` to `Production`
- Set your `JWT_KEY` value

### 5. Verify Deployment

After deployment:

1. Check the application logs for errors
2. Test the GraphQL endpoint: `https://your-backend-url/graphql`
3. Verify database connection is working
4. Test authentication flow

## Important Notes

- ✅ `appsettings.Production.json` is now tracked in Git and will be deployed
- ✅ `appsettings.Development.json` remains private (not tracked)
- ✅ Database connection string is configured for production
- ✅ Redis connection is configurable per environment
- ✅ CORS is configurable per environment
- ⚠️ Make sure to set `JWT_KEY` environment variable before starting the app
- ⚠️ Update the CORS allowed origins with your actual frontend URL

## Troubleshooting

### Database Connection Issues

- Verify the connection string is correct in `appsettings.Production.json`
- Ensure the app is running on MonsterASP.NET infrastructure (firewall requirement)

### CORS Errors

- Add your frontend URL to `Cors:AllowedOrigins` in `appsettings.Production.json`
- Redeploy after making changes

### JWT Authentication Errors

- Verify `JWT_KEY` environment variable is set
- Ensure it matches the key used to generate tokens

## Security Checklist

- [ ] `JWT_KEY` is set as environment variable (not in appsettings)
- [ ] Database password is only in `appsettings.Production.json` (deployed but not in Development)
- [ ] `.env` file is not tracked in Git
- [ ] CORS is configured to only allow your frontend domain
- [ ] HTTPS is enabled in production
