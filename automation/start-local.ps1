# Starts backend and frontend dev servers in separate PowerShell windows.
param(
  [string]$BackendDir = (Join-Path $PSScriptRoot "..\server\src\Jullius.ServiceApi"),
  [string]$FrontendDir = (Join-Path $PSScriptRoot "..\client"),
  [switch]$Restore
)

function Ensure-Tool {
  param([string]$Name)
  if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
    Write-Error "Required tool '$Name' not found in PATH."
    exit 1
  }
}

$psExe = (Get-Command pwsh -ErrorAction SilentlyContinue)?.Source
if (-not $psExe) { $psExe = (Get-Command powershell -ErrorAction SilentlyContinue)?.Source }
if (-not $psExe) { Write-Error "No PowerShell executable found."; exit 1 }

Ensure-Tool dotnet
Ensure-Tool npm
Ensure-Tool ssh

if ($Restore) {
  Push-Location $FrontendDir
  npm install
  Pop-Location

  Push-Location $BackendDir
  dotnet restore
  Pop-Location
}

$backendCmd = "cd \`"$BackendDir\`"; dotnet run --profile http"
$frontendCmd = "cd `"$FrontendDir`"; npm start"

Start-Process -FilePath $psExe -ArgumentList "-NoExit", "-Command", $backendCmd -WorkingDirectory $BackendDir
Start-Process -FilePath $psExe -ArgumentList "-NoExit", "-Command", $frontendCmd -WorkingDirectory $FrontendDir

Write-Host "NOTE: If connecting to a remote DB, ensure your SSH tunnel is active." -ForegroundColor Yellow
Write-Host "Backend: dotnet run --profile http (window opened in $BackendDir)"
Write-Host "Frontend: npm start (Angular dev server) (window opened in $FrontendDir)"
