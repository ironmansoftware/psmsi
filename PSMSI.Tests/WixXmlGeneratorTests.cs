using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using PSMSI.Models;
using PSMSI.Xml;
using Xunit;
using DirectoryModel = PSMSI.Models.Directory;
using FileModel = PSMSI.Models.File;

namespace PSMSI.Tests;

public class WixXmlGeneratorTests
{
    private static readonly XNamespace Wix = "http://schemas.microsoft.com/wix/2006/wi";

    [Fact]
    public void Generate_UsesInstallerMetadataForProductAndPackage()
    {
        var installer = CreateInstaller(requiresElevation: true);

        var document = Generate(installer);

        var product = document.Descendants(Wix + "Product").Single();
        Assert.Equal("*", (string?)product.Attribute("Id"));
        Assert.Equal("Test Product", (string?)product.Attribute("Name"));
        Assert.Equal("1.2.3.4", (string?)product.Attribute("Version"));
        Assert.Equal("Test Manufacturer", (string?)product.Attribute("Manufacturer"));
        Assert.Equal(installer.UpgradeCode.ToString(), (string?)product.Attribute("UpgradeCode"));

        var package = product.Element(Wix + "Package");
        Assert.NotNull(package);
        Assert.Equal("perMachine", (string?)package.Attribute("InstallScope"));
        Assert.Equal("elevated", (string?)package.Attribute("InstallPrivileges"));
        Assert.Equal("Test Description", (string?)package.Attribute("Description"));
        Assert.Equal("x64", (string?)package.Attribute("Platform"));

        Assert.Equal("https://example.test/help", ProductPropertyValue(product, "ARPHELPLINK"));
        Assert.Equal("https://example.test/about", ProductPropertyValue(product, "ARPURLINFOABOUT"));
    }

