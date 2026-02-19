Set-Location $PSScriptRoot

$projects = @(
    "..\src\NativeTray"
)

foreach ($proj in $projects) {
    Push-Location $proj
    Write-Host "Processing $proj..."
    dotnet restore
    dotnet build -c Release
    dotnet pack -c Release -o ../../build/
    Pop-Location
}

Pause
