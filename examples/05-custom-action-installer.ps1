# Example: Installer with Custom Action
# This script creates an MSI installer that runs a PowerShell script during installation.

# Create a sample custom action script
$customActionPath = Join-Path $PSScriptRoot 'myCustomAction.ps1'
if (!(Test-Path $customActionPath)) {
    Set-Content -Path $customActionPath -Value "Write-Host 'Custom action executed during install.'"
}

New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source $customActionPath -Id 'CustomAction'
       }
    }
 } -CustomAction @(
    New-InstallerCustomAction -FileId 'CustomAction' -RunOnInstall
 ) -OutputDirectory (Join-Path $PSScriptRoot "output")
