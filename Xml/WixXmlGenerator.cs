using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PSMSI.Xml
{
    public class WixXmlGenerator
    {
        XNamespace wixNamespace = "http://schemas.microsoft.com/wix/2006/wi";
        private string _configurableDirectoryId;

        private Dictionary<string, string> _files;
        private Dictionary<string, string> _directories;

        public XDocument Generate(Models.Installer installer)
        {
            _files = new Dictionary<string, string>();
            _directories = new Dictionary<string, string>();

            var packageNode = new XElement(wixNamespace + "Package",
                            new XAttribute("InstallScope", installer.RequiresElevation ? "perMachine" : "perUser"),
                            new XAttribute("InstallPrivileges", installer.RequiresElevation ? "elevated" : "limited"),
                            new XAttribute("Comments", installer.ProductName),
                            new XAttribute("InstallerVersion", "200"),
                            new XAttribute("Platform", installer.Platform)
                            );

            if (!string.IsNullOrEmpty(installer.Description))
            {
                packageNode.Add(new XAttribute("Description", installer.Description));
            }

            var powershellProperty = new XElement(wixNamespace + "Property",
                new XAttribute("Id", "POWERSHELL"),
                new XElement(wixNamespace + "RegistrySearch",
                    new XAttribute("Id", "POWERSHELL"),
                    new XAttribute("Type", "raw"),
                    new XAttribute("Root", "HKLM"),
                    new XAttribute("Key", @"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell"),
                    new XAttribute("Name", "Path")));

            var mediaNode = new XElement(wixNamespace + "Media",
                            new XAttribute("Id", "1"),
                            new XAttribute("Cabinet", "cab1.cab"),
                            new XAttribute("EmbedCab", "yes")
                            );

            var targetDirNode = new XElement(wixNamespace + "Directory", new XAttribute("Id", "TARGETDIR"), new XAttribute("Name", "SourceDir"));

            var featureNode = new XElement(wixNamespace + "Feature", new XAttribute("Id", installer.ProductName.Replace(" ", "_").ToUpper()), new XAttribute("Title", installer.ProductName), new XAttribute("Level", "1"));

            var productNode = new XElement(wixNamespace + "Product",
                        new XAttribute("Id", installer.ProductId),
                        new XAttribute("Language", 1033),
                        new XAttribute("Name", installer.ProductName),
                        new XAttribute("Version", installer.Version.ToString()),
                        new XAttribute("Manufacturer", installer.Manufacturer),
                        new XAttribute("UpgradeCode", installer.UpgradeCode),
                        packageNode,
                        powershellProperty,
                        mediaNode,
                        targetDirNode,
                        new XElement(wixNamespace + "MajorUpgrade", new XAttribute("DowngradeErrorMessage", "A newer version of [ProductName] is already installed.")),
                        featureNode);

            if (!string.IsNullOrEmpty(installer.HelpLink))
            {
                productNode.Add(new XElement(wixNamespace + "Property", new XAttribute("Id", "ARPHELPLINK"), new XAttribute("Value", installer.HelpLink)));
            }

            if (!string.IsNullOrEmpty(installer.AboutLink))
            {
                productNode.Add(new XElement(wixNamespace + "Property", new XAttribute("Id", "ARPURLINFOABOUT"), new XAttribute("Value", installer.AboutLink)));
            }

            if (installer.AddRemoveProgramsIcon != null)
            {
                var iconId = "ico" + System.Guid.NewGuid().ToString("n");
                productNode.Add(new XElement(wixNamespace + "Icon", new XAttribute("Id", iconId), new XAttribute("SourceFile", installer.AddRemoveProgramsIcon.FullName)));
                productNode.Add(new XElement(wixNamespace + "Property", new XAttribute("Id", "ARPPRODUCTICON"), new XAttribute("Value", iconId)));
            }

            if (installer.CustomActions != null)
            {
                var installExecuteNode = new XElement(wixNamespace + "InstallExecuteSequence");

                string previousUninstallActionId = null;
                string previousInstallActionId = null;
                foreach (var customAction in installer.CustomActions)
                {
                    if (string.IsNullOrEmpty(customAction.Arguments))
                    {
                        customAction.Arguments = "-NoProfile -WindowStyle Hidden -NonInteractive  -InputFormat None -ExecutionPolicy Bypass";
                    }

                    if (customAction.RunOnInstall)
                    {
                        var customActionNode = new XElement(wixNamespace + "CustomAction",
                          new XAttribute("Id", customAction.Id + "INSTALL"),
                          new XAttribute("Property", "POWERSHELL"),
                          new XAttribute("ExeCommand", $"{customAction.Arguments} -File \"[#{customAction.FileId}]\" {customAction.ScriptArguments}"),
                          new XAttribute("Return", customAction.CheckReturnValue ? "check" : "ignore"),
                          new XAttribute("Execute", "deferred"));

                        productNode.Add(customActionNode);

                        var customActionSequence = new XElement(wixNamespace + "Custom",
                            new XAttribute("Action", customAction.Id + "INSTALL"),
                            new XAttribute("After", previousInstallActionId ?? "InstallFiles"));

                        customActionSequence.Add("NOT Installed");

                        installExecuteNode.Add(customActionSequence);

                        previousInstallActionId = customAction.Id + "INSTALL";
                    }

                    if (customAction.RunOnUninstall)
                    {
                        var customActionNode = new XElement(wixNamespace + "CustomAction",
                          new XAttribute("Id", customAction.Id + "UNINSTALL"),
                          new XAttribute("Property", "POWERSHELL"),
                          new XAttribute("ExeCommand", $"{customAction.Arguments} -File \"[#{customAction.FileId}]\" {customAction.ScriptArguments}"),
                          new XAttribute("Return", customAction.CheckReturnValue ? "check" : "ignore"),
                          new XAttribute("Execute", "deferred"));

                        productNode.Add(customActionNode);

                        var schedule = previousUninstallActionId == null ? new XAttribute("Before", "RemoveFiles") : new XAttribute("After", previousUninstallActionId);

                        var customActionSequence = new XElement(wixNamespace + "Custom",
                            new XAttribute("Action", customAction.Id + "UNINSTALL"),
                            schedule);

                        customActionSequence.Add("Installed");

                        installExecuteNode.Add(customActionSequence);

                        previousUninstallActionId = customAction.Id + "UNINSTALL";
                    }
                }

                productNode.Add(installExecuteNode);
            }

            foreach (var item in installer.Content)
            {
                PopulateFileDictionary(item);
            }

            foreach (var item in installer.Content)
            {
                GenerateContentXml(item, installer, productNode, featureNode, targetDirNode);
            }

            GenerateUserInterface(installer, productNode, featureNode, targetDirNode);

            var document = new XDocument(
                new XElement(wixNamespace + "Wix", productNode));

            return document;
        }

        private void PopulateFileDictionary(object item)
        {
            if (item is Models.Directory directory)
            {
                _directories.Add(directory.Id, directory.Name);

                if (directory.Content != null)
                {
                    foreach (var content in directory.Content)
                    {
                        PopulateFileDictionary(content);
                    }
                }
            }

            if (item is Models.File file)
            {
                _files.Add(file.Id, file.Source);
            }
        }

        private void GenerateContentXml(object item, Models.Installer installer, XElement productNode, XElement featureNode, XElement directoryNode)
        {
            switch (item.GetType().Name)
            {
                case nameof(Models.Directory):
                    GenerateDirectoryXml(item as Models.Directory, installer, productNode, featureNode, directoryNode);
                    break;
                case nameof(Models.File):
                    GenerateFileXml(item as Models.File, installer, productNode, featureNode, directoryNode);
                    break;
                case nameof(Models.Shortcut):
                    GenerateShortcutXml(item as Models.Shortcut, installer, productNode, featureNode, directoryNode);
                    break;
            }
        }

        private void GenerateDirectoryXml(Models.Directory directory, Models.Installer installer, XElement productNode, XElement featureNode, XElement parentDirectory)
        {
            var directoryXml = new XElement(wixNamespace + "Directory", new XAttribute("Id", directory.Id), new XAttribute("Name", directory.Name));
            if (directory.Content != null)
            {
                foreach (var item in directory.Content)
                {
                    GenerateContentXml(item, installer, productNode, featureNode, directoryXml);
                }
            }

            if (directory.Configurable)
            {
                if (string.IsNullOrEmpty(_configurableDirectoryId))
                {
                    _configurableDirectoryId = directory.Id;
                }
                else
                {
                    throw new System.Exception("You cannot specify more than one configurable directory. Remove the -Configurable switch from one of your New-InstallerDirectory calls.");
                }
            }

            parentDirectory.Add(directoryXml);
        }

        private void GenerateFileXml(Models.File file, Models.Installer installer, XElement productNode, XElement featureNode, XElement parentDirectory)
        {
            var componentId = "cmp" + System.Guid.NewGuid().ToString("n");
            var componentXml = new XElement(wixNamespace + "Component",
                new XAttribute("Id", componentId),
                new XAttribute("Guid", "*"),
                new XElement(wixNamespace + "File",
                    new XAttribute("Id", file.Id),
                    new XAttribute("Source", file.Source),
                    new XAttribute("KeyPath", "yes"),
                    new XAttribute("Compressed", "yes"),
                    new XAttribute("Checksum", "yes")));

            var componentRefXml = new XElement(wixNamespace + "ComponentRef", new XAttribute("Id", componentId));

            parentDirectory.Add(componentXml);
            featureNode.Add(componentRefXml);
        }

        private void GenerateShortcutXml(Models.Shortcut shortcut, Models.Installer installer, XElement productNode, XElement featureNode, XElement parentDirectory)
        {
            var componentId = "cmp" + System.Guid.NewGuid().ToString("n");

            var shortcutXml = new XElement(wixNamespace + "Shortcut",
                    new XAttribute("Id", "sht" + System.Guid.NewGuid().ToString("n")),
                    new XAttribute("Name", shortcut.Name),
                    new XAttribute("Show", shortcut.Show)
                    );

            var target = string.IsNullOrEmpty(shortcut.DirectoryId) ? new XAttribute("Target", $"[#{shortcut.FileId}]") : new XAttribute("Target", $"[{shortcut.DirectoryId}]");
            XAttribute arguments = null;

            if (shortcut.Arguments != null)
            {
                arguments = new XAttribute("Arguments", shortcut.Arguments);
            }

            if (!string.IsNullOrEmpty(shortcut.FileId) && _files.ContainsKey(shortcut.FileId))
            {
                var filePath = _files[shortcut.FileId];

                if (filePath.ToLower().EndsWith(".ps1"))
                {
                    target = new XAttribute("Target", "[POWERSHELL]");
                    arguments = new XAttribute("Arguments", $"-File \"[#{shortcut.FileId}]\" {shortcut.Arguments}");
                }
            }

            if (arguments != null)
            {
                shortcutXml.Add(arguments);
            }

            shortcutXml.Add(target);

            if (shortcut.WorkingDirectory != null)
            {
                shortcutXml.Add(new XAttribute("WorkingDirectory", shortcut.WorkingDirectory));
            }

            if (shortcut.IconPath != null)
            {
                var iconId = "ico" + System.Guid.NewGuid().ToString("n");

                var iconXml = new XElement(wixNamespace + "Icon",
                    new XAttribute("Id", iconId),
                    new XAttribute("SourceFile", shortcut.IconPath));

                shortcutXml.Add(new XAttribute("Icon", iconId));
                productNode.Add(iconXml);
            }

            var componentXml = new XElement(wixNamespace + "Component",
                new XAttribute("Id", componentId),
                new XAttribute("Guid", Guid.NewGuid().ToString()),
                shortcutXml,
                new XElement(wixNamespace + "RegistryValue",
                    new XAttribute("Root", "HKCU"),
                    new XAttribute("Key", $"Software\\{installer.Manufacturer}\\{installer.ProductName}"),
                    new XAttribute("Name", "installed"),
                    new XAttribute("Type", "integer"),
                    new XAttribute("Value", "1"),
                    new XAttribute("KeyPath", "yes"))
                );

            var componentRefXml = new XElement(wixNamespace + "ComponentRef", new XAttribute("Id", componentId));

            parentDirectory.Add(componentXml);
            featureNode.Add(componentRefXml);
        }

        private void GenerateUserInterface(Models.Installer installer, XElement productNode, XElement featureNode, XElement parentDirectory)
        {
            var userInterface = installer.UserInterface;
            if (userInterface == null) return;

            var uiXml = new XElement(wixNamespace + "UI");

            if (!string.IsNullOrEmpty(userInterface.ExitDialogText))
            {
                var element = new XElement(wixNamespace + "Property",
                    new XAttribute("Id", "WIXUI_EXITDIALOGOPTIONALTEXT"),
                    new XAttribute("Value", userInterface.ExitDialogText));

                productNode.Add(element);
            }

            if (userInterface.Eula != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUILicenseRtf"), new XAttribute("Value", userInterface.Eula)));
            }

            if (userInterface.ExclamationIcon != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUIExclamationIco"), new XAttribute("Value", userInterface.ExclamationIcon)));
            }

            if (userInterface.TopBanner != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUIBannerBmp"), new XAttribute("Value", userInterface.TopBanner)));
            }

            if (userInterface.WelcomeAndCompletionBackground != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUIDialogBmp"), new XAttribute("Value", userInterface.WelcomeAndCompletionBackground)));
            }

            if (userInterface.InformationIcon != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUIInfoIco"), new XAttribute("Value", userInterface.InformationIcon)));
            }

            if (userInterface.NewIcon != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUINewIco"), new XAttribute("Value", userInterface.NewIcon)));
            }

            if (userInterface.UpIcon != null)
            {
                productNode.Add(new XElement(wixNamespace + "WixVariable", new XAttribute("Id", "WixUIUpIco"), new XAttribute("Value", userInterface.UpIcon)));
            }

            if (string.IsNullOrEmpty(_configurableDirectoryId) && userInterface.Eula != null)
            {
                uiXml.Add(new XElement(wixNamespace + "UIRef", new XAttribute("Id", "WixUI_Minimal")));
            }
            else if (!string.IsNullOrEmpty(_configurableDirectoryId))
            {
                productNode.Add(new XElement(wixNamespace + "Property", new XAttribute("Id", "WIXUI_INSTALLDIR"), new XAttribute("Value", _configurableDirectoryId)));
                uiXml.Add(new XElement(wixNamespace + "UIRef", new XAttribute("Id", "WixUI_InstallDir")));
                if (userInterface.Eula == null)
                {
                    uiXml.Add(new XElement(wixNamespace + "Publish",
                        new XAttribute("Dialog", "WelcomeDlg"),
                        new XAttribute("Control", "Next"),
                        new XAttribute("Event", "NewDialog"),
                        new XAttribute("Value", "InstallDirDlg"),
                        new XAttribute("Order", "2"),
                        1));
                    uiXml.Add(new XElement(wixNamespace + "Publish",
                        new XAttribute("Dialog", "InstallDirDlg"),
                        new XAttribute("Control", "Back"),
                        new XAttribute("Event", "NewDialog"),
                        new XAttribute("Value", "WelcomeDlg"),
                        new XAttribute("Order", "2"),
                        1));
                }
            }

            productNode.Add(uiXml);
        }


    }
}
