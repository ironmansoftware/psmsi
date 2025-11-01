# Example: Basic Installer
# This script creates an MSI installer for "My First Product" with a license file.

# Create a sample license.txt file for demonstration
$licensePath = Join-Path $PSScriptRoot 'license.txt'
if (!(Test-Path $licensePath)) {
    Set-Content -Path $licensePath -Value "This is a sample license file for My First Product."
}

New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "LocalAppDataFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source $licensePath
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output")
