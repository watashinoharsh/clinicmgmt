<#
.SYNOPSIS  Export a solution from the DEV environment and unpack it into source control.
.EXAMPLE   .\export-solution.ps1 -Solution EduCore -EnvironmentUrl https://<your-dev>.crm8.dynamics.com
.NOTES     Always export unmanaged from DEV. Review the diff (git diff --stat) before committing.
#>
param(
    [Parameter(Mandatory)][ValidateSet('EduCore', 'EduFlows', 'EduApps')][string]$Solution,
    [Parameter(Mandatory)][string]$EnvironmentUrl
)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$zip = Join-Path $env:TEMP "edu-export-$Solution.zip"

pac solution export --name $Solution --path $zip --environment $EnvironmentUrl --overwrite --managed false
if ($LASTEXITCODE -ne 0) { throw 'Export failed.' }

pac solution unpack --zipfile $zip --folder "$root\solutions\$Solution\src" --packagetype Both --allowDelete true --allowWrite true
if ($LASTEXITCODE -ne 0) { throw 'Unpack failed.' }

Write-Host "Unpacked $Solution into solutions\$Solution\src. Review with: git diff --stat"
