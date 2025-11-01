# Example: Installer with Custom User Interface
# This script creates an MSI installer with a custom EULA and graphics.

# Create sample EULA and images for demonstration
$eulaPath = Join-Path $PSScriptRoot 'eula.rtf'
if (!(Test-Path $eulaPath)) {
    Set-Content -Path $eulaPath -Value "{\rtf1\ansi\deff0 {\fonttbl {\f0 Arial;}}\f0\fs20 This is a sample EULA.}"
}
$bannerPath = Join-Path $PSScriptRoot 'banner.png'
if (!(Test-Path $bannerPath)) {
    Set-Content -Path $bannerPath -Value $null
}
$welcomePath = Join-Path $PSScriptRoot 'welcome.png'
if (!(Test-Path $welcomePath)) {
    Set-Content -Path $welcomePath -Value $null
}

$UserInterface = New-InstallerUserInterface -Eula $eulaPath -TopBanner $bannerPath -Welcome $welcomePath

# Create a sample license.txt file for demonstration
$licensePath = Join-Path $PSScriptRoot 'license.txt'
if (!(Test-Path $licensePath)) {
    Set-Content -Path $licensePath -Value "This is a sample license file for My First Product."
}

New-Installer -ProductName "My First Product" -UpgradeCode (New-Guid) -Version 1.0.0 -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder" -Content {
        New-InstallerDirectory -DirectoryName "My First Product" -Content {
            New-InstallerFile -Source $licensePath
        }
    }
} -UserInterface $UserInterface -OutputDirectory (Join-Path $PSScriptRoot "output")
