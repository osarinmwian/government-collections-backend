@echo off
echo Building KeyLoyalty API...

REM Set environment variables to suppress NuGet warnings
set NUGET_XMLDOC_MODE=skip
set DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
set DOTNET_CLI_TELEMETRY_OPTOUT=1

REM Clean and build
cd src\KeyLoyalty.API
dotnet clean --nologo --verbosity quiet
dotnet build --nologo --verbosity minimal --no-restore

echo Build completed.