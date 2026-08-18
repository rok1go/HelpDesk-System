$ErrorActionPreference = "Stop"

function Assert-CommandSucceeded
{
    param([string]$Step)

    if ($LASTEXITCODE -ne 0)
    {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

docker compose up -d postgres
Assert-CommandSucceeded "Starting PostgreSQL"

docker compose build migrations
Assert-CommandSucceeded "Building the database migrator"

docker compose run --rm migrations
Assert-CommandSucceeded "Applying database migrations"

dotnet run --project HelpDesk-System.csproj
Assert-CommandSucceeded "Starting the WPF application"
