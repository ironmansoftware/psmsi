# Example: Installer with Shortcut and Working Directory
# This script creates an MSI installer with a shortcut on the Desktop that sets the working directory.

# Create a sample image file for demonstration
$imagePath = Join-Path $PSScriptRoot 'services.png'
if (!(Test-Path $imagePath)) {
    # Create a blank PNG file (not a valid image, but placeholder for demo)
    Set-Content -Path $imagePath -Value $null
}

New-Installer -ProductName "MyImage" -UpgradeCode (New-Guid) -Version 1.0.0 -Content {
    New-InstallerDirectory -PredefinedDirectoryName ProgramFilesFolder -Content {
        New-InstallerDirectory -DirectoryName 'MyDir' -Id 'MyDir' -Content {
            New-InstallerFile -Id 'Image' -Source $imagePath
        }
    }
    New-InstallerDirectory -PredefinedDirectoryName DesktopFolder -Content {
        New-InstallerShortcut -Name 'Test' -FileId 'Image' -WorkingDirectoryId 'MyDir'
    }    
} -OutputDirectory (Join-Path $PSScriptRoot "installer") -RequiresElevation
