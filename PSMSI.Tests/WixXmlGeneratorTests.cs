using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PSMSI.Models;
using PSMSI.Xml;
using Directory = PSMSI.Models.Directory;
using File = PSMSI.Models.File;

namespace PSMSI.Tests;

public class WixXmlGeneratorTests
{
    private static readonly XNamespace Wix = "http://schemas.microsoft.com/wix/2006/wi";

    [Fact]
    public void Generate_UsesInstallerMetadataForProductAndPackage()
    {
        var upgradeCode = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var installer = CreateInstaller(
            productName: "Demo Installer",
            content: new object[] { new File { Id = "appExe", Source = @"C:\demo\app.exe" } });

        installer.Description = "Installs the demo app.";
        installer.Version = new Version(2, 3, 4);
        installer.Manufacturer = "Demo Co";
        installer.UpgradeCode = upgradeCode;
        installer.ProductId = "22222222-2222-2222-2222-222222222222";
        installer.Platform = "x64";
        installer.RequiresElevation = true;
        installer.HelpLink = "https://example.test/help";
        installer.AboutLink = "https://example.test/about";

        var document = new WixXmlGenerator().Generate(installer);

        var product = Required(document.Root!.Element(Wix + "Product"), "Product");
        Assert.Equal(installer.ProductId, product.Attribute("Id")?.Value);
        Assert.Equal("Demo Installer", product.Attribute("Name")?.Value);
        Assert.Equal("2.3.4", product.Attribute("Version")?.Value);
        Assert.Equal("Demo Co", product.Attribute("Manufacturer")?.Value);
        Assert.Equal(upgradeCode.ToString(), product.Attribute("UpgradeCode")?.Value);

        var package = Required(product.Element(Wix + "Package"), "Package");
        Assert.Equal("perMachine", package.Attribute("InstallScope")?.Value);
        Assert.Equal("elevated", package.Attribute("InstallPrivileges")?.Value);
        Assert.Equal("Installs the demo app.", package.Attribute("Description")?.Value);
        Assert.Equal("x64", package.Attribute("Platform")?.Value);

        Assert.Contains(product.Elements(Wix + "Property"),
            property => property.Attribute("Id")?.Value == "ARPHELPLINK" &&
                        property.Attribute("Value")?.Value == installer.HelpLink);
        Assert.Contains(product.Elements(Wix + "Property"),
            property => property.Attribute("Id")?.Value == "ARPURLINFOABOUT" &&
                        property.Attribute("Value")?.Value == installer.AboutLink);
    }

    [Fact]
    public void Generate_TruncatesFeatureIdToWixLimit()
    {
        var document = new WixXmlGenerator().Generate(CreateInstaller(
            productName: "This Product Name Is Far Longer Than The Wix Feature Identifier Limit",
            content: new object[] { new File { Id = "appExe", Source = @"C:\demo\app.exe" } }));

        var feature = Required(document.Descendants(Wix + "Feature").Single(), "Feature");

        Assert.Equal("THIS_PRODUCT_NAME_IS_FAR_LONGER_THAN_T", feature.Attribute("Id")?.Value);
        Assert.Equal(38, feature.Attribute("Id")?.Value.Length);
    }

