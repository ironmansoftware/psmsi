# Example: Installer with Add/Remove Programs Icon
# This script creates an MSI installer with a custom icon for Add/Remove Programs.

# Create a sample license.txt file for demonstration
$licensePath = Join-Path $PSScriptRoot 'license.txt'
if (!(Test-Path $licensePath)) {
    Set-Content -Path $licensePath -Value "This is a sample license file for My First Product."
}

# Create a sample icon.ico file for demonstration
$iconPath = Join-Path $PSScriptRoot 'icon.ico'
if (!(Test-Path $iconPath)) {
    # Create a blank icon file (not a valid icon, but placeholder for demo)
    Set-Content -Path $iconPath -Value $null
}

New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source $licensePath
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output") -AddRemoveProgramsIcon $iconPath
