@echo off
cd src\KeyLoyalty.API
dotnet publish -c Release -o ..\..\..\KeyLoyaltyService --self-contained -r win-x64
sc create "KeyLoyalty Service" binPath="%CD%\..\..\..\KeyLoyaltyService\KeyLoyalty.API.exe" start=auto
sc start "KeyLoyalty Service"
echo Service installed and started