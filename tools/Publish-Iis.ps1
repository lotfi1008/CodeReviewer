param([string]$OutputRoot = (Join-Path $PSScriptRoot '..\artifacts'))
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\src\CodeReviewer.Web\CodeReviewer.Web.csproj'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$destination = Join-Path $OutputRoot "CodeReviewer-$stamp"
if (Test-Path -LiteralPath $destination) { throw 'Output already exists.' }
New-Item -ItemType Directory -Path $destination | Out-Null
# Isolated compilation does not replace the binaries of a running review.
dotnet publish $project -c Release --artifacts-path (Join-Path $OutputRoot "build-$stamp") -o $destination --self-contained false
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
if (Test-Path (Join-Path $destination 'appsettings.Development.json')) { throw 'Development settings must not be published.' }
if (!(Test-Path (Join-Path $destination 'web.config'))) { throw 'IIS web.config was not generated.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot '..\docs\deployment.md') -Destination $destination
$zip = "$destination.zip"
Compress-Archive -Path (Join-Path $destination '*') -DestinationPath $zip
Get-FileHash -LiteralPath $zip -Algorithm SHA256 | Format-List
Write-Output "IIS package: $zip"
