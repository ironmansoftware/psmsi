# Example: Installer with Configurable Directory
# This script creates an MSI installer with a configurable directory for installation.

# Create a sample license.txt file for demonstration
$licensePath = Join-Path $PSScriptRoot 'license.txt'
if (!(Test-Path $licensePath)) {
    Set-Content -Path $licensePath -Value "This is a sample license file for My First Product."
}

New-InstallerDirectory -DirectoryName "My First Product" -Content {
    New-InstallerFile -Source $licensePath
} -Configurable
