# Example: Installer with selectable features
# This script creates an MSI where the user can choose optional content.

$appPath = Join-Path $PSScriptRoot 'feature-app.txt'
if (!(Test-Path $appPath)) {
    Set-Content -Path $appPath -Value "Core application file."
}

$samplePath = Join-Path $PSScriptRoot 'feature-sample.txt'
if (!(Test-Path $samplePath)) {
    Set-Content -Path $samplePath -Value "Optional sample file."
}

$UserInterface = New-InstallerUserInterface -DialogSet FeatureTree

New-Installer -ProductName "Feature Demo" -UpgradeCode (New-Guid) -Version 1.0.0 -Content {
    New-InstallerDirectory -PredefinedDirectoryName "ProgramFilesFolder" -Content {
        New-InstallerDirectory -DirectoryName "Feature Demo" -Id "INSTALLFOLDER" -Configurable -Content {
            New-InstallerFeature -Id "Core" -Title "Core files" -Description "Required files for Feature Demo." -Required -Content {
                New-InstallerFile -Source $appPath -Id "FeatureDemoApp"
            }

            New-InstallerFeature -Id "Samples" -Title "Sample files" -Description "Optional sample content." -DefaultState Absent -Content {
                New-InstallerDirectory -DirectoryName "Samples" -Content {
                    New-InstallerFile -Source $samplePath
                }
            }
        }
    }
} -UserInterface $UserInterface -OutputDirectory (Join-Path $PSScriptRoot "output") -RequiresElevation
