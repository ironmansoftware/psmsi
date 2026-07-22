@{
    RootModule      = 'PSMSI.dll'
    ModuleVersion   = '1.0.0'
    GUID            = '7de0af83-c438-4252-9f5d-b7d4409b152e'
    Author          = 'Adam Driscoll'
    CompanyName     = 'Ironman Software'
    Copyright       = '(c) Ironman Software. All rights reserved.'
    Description     = 'Create MSIs with PowerShell.'
    CmdletsToExport = @(
        'New-Installer', 
        'New-InstallerFile', 
        'New-InstallerShortcut', 
        'New-InstallerDirectory', 
        'New-InstallerUserInterface', 
        'New-InstallerCustomAction'
    )
    PrivateData     = @{
        PSData = @{
            Tags       = @('MSI', "installer")
            LicenseUri = 'https://www.github.com/ironmansoftware/psmsi/LICENSE'
            ProjectUri = 'https://www.github.com/ironmansoftware/psmsi'
        }
    }
}