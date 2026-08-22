@echo off
setlocal

set "PROJECT_ROOT=%~dp0.."
set "PROJECT_FILE=%PROJECT_ROOT%\HelpDesk-System.csproj"
set "APP_DIRECTORY=%PROJECT_ROOT%\bin\Debug\net8.0-windows"
set "APP_EXECUTABLE=%APP_DIRECTORY%\HelpDesk-System.exe"

if /I "%~1"=="additional" goto additional

pushd "%PROJECT_ROOT%"

docker compose up -d postgres
if errorlevel 1 goto startup_failed

docker compose build migrations
if errorlevel 1 goto startup_failed

docker compose run --rm migrations
if errorlevel 1 goto startup_failed

dotnet run --project "%PROJECT_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"
popd
exit /b %EXIT_CODE%

:additional
if not exist "%APP_EXECUTABLE%" (
    echo Start the application normally before opening an additional session.
    exit /b 1
)

start "" /D "%APP_DIRECTORY%" "%APP_EXECUTABLE%"
if errorlevel 1 (
    echo Failed to start an additional HelpDesk session.
    exit /b 1
)

echo Started an additional HelpDesk session.
exit /b 0

:startup_failed
set "EXIT_CODE=%ERRORLEVEL%"
popd
echo HelpDesk local startup failed with exit code %EXIT_CODE%.
exit /b %EXIT_CODE%
