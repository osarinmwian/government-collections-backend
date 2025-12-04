@echo off
echo Stopping KeyLoyalty.API process...
taskkill /F /IM KeyLoyalty.API.exe 2>nul
timeout /t 2 /nobreak >nul

echo Building project...
cd src\KeyLoyalty.API
dotnet build --nologo --verbosity quiet

echo Done.