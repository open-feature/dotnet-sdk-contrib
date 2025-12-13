<#
Runs the tests for this project using dotnet.

Usage:
	./run-tests.ps1 -- [additional dotnet test args]

Examples:
	./run-tests.ps1
	./run-tests.ps1 --verbosity normal

This script works on Windows PowerShell and PowerShell Core (pwsh) on Linux/Ubuntu.
#>

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]
    $ExtraArgs
)

if ($PSScriptRoot) { Set-Location $PSScriptRoot }

$dotnetArgs = @('test', '--filter', 'Category=in-process', '--framework', 'net8.0')
if ($ExtraArgs) { $dotnetArgs += $ExtraArgs }

Write-Host "Running: dotnet $($dotnetArgs -join ' ')" -ForegroundColor Cyan

& dotnet @dotnetArgs
$exitCode = $LASTEXITCODE
exit $exitCode