    [Fact]
    public void Generate_CreatesDirectoryFileShortcutAndComponentReferences()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new Directory
            {
                Id = "INSTALLFOLDER",
                Name = "Demo",
                Content = new object[]
                {
                    new File { Id = "appExe", Source = @"C:\demo\app.exe" },
                    new Shortcut
                    {
                        FileId = "appExe",
                        Name = "Demo App",
                        Show = "normal",
                        WorkingDirectory = "INSTALLFOLDER",
                        Arguments = "--open"
                    }
                }
            }
        });

        var document = new WixXmlGenerator().Generate(installer);

        var installFolder = Required(document.Descendants(Wix + "Directory")
            .Single(element => element.Attribute("Id")?.Value == "INSTALLFOLDER"), "INSTALLFOLDER");
        Assert.Equal("Demo", installFolder.Attribute("Name")?.Value);

        var file = Required(document.Descendants(Wix + "File")
            .Single(element => element.Attribute("Id")?.Value == "appExe"), "appExe");
        Assert.Equal(@"C:\demo\app.exe", file.Attribute("Source")?.Value);
        Assert.Equal("yes", file.Attribute("KeyPath")?.Value);
        Assert.Equal("yes", file.Attribute("Compressed")?.Value);
        Assert.Equal("yes", file.Attribute("Checksum")?.Value);

        var shortcut = Required(document.Descendants(Wix + "Shortcut")
            .Single(element => element.Attribute("Name")?.Value == "Demo App"), "Demo App shortcut");
        Assert.Equal("[#appExe]", shortcut.Attribute("Target")?.Value);
        Assert.Equal("--open", shortcut.Attribute("Arguments")?.Value);
        Assert.Equal("INSTALLFOLDER", shortcut.Attribute("WorkingDirectory")?.Value);

        var components = document.Descendants(Wix + "Component").ToArray();
        var componentRefs = document.Descendants(Wix + "ComponentRef").ToArray();
        Assert.Equal(2, components.Length);
        Assert.Equal(components.Select(component => component.Attribute("Id")?.Value).OrderBy(id => id),
            componentRefs.Select(componentRef => componentRef.Attribute("Id")?.Value).OrderBy(id => id));
    }

    [Fact]
    public void Generate_UsesPowerShellAsShortcutTargetForScriptFiles()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new File { Id = "scriptFile", Source = @"C:\demo\install.ps1" },
            new Shortcut { FileId = "scriptFile", Name = "Run Script", Show = "minimized", Arguments = "-Verbose" }
        });

        var document = new WixXmlGenerator().Generate(installer);

        var shortcut = Required(document.Descendants(Wix + "Shortcut").Single(), "Shortcut");
        Assert.Equal("[POWERSHELL]", shortcut.Attribute("Target")?.Value);
        Assert.Equal("-File \"[#scriptFile]\" -Verbose", shortcut.Attribute("Arguments")?.Value);
        Assert.Equal("minimized", shortcut.Attribute("Show")?.Value);
    }

    [Fact]
    public void Generate_CreatesInstallAndUninstallCustomActions()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new File { Id = "scriptFile", Source = @"C:\demo\install.ps1" }
        });
        installer.CustomActions = new[]
        {
            new CustomAction
            {
                FileId = "scriptFile",
                RunOnInstall = true,
                RunOnUninstall = true,
                CheckReturnValue = true,
                ScriptArguments = "-Mode Test"
            }
        };

        var actionId = installer.CustomActions[0].Id;
        var document = new WixXmlGenerator().Generate(installer);

        var customActions = document.Descendants(Wix + "CustomAction").ToArray();
        Assert.Contains(customActions, action => action.Attribute("Id")?.Value == actionId + "INSTALL");
        Assert.Contains(customActions, action => action.Attribute("Id")?.Value == actionId + "UNINSTALL");
        Assert.All(customActions, action =>
        {
            Assert.Equal("POWERSHELL", action.Attribute("Property")?.Value);
            Assert.Equal("check", action.Attribute("Return")?.Value);
            Assert.Equal("deferred", action.Attribute("Execute")?.Value);
            Assert.Contains("-NoProfile -WindowStyle Hidden -NonInteractive", action.Attribute("ExeCommand")?.Value);
            Assert.Contains("-File \"[#scriptFile]\" -Mode Test", action.Attribute("ExeCommand")?.Value);
        });

        var sequence = Required(document.Descendants(Wix + "InstallExecuteSequence").Single(), "InstallExecuteSequence");
        Assert.Contains(sequence.Elements(Wix + "Custom"),
            custom => custom.Attribute("Action")?.Value == actionId + "INSTALL" &&
                      custom.Attribute("After")?.Value == "InstallFiles" &&
                      custom.Value == "NOT Installed");
        Assert.Contains(sequence.Elements(Wix + "Custom"),
            custom => custom.Attribute("Action")?.Value == actionId + "UNINSTALL" &&
                      custom.Attribute("Before")?.Value == "RemoveFiles" &&
                      custom.Value == "Installed");
    }

    [Fact]
    public void Generate_AddsInstallDirUiForConfigurableDirectory()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new Directory { Id = "INSTALLFOLDER", Name = "Demo", Configurable = true }
        });
        installer.UserInterface = new UserInterface { ExitDialogText = "Thanks for installing." };

        var document = new WixXmlGenerator().Generate(installer);

        Assert.Contains(document.Descendants(Wix + "Property"),
            property => property.Attribute("Id")?.Value == "WIXUI_INSTALLDIR" &&
                        property.Attribute("Value")?.Value == "INSTALLFOLDER");
        Assert.Contains(document.Descendants(Wix + "Property"),
            property => property.Attribute("Id")?.Value == "WIXUI_EXITDIALOGOPTIONALTEXT" &&
                        property.Attribute("Value")?.Value == "Thanks for installing.");
        Assert.Contains(document.Descendants(Wix + "UIRef"),
            uiRef => uiRef.Attribute("Id")?.Value == "WixUI_InstallDir");
    }

    [Fact]
    public void Generate_DoesNotReuseConfigurableDirectoryBetweenCalls()
    {
        var generator = new WixXmlGenerator();

        generator.Generate(CreateInstaller(content: new object[]
        {
            new Directory { Id = "INSTALLFOLDER", Name = "Demo", Configurable = true }
        }));

        var document = generator.Generate(CreateInstaller(content: Array.Empty<object>()));

        Assert.DoesNotContain(document.Descendants(Wix + "Property"),
            property => property.Attribute("Id")?.Value == "WIXUI_INSTALLDIR");
    }

    [Fact]
    public void Generate_ThrowsWhenMoreThanOneDirectoryIsConfigurable()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new Directory { Id = "FIRST", Name = "First", Configurable = true },
            new Directory { Id = "SECOND", Name = "Second", Configurable = true }
        });

        var exception = Assert.Throws<Exception>(() => new WixXmlGenerator().Generate(installer));
        Assert.Contains("more than one configurable directory", exception.Message);
    }

    private static Installer CreateInstaller(string productName = "Demo Installer", object[]? content = null)
    {
        return new Installer
        {
            ProductName = productName,
            Version = new Version(1, 0),
            Manufacturer = "Demo Co",
            UpgradeCode = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Platform = "x86",
            OutputDirectory = new DirectoryInfo(Path.GetTempPath()),
            Content = content ?? Array.Empty<object>(),
            UserInterface = new UserInterface()
        };
    }

    private static XElement Required(XElement? element, string name)
    {
        return element ?? throw new InvalidOperationException($"{name} was not generated.");
    }
}
