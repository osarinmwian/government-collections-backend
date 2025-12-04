# PowerShell script to update vulnerable packages
# Run this from the solution directory

Write-Host "Updating vulnerable packages..." -ForegroundColor Green

# Update Oracle.ManagedDataAccess
Update-Package Oracle.ManagedDataAccess -Reinstall -ProjectName OmniChannel.Gateway

# Update System.Text.Json
Update-Package System.Text.Json -Reinstall -ProjectName OmniChannel.Gateway

# Update Microsoft.Bcl.AsyncInterfaces if needed
Update-Package Microsoft.Bcl.AsyncInterfaces -Reinstall -ProjectName OmniChannel.Gateway

# Update System.Text.Encodings.Web if needed
Update-Package System.Text.Encodings.Web -Reinstall -ProjectName OmniChannel.Gateway

Write-Host "Package updates completed. Please rebuild the solution." -ForegroundColor Green