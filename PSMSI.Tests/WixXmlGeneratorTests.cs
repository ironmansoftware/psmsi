using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PSMSI.Models;
using PSMSI.Xml;
using Xunit;
using WixDirectory = PSMSI.Models.Directory;
using WixFile = PSMSI.Models.File;

namespace PSMSI.Tests;

public class WixXmlGeneratorTests
{
    private static readonly XNamespace Wix = "http://schemas.microsoft.com/wix/2006/wi";

    [Fact]
    public void Generate_CreatesProductPackageAndFileComponent()
    {
        var installer = CreateInstaller(
            requiresElevation: true,
            platform: "x64",
            content: new object[]
            {
                new WixDirectory
                {
                    Id = "INSTALLFOLDER",
                    Name = "Demo",
                    Content = new object[]
                    {
                        new WixFile
                        {
                            Id = "MainExe",
                            Source = @"C:\Source\demo.exe"
                        }
                    }
                }
            });

        var document = new WixXmlGenerator().Generate(installer);

        var product = RequiredElement(document.Root, "Product");
        Assert.Equal("*", (string)product.Attribute("Id"));
        Assert.Equal("Demo Product", (string)product.Attribute("Name"));
        Assert.Equal("1.2.3", (string)product.Attribute("Version"));
        Assert.Equal("PSMSI Tests", (string)product.Attribute("Manufacturer"));

        var package = RequiredElement(product, "Package");
        Assert.Equal("perMachine", (string)package.Attribute("InstallScope"));
        Assert.Equal("elevated", (string)package.Attribute("InstallPrivileges"));
        Assert.Equal("x64", (string)package.Attribute("Platform"));

        var directory = RequiredElement(product, "Directory", element => (string)element.Attribute("Id") == "INSTALLFOLDER");
        Assert.Equal("Demo", (string)directory.Attribute("Name"));

        var file = RequiredElement(product, "File");
        Assert.Equal("MainExe", (string)file.Attribute("Id"));
        Assert.Equal(@"C:\Source\demo.exe", (string)file.Attribute("Source"));
        Assert.Equal("yes", (string)file.Attribute("KeyPath"));

        var component = file.Parent;
        var componentRef = RequiredElement(product, "ComponentRef");
        Assert.Equal((string)component.Attribute("Id"), (string)componentRef.Attribute("Id"));
    }

