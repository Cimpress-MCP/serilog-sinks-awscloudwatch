$ErrorActionPreference = "Stop"

function Pack-Project
{
    param([string] $DirectoryName, [string]$revision)

    Push-Location $DirectoryName
    & dotnet pack -c Release -o ..\..\.\artifacts --version-suffix $revision
    if($LASTEXITCODE -ne 0) { exit 1 }
    Pop-Location
}

function Set-BuildVersion
{
    param([string] $DirectoryName, [string]$revision)
	
    $projectJson = Join-Path $DirectoryName "project.json"
    $jsonData = Get-Content -Path $projectJson -Raw | ConvertFrom-JSON
	$jsonData.version = $revision
    $jsonData | ConvertTo-Json -Depth 999 | Out-File $projectJson
    Write-Host "Set version of $projectJson to $revision"
}

function Test-Project
{
    param([string] $DirectoryName)

    Push-Location $DirectoryName
    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 2 }
    Pop-Location
}

$revision = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "0.0.1" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];

Push-Location $PSScriptRoot

# Clean
if(Test-Path .\artifacts) { Remove-Item .\artifacts -Force -Recurse }

# Modify project.json
Get-ChildItem -Path .\src -Filter *.xproj -Recurse | ForEach-Object { Set-BuildVersion $_.DirectoryName $revision }

# Package restore
& dotnet restore
if($LASTEXITCODE -ne 0) { exit 1 }

# Build/package
Get-ChildItem -Path .\src -Filter *.xproj -Recurse | ForEach-Object { Pack-Project $_.DirectoryName $revision }

# Test
Get-ChildItem -Path .\test -Filter *.xproj -Recurse | ForEach-Object { Test-Project $_.DirectoryName }

Pop-Location
