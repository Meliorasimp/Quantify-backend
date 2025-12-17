# Create a new SQL Server migration with proper naming convention
# Usage: .\add-migration.ps1 "YourFeatureName"

param(
    [Parameter(Mandatory=$true)]
    [string]$MigrationName
)

# Set environment to Development
$env:ASPNETCORE_ENVIRONMENT='Development'

# Add SqlServer_ prefix if not already present
if ($MigrationName -notmatch '^SqlServer_') {
    $MigrationName = "SqlServer_$MigrationName"
}

Write-Host "Creating migration: $MigrationName" -ForegroundColor Cyan
dotnet ef migrations add $MigrationName

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Migration created successfully!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Yellow
    Write-Host "  1. Review the migration file in the Migrations folder"
    Write-Host "  2. Run: .\update-local-db.ps1" -ForegroundColor Cyan
    Write-Host "     to apply the migration to your local database"
} else {
    Write-Host "`n❌ Migration creation failed." -ForegroundColor Red
}