    [Fact]
    public void Generate_AssignsComponentsToExplicitFeatures()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new Feature
                {
                    Id = "ToolsFeature",
                    Title = "Command Line Tools",
                    Description = "Installs command line tools.",
                    DefaultState = "Absent",
                    Display = "Expand",
                    Required = true,
                    Content = new object[]
                    {
                        new WixFile
                        {
                            Id = "ToolExe",
                            Source = @"C:\Source\tool.exe"
                        }
                    }
                }
            });

        var document = new WixXmlGenerator().Generate(installer);

        var feature = RequiredElement(
            document.Root,
            "Feature",
            element => (string)element.Attribute("Id") == "ToolsFeature");

        Assert.Equal("Command Line Tools", (string)feature.Attribute("Title"));
        Assert.Equal("Installs command line tools.", (string)feature.Attribute("Description"));
        Assert.Equal("1000", (string)feature.Attribute("Level"));
        Assert.Equal("expand", (string)feature.Attribute("Display"));
        Assert.Equal("disallow", (string)feature.Attribute("Absent"));

        var file = RequiredElement(document.Root, "File", element => (string)element.Attribute("Id") == "ToolExe");
        var componentRef = RequiredElement(feature, "ComponentRef");
        Assert.Equal((string)file.Parent.Attribute("Id"), (string)componentRef.Attribute("Id"));
    }

    [Fact]
    public void Generate_UsesInstallDirUiForConfigurableDirectory()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new WixDirectory
                {
                    Id = "INSTALLFOLDER",
                    Name = "Demo",
                    Configurable = true
                }
            },
            userInterface: new UserInterface());

        var document = new WixXmlGenerator().Generate(installer);
        var product = RequiredElement(document.Root, "Product");

        var installDirProperty = RequiredElement(
            product,
            "Property",
            element => (string)element.Attribute("Id") == "WIXUI_INSTALLDIR");
        Assert.Equal("INSTALLFOLDER", (string)installDirProperty.Attribute("Value"));

        var uiRef = RequiredElement(product, "UIRef");
        Assert.Equal("WixUI_InstallDir", (string)uiRef.Attribute("Id"));
    }

    [Fact]
    public void Generate_UsesFeatureTreeUiWhenExplicitFeaturesArePresent()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new Feature
                {
                    Id = "ToolsFeature",
                    Content = Array.Empty<object>()
                }
            });

        var document = new WixXmlGenerator().Generate(installer);

        var uiRef = RequiredElement(document.Root, "UIRef");
        Assert.Equal("WixUI_FeatureTree", (string)uiRef.Attribute("Id"));
    }

    [Fact]
    public void Generate_AddsInstallAndUninstallCustomActions()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new WixFile
                {
                    Id = "SetupScript",
                    Source = @"C:\Source\setup.ps1"
                }
            },
            customActions: new[]
            {
                new CustomAction
                {
                    FileId = "SetupScript",
                    RunOnInstall = true,
                    RunOnUninstall = true,
                    CheckReturnValue = true,
                    ScriptArguments = "-Mode Silent"
                }
            });

        var document = new WixXmlGenerator().Generate(installer);

        var customActions = document.Root.Descendants(Wix + "CustomAction").ToArray();
        Assert.Equal(2, customActions.Length);
        Assert.All(customActions, action =>
        {
            Assert.Equal("POWERSHELL", (string)action.Attribute("Property"));
            Assert.Equal("check", (string)action.Attribute("Return"));
            Assert.Equal("deferred", (string)action.Attribute("Execute"));
            Assert.Contains("-NoProfile -WindowStyle Hidden -NonInteractive", (string)action.Attribute("ExeCommand"));
            Assert.Contains("-File \"[#SetupScript]\" -Mode Silent", (string)action.Attribute("ExeCommand"));
        });

        var sequenceActions = document.Root.Descendants(Wix + "InstallExecuteSequence")
            .Elements(Wix + "Custom")
            .Select(element => (string)element.Attribute("Action"))
            .ToArray();
        Assert.All(customActions.Select(action => action.Attribute("Id").Value), actionId => Assert.Contains(actionId, sequenceActions));
    }

    [Fact]
    public void Generate_ThrowsWhenDuplicateFeatureIdsAreDefined()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new Feature { Id = "ToolsFeature" },
                new Feature { Id = "ToolsFeature" }
            });

        var exception = Assert.Throws<Exception>(() => new WixXmlGenerator().Generate(installer));
        Assert.Contains("A feature with the ID 'ToolsFeature' already exists", exception.Message);
    }

    [Fact]
    public void Generate_ThrowsWhenContentReferencesMissingFeature()
    {
        var installer = CreateInstaller(
            content: new object[]
            {
                new WixFile
                {
                    Id = "MainExe",
                    Source = @"C:\Source\demo.exe",
                    FeatureId = "MissingFeature"
                }
            });

        var exception = Assert.Throws<Exception>(() => new WixXmlGenerator().Generate(installer));
        Assert.Equal("Feature 'MissingFeature' has not been defined. Add a New-InstallerFeature call with that ID.", exception.Message);
    }

    private static Installer CreateInstaller(
        object[] content,
        bool requiresElevation = false,
        string platform = "x86",
        UserInterface userInterface = null,
        CustomAction[] customActions = null)
    {
        return new Installer
        {
            ProductName = "Demo Product",
            Description = "A demo installer.",
            Version = new Version(1, 2, 3),
            Manufacturer = "PSMSI Tests",
            UpgradeCode = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Platform = platform,
            OutputDirectory = new DirectoryInfo(Path.GetTempPath()),
            RequiresElevation = requiresElevation,
            Content = content,
            UserInterface = userInterface,
            CustomActions = customActions,
            ProductId = "*"
        };
    }

    private static XElement RequiredElement(XContainer container, string name)
    {
        return Assert.Single(container.Descendants(Wix + name));
    }

    private static XElement RequiredElement(XContainer container, string name, Func<XElement, bool> predicate)
    {
        var matches = container.Descendants(Wix + name).Where(predicate).ToArray();
        Assert.Single(matches);
        return matches[0];
    }
}
