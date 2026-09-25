<#
.SYNOPSIS  Local build: run unit tests, build the plug-in package, pack all solutions (managed and unmanaged) into .\out.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
Set-Location $root

dotnet test tests\EduCore.Plugins.Tests\EduCore.Plugins.Tests.csproj --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Unit tests failed.' }

dotnet build src\EduCore.Plugins\EduCore.Plugins.csproj --configuration Release --nologo
if ($LASTEXITCODE -ne 0) { throw 'Plug-in build failed.' }

New-Item -ItemType Directory -Force out | Out-Null
foreach ($name in 'EduCore', 'EduFlows', 'EduApps') {
    foreach ($type in 'Unmanaged', 'Managed') {
        $zip = "out\${name}_$($type.ToLower()).zip"
        pac solution pack --zipfile $zip --folder "solutions\$name\src" --packagetype $type
        if ($LASTEXITCODE -ne 0) { throw "Pack failed: $name $type" }
    }
}
Write-Host 'Build complete. Packages are in .\out'
