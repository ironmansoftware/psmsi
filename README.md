# Installers

PSMSI includes cmdlets for creating MSI packages that can contain any file and directory structure you wish. It also includes functionality to customize the installer interface by including custom EULAs and images.

## Getting Started

To get started creating installers with PSMSI, you will need to download the latest version of the PSMSI module. This can be installed with `Install-Module`.

```powershell
Install-Module PSMSI
```

## WiX Toolset

This module is based on v3 of the [Wix Toolset](https://wixtoolset.org/docs/v3/). There is so much more we could accomplish with this module. It mainly creates WiX XML and runs the WiX tools to generate MSIs. We're very open to PRs and issues. Feel free to check out the WiX documentation for features that could be added. 

## Creating your first installer

The `New-Installer` cmdlet is used to generate an installer. It can contain directories and files for installation. The first step is to define the basic parameters of your installer.

The Product and UpgradeCode parameters are required. The UpgradeCode is a GUID that needs to remain the same for each version of your product and should be unique from other products.

```powershell
New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82'
```

Within the Content parameter of the New-Installer cmdlet, you need to include a root directory for installation on the end user's machine. The root directory needs to be a predefined directory. One of the parameter sets on New-InstallerDirectory defines a PredefinedDirectory parameter that you can use to select the target root directory.

```powershell
New-InstallerDirectory -PredefinedDirectory "LocalAppDataFolder"
```

You can now optionally specify a nested directory within your root directory. This will be created if it does not exist and removed on uninstall.

```powershell
New-InstallerDirectory -DirectoryName "My First Product"
```

Finally, you can include files within your directory. The New-InstallerFile cmdlet accepts a Source parameter with the path to the file you would like to install.

```powershell
New-InstallerFile -Source .\MyTextFile.txt
```

The full script for this installer looks like this.

```powershell
New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "LocalAppDataFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source .\license.txt
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output")
```

Running the above script will produce a WXS, WXSOBJ and MSI file in the output directory. The MSI is the only file that you need to provide to your end users. The WXS and WXSOBJ files are artifacts of the Windows Installer XML Toolkit used to generate these installers.

## Installers

### All Users Installation

You can use the `-RequiresElevation` parameter of `New-Installer` to change from the default `PerUser` installation to a `PerMachine` installation.

The following creates an installer that will install to the program files folder.&#x20;

```powershell
New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source .\license.txt
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output") -RequiresElevation
```

### Add\Remove Programs Icon

The Application Icon that will be displayed within Add\Remove Programs can be defined using the `AddRemoveProgramsIcon` of `New-Installer`.&#x20;

```powershell
New-Installer -Product "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source .\license.txt
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output") -AddRemoveProgramsIcon "icon.ico"
```

### Upgrade Code

The `UpgradeCode` value should be static to ensure that upgrades work successfully. Define the upgrade code by on `New-Installer`.

```powershell
New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source .\license.txt
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output") 
```

### Version

The installer version is set using the `Version` parameter of `New-Installer`. You can provide upgrades by increasing the version and keeping the Upgrade Code the same.&#x20;

The version defaults to 1.0.&#x20;

```powershell
New-Installer -ProductName "My First Product" -UpgradeCode '1a73a1be-50e6-4e92-af03-586f4a9d9e82' -Content {
    New-InstallerDirectory -PredefinedDirectory "ProgramFilesFolder"  -Content {
       New-InstallerDirectory -DirectoryName "My First Product" -Content {
          New-InstallerFile -Source .\license.txt
       }
    }
 } -OutputDirectory (Join-Path $PSScriptRoot "output") -Version 2.0
```

## Custom Actions

Custom actions allow you to run PowerShell scripts during install and uninstall. You will need to include the script as a file in your installer. Use the `FileId` parameter of `New-InstallerCustomAction` to reference the PS1 file you wish to execute.&#x20;

For example, you may have a script named `MyCustomAction.ps1` with an ID of `CustomAction`.&#x20;

```powershell
New-InstallerFile -Source .\myCustomAction.ps1 -Id 'CustomAction'
```

You can then use that script as a custom action during an installation.&#x20;

```powershell
New-InstallerCustomAction -FileId 'CustomAction' -RunOnInstall
```

### Arguments

You can pass arguments to both PowerShell.exe and your script. The `Arguments` parameter passes custom arguments to PowerShell.exe (like -NoProfile). The `ScriptArguments` parameter defines arguments to pass to the script itself.&#x20;

### CheckReturnValue

This checks the exit code of PowerShell.exe. If the exit code is non-zero, then it will cause the install to fail.&#x20;

### RunOnInstall

Runs the custom action during install.&#x20;

### RunOnUninstall

Runs the custom action during uninstall.

## Directories and Files

You can create directories and files using `New-InstallerDirectory` and `New-InstallerFile`. Directories should start with one of the pre-defined directories provided by MSI.&#x20;

### Pre-defined Directories

Use the `PredefinedDirectory` parameter of `New-InstallerDirectory` to define the root folder for the installation. You can use directories such as `Program Files`, `AppData` and `CommonAppData`.&#x20;

### Custom Folders

Custom folders appear within pre-defined directories. You can nest folders to create a folder tree. Folders can then contain files. Use the `DirectoryName` parameter of `New-InstallerDirectory` to create a directory. Use `Content` to specify either folders or files to include.&#x20;

Including the `Configurable` property on `New-InstallerDirectory` will allow the end user to select a directory during installation.&#x20;

```powershell
New-InstallerDirectory -DirectoryName "My First Product" -Content {
    New-InstallerFile -Source .\license.txt
} -Configurable
```

### Files

Files are defined by their current location and an ID. The `Source` parameter should identify the file you wish to include. It's location in the `New-InstallerDirectory` tree will define where it is installed on disk.&#x20;

```powershell
New-InstallerDirectory -DirectoryName "My First Product" -Content {
    New-InstallerFile -Source .\license.txt
}
```

## Shortcuts

Shortcuts can be defined for installers using `New-InstallerShortcut`. You will define where the shortcut is located using `New-InstallerDirectory` and reference a file by Id.&#x20;

For example, to define a file by ID, you would include the `Id` parameter of `New-InstallerFile`.&#x20;

```powershell
New-InstallerFile -Source .\MyTextFile.txt -Id "myTestFile"
```

Next, you would define the shortcut in a directory and reference the file by ID.

```powershell
New-InstallerDirectory -PredefinedDirectory "DesktopFolder" -Content {
    New-InstallerShortcut -Name "My Test File" -FileId "myTestFile"
}
```

### Working Directory

You can set the working directory of a shortcut by specifying the ID of the folder. The below example sets the working directory to the installation directory's ID.&#x20;

```powershell
New-Installer -ProductName "MyImage" -UpgradeCode (New-Guid) -Version 1.0.0 -Content {
    New-InstallerDirectory -PredefinedDirectoryName ProgramFilesFolder -Content {
        New-InstallerDirectory -DirectoryName 'MyDir' -Id 'MyDir' -Content {
            New-InstallerFile -Id 'Image' -Source 'services.png'
        }
    }
    New-InstallerDirectory -PredefinedDirectoryName DesktopFolder -Content {
        New-InstallerShortcut -Name 'Test' -FileId 'Image' -WorkingDirectoryId 'MyDir'
    }    
} -OutputDirectory .\installer -RequiresElevation
```

## User Interfaces

You can customize the user interface of the installer by using the `UserInterface` parameter of `New-Installer` along with `New-InstallerUserInterface`.&#x20;

User interfaces can include custom graphics and EULAs for your installer.&#x20;

```powershell
 $UserInterface = New-InstallerUserInterface -Eula (Join-Path $PSScriptRoot 'eula.rtf') -TopBanner (Join-Path $PSScriptRoot "banner.png") -Welcome (Join-Path $PSScriptRoot "welcome.png")
```

### Ironman Software Free Tools

For more free tools, visit the [Ironman Software free tools index](https://ironmansoftware.com/free-powershell-tools). 