    [Fact]
    public void Generate_AddsDirectoriesFilesAndFeatureComponentReferences()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new DirectoryModel
            {
                Id = "INSTALLFOLDER",
                Name = "Product",
                Content = new object[]
                {
                    new FileModel { Id = "ReadmeFile", Source = @"C:\input\readme.txt" }
                }
            }
        });

        var document = Generate(installer);

        var installFolder = document.Descendants(Wix + "Directory")
            .Single(x => (string?)x.Attribute("Id") == "INSTALLFOLDER");
        Assert.Equal("Product", (string?)installFolder.Attribute("Name"));

        var file = installFolder.Descendants(Wix + "File").Single();
        Assert.Equal("ReadmeFile", (string?)file.Attribute("Id"));
        Assert.Equal(@"C:\input\readme.txt", (string?)file.Attribute("Source"));
        Assert.Equal("yes", (string?)file.Attribute("KeyPath"));
        Assert.Equal("yes", (string?)file.Attribute("Compressed"));

        var componentId = (string?)file.Parent?.Attribute("Id");
        Assert.NotNull(componentId);
        var componentRef = document.Descendants(Wix + "ComponentRef")
            .Single(x => (string?)x.Attribute("Id") == componentId);
        Assert.NotNull(componentRef);
    }

    [Fact]
    public void Generate_ConvertsPowerShellScriptShortcutToPowerShellTarget()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new DirectoryModel
            {
                Id = "INSTALLFOLDER",
                Name = "Product",
                Content = new object[]
                {
                    new FileModel { Id = "StartScript", Source = @"C:\input\start.ps1" },
                    new Shortcut
                    {
                        FileId = "StartScript",
                        Name = "Launch Product",
                        Arguments = "-Mode Silent",
                        WorkingDirectory = "INSTALLFOLDER",
                        Show = "minimized"
                    }
                }
            }
        });

        var document = Generate(installer);

        var shortcut = document.Descendants(Wix + "Shortcut").Single();
        Assert.Equal("Launch Product", (string?)shortcut.Attribute("Name"));
        Assert.Equal("minimized", (string?)shortcut.Attribute("Show"));
        Assert.Equal("[POWERSHELL]", (string?)shortcut.Attribute("Target"));
        Assert.Equal("-File \"[#StartScript]\" -Mode Silent", (string?)shortcut.Attribute("Arguments"));
        Assert.Equal("INSTALLFOLDER", (string?)shortcut.Attribute("WorkingDirectory"));
    }

    [Fact]
    public void Generate_AddsCustomActionsWithDefaultArgumentsAndInstallUninstallConditions()
    {
        var installAction = new CustomAction
        {
            FileId = "InstallScript",
            RunOnInstall = true,
            CheckReturnValue = true,
            ScriptArguments = "-Name Test"
        };
        var uninstallAction = new CustomAction
        {
            FileId = "UninstallScript",
            RunOnUninstall = true,
            Arguments = "-NoProfile",
            ScriptArguments = "-Remove",
            CheckReturnValue = false
        };
        var installer = CreateInstaller(customActions: new[] { installAction, uninstallAction });

        var document = Generate(installer);

        var installNode = CustomActionElement(document, installAction.Id + "INSTALL");
        Assert.Equal("POWERSHELL", (string?)installNode.Attribute("Property"));
        Assert.Equal("check", (string?)installNode.Attribute("Return"));
        Assert.Contains("-ExecutionPolicy Bypass", (string?)installNode.Attribute("ExeCommand"));
        Assert.Contains("-File \"[#InstallScript]\" -Name Test", (string?)installNode.Attribute("ExeCommand"));

        var uninstallNode = CustomActionElement(document, uninstallAction.Id + "UNINSTALL");
        Assert.Equal("ignore", (string?)uninstallNode.Attribute("Return"));
        Assert.Equal("-NoProfile -File \"[#UninstallScript]\" -Remove", (string?)uninstallNode.Attribute("ExeCommand"));

        var installSequence = document.Descendants(Wix + "Custom")
            .Single(x => (string?)x.Attribute("Action") == installAction.Id + "INSTALL");
        Assert.Equal("InstallFiles", (string?)installSequence.Attribute("After"));
        Assert.Equal("NOT Installed", installSequence.Value);

        var uninstallSequence = document.Descendants(Wix + "Custom")
            .Single(x => (string?)x.Attribute("Action") == uninstallAction.Id + "UNINSTALL");
        Assert.Equal("RemoveFiles", (string?)uninstallSequence.Attribute("Before"));
        Assert.Equal("Installed", uninstallSequence.Value);
    }

    [Fact]
    public void Generate_ConfigurableDirectoryUsesInstallDirUiAndCanBeGeneratedAgain()
    {
        var generator = new WixXmlGenerator();
        var installer = CreateInstaller(
            content: new object[]
            {
                new DirectoryModel
                {
                    Id = "INSTALLFOLDER",
                    Name = "Product",
                    Configurable = true
                }
            },
            userInterface: new UserInterface
            {
                ExitDialogText = "Finished"
            });

        var firstDocument = generator.Generate(installer);
        var secondDocument = generator.Generate(CreateInstaller(userInterface: new UserInterface
        {
            Eula = @"C:\input\eula.rtf"
        }));

        var firstProduct = firstDocument.Descendants(Wix + "Product").Single();
        Assert.Equal("INSTALLFOLDER", ProductPropertyValue(firstProduct, "WIXUI_INSTALLDIR"));
        Assert.Contains(firstProduct.Descendants(Wix + "UIRef"), x => (string?)x.Attribute("Id") == "WixUI_InstallDir");
        Assert.Equal("Finished", ProductPropertyValue(firstProduct, "WIXUI_EXITDIALOGOPTIONALTEXT"));

        var secondProduct = secondDocument.Descendants(Wix + "Product").Single();
        Assert.Null(ProductPropertyValue(secondProduct, "WIXUI_INSTALLDIR"));
        Assert.Contains(secondProduct.Descendants(Wix + "UIRef"), x => (string?)x.Attribute("Id") == "WixUI_Minimal");
    }

    [Fact]
    public void Generate_ThrowsWhenMoreThanOneDirectoryIsConfigurable()
    {
        var installer = CreateInstaller(content: new object[]
        {
            new DirectoryModel { Id = "FIRST", Name = "First", Configurable = true },
            new DirectoryModel { Id = "SECOND", Name = "Second", Configurable = true }
        });

        var exception = Assert.Throws<Exception>(() => Generate(installer));
        Assert.Equal("You cannot specify more than one configurable directory. Remove the -Configurable switch from one of your New-InstallerDirectory calls.", exception.Message);
    }

    private static XDocument Generate(Installer installer)
    {
        return new WixXmlGenerator().Generate(installer);
    }

    private static Installer CreateInstaller(
        IEnumerable<object>? content = null,
        CustomAction[]? customActions = null,
        UserInterface? userInterface = null,
        bool requiresElevation = false)
    {
        return new Installer
        {
            ProductName = "Test Product",
            Description = "Test Description",
            Version = new Version(1, 2, 3, 4),
            Manufacturer = "Test Manufacturer",
            UpgradeCode = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            Platform = "x64",
            OutputDirectory = new DirectoryInfo(Path.GetTempPath()),
            Content = content ?? Array.Empty<object>(),
            HelpLink = "https://example.test/help",
            AboutLink = "https://example.test/about",
            RequiresElevation = requiresElevation,
            UserInterface = userInterface,
            CustomActions = customActions
        };
    }

    private static XElement CustomActionElement(XDocument document, string id)
    {
        return document.Descendants(Wix + "CustomAction")
            .Single(x => (string?)x.Attribute("Id") == id);
    }

    private static string? ProductPropertyValue(XElement product, string id)
    {
        return (string?)product.Elements(Wix + "Property")
            .SingleOrDefault(x => (string?)x.Attribute("Id") == id)
            ?.Attribute("Value");
    }
}
