# Example: Installer with Shortcut
# This script creates an MSI installer with a shortcut on the Desktop to a file.

# Create a sample file for shortcut
$testFilePath = Join-Path $PSScriptRoot 'MyTextFile.txt'
if (!(Test-Path $testFilePath)) {
    Set-Content -Path $testFilePath -Value "This is a test file for shortcut."
}

New-Installer -ProductName "My First Product" -UpgradeCode (New-Guid) -Version 1.0.0 -Content {
    New-InstallerFile -Source $testFilePath -Id "myTestFile"
    New-InstallerDirectory -PredefinedDirectory "DesktopFolder" -Content {
        New-InstallerShortcut -Name "My Test File" -FileId "myTestFile"
    }
} -OutputDirectory (Join-Path $PSScriptRoot "output")
