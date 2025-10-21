using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using CsprojChecker.Core.Models;

namespace CsprojChecker.Core;

/// <summary>
/// Service for converting project files between old-style and SDK-style formats
/// and modifying target frameworks
/// </summary>
public class ProjectConversionService
{
    /// <summary>
    /// Convert an old-style project to SDK-style (round-trip compatible)
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <returns>Result of the conversion operation</returns>
    public ConversionResult ConvertOldStyleToSdkStyle(string csprojPath)
    {
        try
        {
            // Check if file is read-only
            if (File.Exists(csprojPath))
            {
                var fileInfo = new FileInfo(csprojPath);
                if (fileInfo.IsReadOnly)
                {
                    return new ConversionResult 
                    { 
                        Success = false, 
                        Error = $"File is read-only: {csprojPath}" 
                    };
                }
            }

            XDocument doc;
            Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(csprojPath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = $"File is locked or inaccessible: {ex.Message}" 
                };
            }

            var root = doc.Root;

            if (root == null)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = "Invalid project file" 
                };
            }

            // Check if already SDK-style
            if (root.Attribute("Sdk") != null)
            {
                // Already SDK-style, skip
                var existingTfm = ParseTargetFrameworks(root);
                return new ConversionResult 
                { 
                    Success = true, 
                    ResultingTargetFramework = existingTfm 
                };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Get framework version
            var oldTfm = ParseTargetFrameworks(root);

            // Detect if it's a WinForms or WPF project
            bool isWinForms = DetectWinFormsProject(root, ns);
            bool isWpf = DetectWpfProject(root, ns);
            bool isDesktop = isWinForms || isWpf;

            // Convert framework version
            string newTfm = ConvertFrameworkVersion(oldTfm, isWinForms);

            // Create new SDK-style project
            var newRoot = new XElement("Project");

            // Determine SDK attribute - use WindowsDesktop SDK for WinForms/WPF projects
            string sdkValue = isDesktop ? "Microsoft.NET.Sdk.WindowsDesktop" : "Microsoft.NET.Sdk";
            newRoot.Add(new XAttribute("Sdk", sdkValue));

            // Create PropertyGroup with essential properties
            var propertyGroup = new XElement("PropertyGroup");

            // Add TargetFramework
            propertyGroup.Add(new XElement("TargetFramework", newTfm));

            // Add OutputType if needed
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            if (outputType != null)
            {
                propertyGroup.Add(new XElement("OutputType", outputType.Value));
            }

            // Add WinForms specific properties if needed
            if (isWinForms)
            {
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
            }

            // Add WPF specific properties if needed
            if (isWpf)
            {
                propertyGroup.Add(new XElement("UseWPF", "true"));
            }

            // Add common properties
            propertyGroup.Add(new XElement("ImplicitUsings", "enable"));
            propertyGroup.Add(new XElement("Nullable", "enable"));

            // Copy over RootNamespace if it exists
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            if (rootNamespace != null)
            {
                propertyGroup.Add(new XElement("RootNamespace", rootNamespace.Value));
            }

            // Copy over AssemblyName if it exists
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            if (assemblyName != null)
            {
                propertyGroup.Add(new XElement("AssemblyName", assemblyName.Value));
            }

            newRoot.Add(propertyGroup);

            // Preserve PackageReferences
            var packageRefs = root
                .Descendants(ns + "PackageReference")
                .Select(CloneElementWithoutNamespace)
                .ToList();

            if (packageRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var pkg in packageRefs)
                    group.Add(pkg);
                newRoot.Add(group);
            }

            // Preserve ProjectReferences
            var projectRefs = root
                .Descendants(ns + "ProjectReference")
                .Select(CloneElementWithoutNamespace)
                .ToList();

            if (projectRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var projRef in projectRefs)
                    group.Add(projRef);
                newRoot.Add(group);
            }

            // Create new document
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);

            // Save the new document with encoding preservation
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(csprojPath, settings))
                {
                    newDoc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = $"Failed to write to file (may be locked): {ex.Message}" 
                };
            }

            return new ConversionResult 
            { 
                Success = true, 
                ResultingTargetFramework = newTfm 
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    /// <summary>
    /// Convert an old-style project to SDK-style (one-way modern conversion)
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <returns>Result of the conversion operation</returns>
    public ConversionResult ConvertOldStyleToSdkStyleModern(string csprojPath)
    {
        try
        {
            if (!File.Exists(csprojPath))
                return new ConversionResult { Success = false, Error = "Project file not found" };

            var fileInfo = new FileInfo(csprojPath);
            if (fileInfo.IsReadOnly)
                return new ConversionResult { Success = false, Error = $"File is read-only: {csprojPath}" };

            var backupPath = csprojPath + ".bak";
            File.Copy(csprojPath, backupPath, overwrite: true);

            XDocument doc;
            Encoding? encoding;
            using (var reader = new StreamReader(csprojPath, true))
            {
                reader.Peek();
                encoding = reader.CurrentEncoding;
            }
            doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);

            var root = doc.Root;
            if (root == null)
                return new ConversionResult { Success = false, Error = "Invalid project file" };
                
            if (root.Attribute("Sdk") != null)
            {
                var existingTfm = ParseTargetFrameworks(root);
                return new ConversionResult { Success = true, ResultingTargetFramework = existingTfm };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Detect project type
            bool isWinForms = DetectWinFormsProject(root, ns);
            bool isWpf = DetectWpfProject(root, ns);
            bool isDesktop = isWinForms || isWpf;

            // Get old framework
            var oldTfm = ParseTargetFrameworks(root);

            // Use legacy-to-modern mapping
            string mappedTfm = MapLegacyFrameworkToModernTfm(oldTfm, isDesktop);
            string sdkValue = isDesktop ? "Microsoft.NET.Sdk.WindowsDesktop" : "Microsoft.NET.Sdk";

            // Build new project structure
            var project = new XElement("Project", new XAttribute("Sdk", sdkValue));
            var propertyGroup = new XElement("PropertyGroup");

            propertyGroup.Add(new XElement("TargetFramework", mappedTfm));
            propertyGroup.Add(new XElement("Nullable", "enable"));
            propertyGroup.Add(new XElement("ImplicitUsings", "enable"));
            propertyGroup.Add(new XElement("LangVersion", "latest"));

            AddPropertyIfExists(propertyGroup, root, ns, "OutputType");
            AddPropertyIfExists(propertyGroup, root, ns, "RootNamespace");
            AddPropertyIfExists(propertyGroup, root, ns, "AssemblyName");

            if (isWinForms)
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
            if (isWpf)
                propertyGroup.Add(new XElement("UseWPF", "true"));

            project.Add(propertyGroup);

            // Preserve PackageReferences
            var packageRefs = root
                .Descendants(ns + "PackageReference")
                .Select(CloneElementWithoutNamespace)
                .ToList();

            if (packageRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var pkg in packageRefs)
                    group.Add(pkg);
                project.Add(group);
            }

            // Preserve ProjectReferences
            var projectRefs = root
                .Descendants(ns + "ProjectReference")
                .Select(CloneElementWithoutNamespace)
                .ToList();

            if (projectRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var projRef in projectRefs)
                    group.Add(projRef);
                project.Add(group);
            }

            // Save atomically
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), project);
            var tempPath = csprojPath + ".tmp";
            var settings = new XmlWriterSettings
            {
                Encoding = encoding ?? Encoding.UTF8,
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(tempPath, settings))
                newDoc.Save(writer);

            File.Replace(tempPath, csprojPath, backupPath, ignoreMetadataErrors: true);

            return new ConversionResult 
            { 
                Success = true, 
                ResultingTargetFramework = mappedTfm 
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    /// <summary>
    /// Convert an old-style project to SDK-style (custom one-way modern conversion with explicit includes)
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <returns>Result of the conversion operation</returns>
    public ConversionResult ConvertOldStyleToSdkStyleCustomModern(string csprojPath)
    {
        try
        {
            if (!File.Exists(csprojPath))
                return new ConversionResult { Success = false, Error = "Project file not found" };

            var fileInfo = new FileInfo(csprojPath);
            if (fileInfo.IsReadOnly)
                return new ConversionResult { Success = false, Error = $"File is read-only: {csprojPath}" };

            var backupPath = csprojPath + ".bak";
            File.Copy(csprojPath, backupPath, overwrite: true);

            XDocument doc;
            Encoding? encoding;
            using (var reader = new StreamReader(csprojPath, true))
            {
                reader.Peek();
                encoding = reader.CurrentEncoding;
            }
            doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);

            var root = doc.Root;
            if (root == null)
                return new ConversionResult { Success = false, Error = "Invalid project file" };
                
            if (root.Attribute("Sdk") != null)
            {
                var existingTfm = ParseTargetFrameworks(root);
                return new ConversionResult { Success = true, ResultingTargetFramework = existingTfm };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Detect project type
            bool isWinForms = DetectWinFormsProject(root, ns);
            bool isWpf = DetectWpfProject(root, ns);
            bool isDesktop = isWinForms || isWpf;

            // Get old framework
            var oldTfmVersion = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault()?.Value ?? "v4.7.2";
            string mappedTfm = ConvertFrameworkVersionCustomModern(oldTfmVersion);
            
            // Get OutputType to determine if library
            var outputTypeElement = root.Descendants(ns + "OutputType").FirstOrDefault();
            bool isLibrary = outputTypeElement?.Value.Equals("Library", StringComparison.OrdinalIgnoreCase) ?? false;

            string sdkValue = isDesktop ? "Microsoft.NET.Sdk.WindowsDesktop" : "Microsoft.NET.Sdk";

            // Build new project structure
            var project = new XElement("Project", new XAttribute("Sdk", sdkValue));
            
            // Main PropertyGroup
            var propertyGroup = new XElement("PropertyGroup");

            // Always add these modern properties first
            AddPropertyIfNotExists(propertyGroup, "Configuration", 
                new XAttribute("Condition", " '$(Configuration)' == '' "), "Debug");
            AddPropertyIfNotExists(propertyGroup, "Platform", 
                new XAttribute("Condition", " '$(Platform)' == '' "), "AnyCPU");

            // Add core properties
            AddPropertyIfExistsOld(propertyGroup, root, ns, "OutputType");
            AddPropertyIfExistsOld(propertyGroup, root, ns, "RootNamespace");
            AddPropertyIfExistsOld(propertyGroup, root, ns, "AssemblyName");
            
            propertyGroup.Add(new XElement("TargetFramework", mappedTfm));

            // Always add custom modern properties
            propertyGroup.Add(new XElement("GenerateAssemblyInfo", "True"));
            propertyGroup.Add(new XElement("EnableDefaultCompileItems", "false"));
            propertyGroup.Add(new XElement("EnableDefaultEmbeddedResourceItems", "false"));
            propertyGroup.Add(new XElement("AppendTargetFrameworkToOutputPath", "true"));

            // Conditionally add ProjectGuid if it exists (for backward compatibility)
            var projectGuid = root.Descendants(ns + "ProjectGuid").FirstOrDefault();
            if (projectGuid != null && !string.IsNullOrWhiteSpace(projectGuid.Value))
            {
                propertyGroup.Add(new XElement("ProjectGuid", projectGuid.Value));
            }

            // Check for StartupObject and only add if not empty and not a library
            var startupObject = root.Descendants(ns + "StartupObject").FirstOrDefault();
            if (startupObject != null && !string.IsNullOrWhiteSpace(startupObject.Value) && !isLibrary)
            {
                propertyGroup.Add(new XElement("StartupObject", startupObject.Value));
            }

            // Preserve AutoGenerateBindingRedirects if it exists
            var autoGenBindingRedirects = root.Descendants(ns + "AutoGenerateBindingRedirects").FirstOrDefault();
            if (autoGenBindingRedirects != null && !string.IsNullOrWhiteSpace(autoGenBindingRedirects.Value))
            {
                propertyGroup.Add(new XElement("AutoGenerateBindingRedirects", autoGenBindingRedirects.Value));
            }

            // Handle ApplicationIcon
            var applicationIcon = root.Descendants(ns + "ApplicationIcon").FirstOrDefault();
            bool isExecutable = outputTypeElement?.Value.Equals("Exe", StringComparison.OrdinalIgnoreCase) == true ||
                               outputTypeElement?.Value.Equals("WinExe", StringComparison.OrdinalIgnoreCase) == true;
            
            if (applicationIcon != null && !string.IsNullOrWhiteSpace(applicationIcon.Value) && isExecutable)
            {
                propertyGroup.Add(new XElement("ApplicationIcon", applicationIcon.Value));
            }

            if (isWinForms)
                propertyGroup.Add(new XElement("UseWindowsForms", "true"));
            if (isWpf)
                propertyGroup.Add(new XElement("UseWPF", "true"));

            project.Add(propertyGroup);

            // Preserve conditional PropertyGroups (Debug, Release, etc.)
            var conditionalGroups = root.Elements(ns + "PropertyGroup")
                .Where(pg => pg.Attribute("Condition") != null)
                .ToList();

            foreach (var condGroup in conditionalGroups)
            {
                var newCondGroup = new XElement("PropertyGroup");
                var condition = condGroup.Attribute("Condition");
                if (condition != null)
                {
                    newCondGroup.Add(new XAttribute("Condition", condition.Value));
                }

                // Preserve specific compiler/build settings
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "DebugSymbols");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "DebugType");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "Optimize");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "OutputPath");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "DefineConstants");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "WarningLevel");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "PlatformTarget");
                PreservePropertyIfExists(newCondGroup, condGroup, ns, "Prefer32Bit");

                // Skip obsolete properties like ErrorReport
                // Do NOT add: ErrorReport, ProductVersion, SchemaVersion, AppDesignerFolder, TargetFrameworkProfile

                if (newCondGroup.HasElements)
                {
                    project.Add(newCondGroup);
                }
            }

            // Preserve all Compile includes
            var compileItems = root.Descendants(ns + "Compile").ToList();
            if (compileItems.Any())
            {
                var compileGroup = new XElement("ItemGroup");
                foreach (var compile in compileItems)
                {
                    var newCompile = CloneElementWithoutNamespace(compile);
                    compileGroup.Add(newCompile);
                }
                project.Add(compileGroup);
            }

            // Preserve EmbeddedResource includes
            var embeddedResourceItems = root.Descendants(ns + "EmbeddedResource").ToList();
            if (embeddedResourceItems.Any())
            {
                var embeddedGroup = new XElement("ItemGroup");
                foreach (var embedded in embeddedResourceItems)
                {
                    var newEmbedded = CloneElementWithoutNamespace(embedded);
                    embeddedGroup.Add(newEmbedded);
                }
                project.Add(embeddedGroup);
            }

            // Preserve Content items
            var contentItems = root.Descendants(ns + "Content").ToList();
            
            // If ApplicationIcon exists and is not already in Content, add it
            if (applicationIcon != null && !string.IsNullOrWhiteSpace(applicationIcon.Value) && isExecutable)
            {
                var iconPath = applicationIcon.Value;
                bool iconAlreadyInContent = contentItems.Any(c => c.Attribute("Include")?.Value == iconPath);
                if (!iconAlreadyInContent)
                {
                    // Add icon to content items
                    var iconContent = new XElement(ns + "Content");
                    iconContent.Add(new XAttribute("Include", iconPath));
                    contentItems.Add(iconContent);
                }
            }

            if (contentItems.Any())
            {
                var contentGroup = new XElement("ItemGroup");
                foreach (var content in contentItems)
                {
                    var newContent = CloneElementWithoutNamespace(content);
                    contentGroup.Add(newContent);
                }
                project.Add(contentGroup);
            }

            // Preserve None items
            var noneItems = root.Descendants(ns + "None").ToList();
            if (noneItems.Any())
            {
                var noneGroup = new XElement("ItemGroup");
                foreach (var none in noneItems)
                {
                    var newNone = CloneElementWithoutNamespace(none);
                    noneGroup.Add(newNone);
                }
                project.Add(noneGroup);
            }

            // Preserve ApplicationDefinition items (for WPF)
            var appDefItems = root.Descendants(ns + "ApplicationDefinition").ToList();
            if (appDefItems.Any())
            {
                var appDefGroup = new XElement("ItemGroup");
                foreach (var appDef in appDefItems)
                {
                    var newAppDef = CloneElementWithoutNamespace(appDef);
                    appDefGroup.Add(newAppDef);
                }
                project.Add(appDefGroup);
            }

            // Preserve Page items (for WPF)
            var pageItems = root.Descendants(ns + "Page").ToList();
            if (pageItems.Any())
            {
                var pageGroup = new XElement("ItemGroup");
                foreach (var page in pageItems)
                {
                    var newPage = CloneElementWithoutNamespace(page);
                    pageGroup.Add(newPage);
                }
                project.Add(pageGroup);
            }

            // Handle Reference elements
            var references = root.Descendants(ns + "Reference").ToList();
            var localReferences = new List<XElement>();
            var packageReferencesToAdd = new List<(string Name, string Version)>();

            foreach (var reference in references)
            {
                var includeName = reference.Attribute("Include")?.Value ?? "";
                var hintPath = reference.Elements(ns + "HintPath").FirstOrDefault()?.Value;

                // Skip framework references that are automatically included by SDK
                if (IsFrameworkReference(includeName))
                {
                    continue;
                }

                // Check if this is a NuGet package reference (HintPath contains /packages/)
                if (!string.IsNullOrEmpty(hintPath) && hintPath.Contains(@"\packages\"))
                {
                    // Extract package name and version from HintPath
                    // Example: ..\packages\Newtonsoft.Json.13.0.1\lib\net45\Newtonsoft.Json.dll
                    var match = System.Text.RegularExpressions.Regex.Match(
                        hintPath, 
                        @"\\packages\\([^\\]+?)\.(\d+(?:\.\d+)+)\\");
                    
                    if (match.Success)
                    {
                        var packageName = match.Groups[1].Value;
                        var packageVersion = match.Groups[2].Value;
                        packageReferencesToAdd.Add((packageName, packageVersion));
                        continue;
                    }
                }

                // Keep local references with HintPath
                if (!string.IsNullOrEmpty(hintPath))
                {
                    localReferences.Add(reference);
                }
            }

            // Add local references
            if (localReferences.Any())
            {
                var refGroup = new XElement("ItemGroup");
                foreach (var localRef in localReferences)
                {
                    var newRef = CloneElementWithoutNamespace(localRef);
                    refGroup.Add(newRef);
                }
                project.Add(refGroup);
            }

            // Preserve existing PackageReferences and add converted ones
            var existingPackageRefs = root
                .Descendants(ns + "PackageReference")
                .Select(CloneElementWithoutNamespace)
                .ToList();

            var allPackageRefs = new List<XElement>();
            allPackageRefs.AddRange(existingPackageRefs);

            // Add converted NuGet references
            foreach (var (name, version) in packageReferencesToAdd)
            {
                var packageRef = new XElement("PackageReference");
                packageRef.Add(new XAttribute("Include", name));
                packageRef.Add(new XAttribute("Version", version));
                allPackageRefs.Add(packageRef);
            }

            if (allPackageRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var pkg in allPackageRefs)
                    group.Add(pkg);
                project.Add(group);
            }

            // Preserve and simplify ProjectReferences to self-closing format
            var projectRefs = root.Descendants(ns + "ProjectReference").ToList();
            if (projectRefs.Any())
            {
                var group = new XElement("ItemGroup");
                foreach (var projRef in projectRefs)
                {
                    // Create self-closing ProjectReference with only Include attribute
                    var includeAttr = projRef.Attribute("Include");
                    if (includeAttr != null)
                    {
                        var newProjRef = new XElement("ProjectReference", new XAttribute("Include", includeAttr.Value));
                        group.Add(newProjRef);
                    }
                }
                project.Add(group);
            }

            // Save atomically
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), project);
            var tempPath = csprojPath + ".tmp";
            var settings = new XmlWriterSettings
            {
                Encoding = encoding ?? Encoding.UTF8,
                Indent = true,
                IndentChars = "  ",
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(tempPath, settings))
                newDoc.Save(writer);

            File.Replace(tempPath, csprojPath, backupPath, ignoreMetadataErrors: true);

            return new ConversionResult 
            { 
                Success = true, 
                ResultingTargetFramework = mappedTfm 
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    /// <summary>
    /// Convert an SDK-style project to old-style format
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <returns>Result of the conversion operation</returns>
    public ConversionResult ConvertSdkStyleToOldStyle(string csprojPath)
    {
        try
        {
            // Check if file is read-only
            if (File.Exists(csprojPath))
            {
                var fileInfo = new FileInfo(csprojPath);
                if (fileInfo.IsReadOnly)
                {
                    return new ConversionResult 
                    { 
                        Success = false, 
                        Error = $"File is read-only: {csprojPath}" 
                    };
                }
            }

            XDocument doc;
            Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(csprojPath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = $"File is locked or inaccessible: {ex.Message}" 
                };
            }

            var root = doc.Root;

            if (root == null)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = "Invalid project file" 
                };
            }

            // Check if already Old-style
            if (root.Attribute("Sdk") == null)
            {
                // Already Old-style, skip
                var existingTfm = ParseTargetFrameworks(root);
                return new ConversionResult 
                { 
                    Success = true, 
                    ResultingTargetFramework = existingTfm 
                };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Check for blocking conditions
            var packageRefs = root.Descendants(ns + "PackageReference").ToList();
            if (packageRefs.Any())
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = $"Has {packageRefs.Count} PackageReference(s)" 
                };
            }

            var currentTfm = ParseTargetFrameworks(root);

            // Check if multiple targets
            if (currentTfm.Contains(";"))
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = "Multiple target frameworks" 
                };
            }

            // Check if it's a .NET Framework target
            if (!IsNetFrameworkTarget(currentTfm))
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = "Not a .NET Framework target" 
                };
            }

            // Convert framework version from SDK-style to Old-style
            string newTfm = ConvertSdkToOldStyleFrameworkVersion(currentTfm);

            // Detect if it's a WinForms project
            bool isWinForms = IsWinFormsInSdkProject(root, ns);

            // Create new Old-style project
            XNamespace msbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";
            var newRoot = new XElement(msbuildNs + "Project");
            newRoot.Add(new XAttribute("ToolsVersion", "15.0"));

            // Default xmlns attribute
            newRoot.Add(new XAttribute("xmlns", "http://schemas.microsoft.com/developer/msbuild/2003"));

            // Import Microsoft.Common.props at the beginning
            var importGroup1 = new XElement(msbuildNs + "Import");
            importGroup1.Add(new XAttribute("Project", @"$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"));
            importGroup1.Add(new XAttribute("Condition", @"Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"));
            newRoot.Add(importGroup1);

            // Create PropertyGroup with essential properties
            var propertyGroup = new XElement(msbuildNs + "PropertyGroup");

            // Copy Configuration and Platform
            propertyGroup.Add(new XElement(msbuildNs + "Configuration", new XAttribute("Condition", " '$(Configuration)' == '' "), "Debug"));
            propertyGroup.Add(new XElement(msbuildNs + "Platform", new XAttribute("Condition", " '$(Platform)' == '' "), "AnyCPU"));

            // Add ProjectGuid
            propertyGroup.Add(new XElement(msbuildNs + "ProjectGuid", "{" + Guid.NewGuid().ToString().ToUpper() + "}"));

            // Add OutputType
            var outputType = root.Descendants(ns + "OutputType").FirstOrDefault();
            if (outputType != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "OutputType", outputType.Value));
            }
            else
            {
                propertyGroup.Add(new XElement(msbuildNs + "OutputType", "Library"));
            }

            // Add RootNamespace
            var rootNamespace = root.Descendants(ns + "RootNamespace").FirstOrDefault();
            if (rootNamespace != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", rootNamespace.Value));
            }
            else
            {
                // Use file name without extension
                propertyGroup.Add(new XElement(msbuildNs + "RootNamespace", Path.GetFileNameWithoutExtension(csprojPath)));
            }

            // Add AssemblyName
            var assemblyName = root.Descendants(ns + "AssemblyName").FirstOrDefault();
            if (assemblyName != null)
            {
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", assemblyName.Value));
            }
            else
            {
                // Use file name without extension
                propertyGroup.Add(new XElement(msbuildNs + "AssemblyName", Path.GetFileNameWithoutExtension(csprojPath)));
            }

            // Add TargetFrameworkVersion
            propertyGroup.Add(new XElement(msbuildNs + "TargetFrameworkVersion", newTfm));

            // Add FileAlignment
            propertyGroup.Add(new XElement(msbuildNs + "FileAlignment", "512"));

            // Add Deterministic
            propertyGroup.Add(new XElement(msbuildNs + "Deterministic", "true"));

            newRoot.Add(propertyGroup);

            // Add Debug PropertyGroup
            var debugPropertyGroup = new XElement(msbuildNs + "PropertyGroup");
            debugPropertyGroup.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DebugSymbols", "true"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DebugType", "full"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "Optimize", "false"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "OutputPath", @"bin\Debug\"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "DefineConstants", "DEBUG;TRACE"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            debugPropertyGroup.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(debugPropertyGroup);

            // Add Release PropertyGroup
            var releasePropertyGroup = new XElement(msbuildNs + "PropertyGroup");
            releasePropertyGroup.Add(new XAttribute("Condition", " '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "));
            releasePropertyGroup.Add(new XElement(msbuildNs + "DebugType", "pdbonly"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "Optimize", "true"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "OutputPath", @"bin\Release\"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "DefineConstants", "TRACE"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "ErrorReport", "prompt"));
            releasePropertyGroup.Add(new XElement(msbuildNs + "WarningLevel", "4"));
            newRoot.Add(releasePropertyGroup);

            // Add References ItemGroup
            var referencesGroup = new XElement(msbuildNs + "ItemGroup");
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Core")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml.Linq")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data.DataSetExtensions")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "Microsoft.CSharp")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Data")));
            referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Xml")));

            // Add WinForms references if needed
            if (isWinForms)
            {
                referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Drawing")));
                referencesGroup.Add(new XElement(msbuildNs + "Reference", new XAttribute("Include", "System.Windows.Forms")));
            }

            newRoot.Add(referencesGroup);

            // Import Microsoft.CSharp.targets at the end
            var importGroup2 = new XElement(msbuildNs + "Import");
            importGroup2.Add(new XAttribute("Project", @"$(MSBuildToolsPath)\Microsoft.CSharp.targets"));
            newRoot.Add(importGroup2);

            // Create new document
            var newDoc = new XDocument(new XDeclaration("1.0", "utf-8", null), newRoot);

            // Save the new document with encoding preservation
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = false
                };

                using (var writer = XmlWriter.Create(csprojPath, settings))
                {
                    newDoc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                return new ConversionResult 
                { 
                    Success = false, 
                    Error = $"Failed to write to file (may be locked): {ex.Message}" 
                };
            }

            return new ConversionResult 
            { 
                Success = true, 
                ResultingTargetFramework = newTfm 
            };
        }
        catch (Exception ex)
        {
            return new ConversionResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    /// <summary>
    /// Change the target framework of a project
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <param name="newFramework">New target framework value (can be single or semicolon-separated)</param>
    /// <returns>Result of the operation</returns>
    public OperationResult ChangeTargetFramework(string csprojPath, string newFramework)
    {
        try
        {
            // Check if file is read-only
            if (File.Exists(csprojPath))
            {
                var fileInfo = new FileInfo(csprojPath);
                if (fileInfo.IsReadOnly)
                {
                    return new OperationResult 
                    { 
                        Success = false, 
                        Error = $"File is read-only: {csprojPath}" 
                    };
                }
            }

            // Load document with encoding preservation
            XDocument doc;
            Encoding? encoding = null;

            try
            {
                // Detect encoding from file
                using (var reader = new StreamReader(csprojPath, true))
                {
                    reader.Peek(); // Force encoding detection
                    encoding = reader.CurrentEncoding;
                }

                doc = XDocument.Load(csprojPath, LoadOptions.PreserveWhitespace);
            }
            catch (IOException ex)
            {
                return new OperationResult 
                { 
                    Success = false, 
                    Error = $"File is locked or inaccessible: {ex.Message}" 
                };
            }

            var root = doc.Root;

            if (root == null)
            {
                return new OperationResult 
                { 
                    Success = false, 
                    Error = "Invalid project file" 
                };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Find ALL existing TargetFramework or TargetFrameworks elements
            var targetFrameworkElements = root.Descendants(ns + "TargetFramework").ToList();
            var targetFrameworksElements = root.Descendants(ns + "TargetFrameworks").ToList();

            // Determine if we need singular or plural based on the new value
            bool isMultiple = newFramework.Contains(';');

            if (isMultiple)
            {
                // We need TargetFrameworks (plural)
                // Remove ALL conflicting TargetFramework (singular) elements
                foreach (var element in targetFrameworkElements)
                {
                    element.Remove();
                }

                if (targetFrameworksElements.Any())
                {
                    // Update ALL existing TargetFrameworks elements
                    foreach (var element in targetFrameworksElements)
                    {
                        element.Value = newFramework;
                    }
                }
                else
                {
                    // Create new TargetFrameworks element in the first PropertyGroup
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup == null)
                    {
                        propertyGroup = new XElement(ns + "PropertyGroup");
                        root.Add(propertyGroup);
                    }
                    propertyGroup.Add(new XElement(ns + "TargetFrameworks", newFramework));
                }
            }
            else
            {
                // Single framework
                // Remove ALL conflicting TargetFrameworks (plural) elements
                foreach (var element in targetFrameworksElements)
                {
                    element.Remove();
                }

                if (targetFrameworkElements.Any())
                {
                    // Update ALL existing TargetFramework elements
                    foreach (var element in targetFrameworkElements)
                    {
                        element.Value = newFramework;
                    }
                }
                else
                {
                    // Create new TargetFramework element in the first PropertyGroup
                    var propertyGroup = root.Descendants(ns + "PropertyGroup").FirstOrDefault();
                    if (propertyGroup == null)
                    {
                        propertyGroup = new XElement(ns + "PropertyGroup");
                        root.Add(propertyGroup);
                    }
                    propertyGroup.Add(new XElement(ns + "TargetFramework", newFramework));
                }
            }

            // Save with original encoding
            try
            {
                var settings = new XmlWriterSettings
                {
                    Encoding = encoding ?? Encoding.UTF8,
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = doc.Declaration == null
                };

                using (var writer = XmlWriter.Create(csprojPath, settings))
                {
                    doc.Save(writer);
                }
            }
            catch (IOException ex)
            {
                return new OperationResult 
                { 
                    Success = false, 
                    Error = $"Failed to write to file (may be locked): {ex.Message}" 
                };
            }

            return new OperationResult 
            { 
                Success = true, 
                ResultingTargetFramework = newFramework 
            };
        }
        catch (Exception ex)
        {
            return new OperationResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    /// <summary>
    /// Append a target framework to an existing project
    /// </summary>
    /// <param name="csprojPath">Path to the .csproj file</param>
    /// <param name="frameworkToAppend">Framework to append</param>
    /// <returns>Result of the operation</returns>
    public OperationResult AppendTargetFramework(string csprojPath, string frameworkToAppend)
    {
        try
        {
            // Load the project
            var doc = XDocument.Load(csprojPath);
            var root = doc.Root;

            if (root == null)
            {
                return new OperationResult 
                { 
                    Success = false, 
                    Error = "Invalid project file" 
                };
            }

            // Check if it's SDK-style (required for append)
            if (root.Attribute("Sdk") == null)
            {
                return new OperationResult 
                { 
                    Success = false, 
                    Error = "Append is only supported for SDK-style projects" 
                };
            }

            XNamespace ns = root.GetDefaultNamespace();

            // Check if WinForms
            var useWinForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
            bool isWinForms = useWinForms != null && useWinForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase);

            // Get current TFMs
            var currentTfms = ParseTargetFrameworks(root);

            // Append the new framework
            var newTfms = AppendTfmValue(currentTfms, frameworkToAppend, isWinForms);

            // Use ChangeTargetFramework to update the project
            return ChangeTargetFramework(csprojPath, newTfms);
        }
        catch (Exception ex)
        {
            return new OperationResult 
            { 
                Success = false, 
                Error = ex.Message 
            };
        }
    }

    #region Helper Methods

    private string ParseTargetFrameworks(XElement root)
    {
        XNamespace ns = root.GetDefaultNamespace();

        // Look for TargetFrameworks (plural) first
        var targetFrameworksElement = root.Descendants(ns + "TargetFrameworks").FirstOrDefault();
        if (targetFrameworksElement != null)
        {
            return targetFrameworksElement.Value.Trim();
        }

        // Look for TargetFramework (singular)
        var targetFrameworkElement = root.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (targetFrameworkElement != null)
        {
            return targetFrameworkElement.Value.Trim();
        }

        // Look for TargetFrameworkVersion (old-style projects)
        var targetFrameworkVersionElement = root.Descendants(ns + "TargetFrameworkVersion").FirstOrDefault();
        if (targetFrameworkVersionElement != null)
        {
            return targetFrameworkVersionElement.Value.Trim();
        }

        // If neither exists, return empty string
        return "";
    }

    private bool DetectWinFormsProject(XElement root, XNamespace ns)
    {
        // Check for WinForms indicators
        var references = root.Descendants(ns + "Reference")
                            .Select(r => r.Attribute("Include")?.Value ?? "")
                            .ToList();

        bool hasWinFormsRef = references.Any(r =>
            r.Contains("System.Windows.Forms") ||
            r.Contains("System.Drawing"));

        // Check for UseWindowsForms property
        var useWindowsForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
        if (useWindowsForms != null && useWindowsForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Check for form files
        var compiles = root.Descendants(ns + "Compile")
                          .Select(c => c.Attribute("Include")?.Value ?? "")
                          .ToList();

        bool hasFormFiles = compiles.Any(c => c.EndsWith(".Designer.cs"));

        return hasWinFormsRef || hasFormFiles;
    }

    private bool DetectWpfProject(XElement root, XNamespace ns)
    {
        var useWpf = root.Descendants(ns + "UseWPF").FirstOrDefault();
        if (useWpf != null && useWpf.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var references = root.Descendants(ns + "Reference")
                             .Select(r => r.Attribute("Include")?.Value ?? string.Empty)
                             .ToList();

        bool hasWpfReferences = references.Any(r =>
            r.StartsWith("PresentationFramework", StringComparison.OrdinalIgnoreCase) ||
            r.StartsWith("PresentationCore", StringComparison.OrdinalIgnoreCase) ||
            r.StartsWith("WindowsBase", StringComparison.OrdinalIgnoreCase) ||
            r.StartsWith("System.Xaml", StringComparison.OrdinalIgnoreCase));

        if (hasWpfReferences)
        {
            return true;
        }

        bool hasWpfItems = root.Descendants(ns + "Page").Any() ||
                            root.Descendants(ns + "ApplicationDefinition").Any();

        return hasWpfItems;
    }

    private string ConvertFrameworkVersion(string oldTfm, bool isWinForms)
    {
        // Handle variable tokens - preserve them verbatim
        if (oldTfm.StartsWith("$"))
        {
            return oldTfm;
        }

        // Map old-style versions to SDK-style
        // Examples: v4.5 → net45, v4.7.2 → net472
        var trimmed = oldTfm.Trim();

        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            // Remove 'v' prefix and dots
            var version = trimmed.Substring(1).Replace(".", "");
            var newTfm = "net" + version;

            // For .NET Framework 4.x versions, do NOT add -windows suffix
            // (This is the realistic expectation per the tests)
            return newTfm;
        }

        // If it doesn't start with 'v', return as-is
        return trimmed;
    }

    private string MapLegacyFrameworkToModernTfm(string oldTfm, bool isDesktop)
    {
        if (string.IsNullOrWhiteSpace(oldTfm))
            return "net48"; // Fallback: no -windows for net4x

        var tfm = oldTfm.Trim();

        // Preserve variable tokens verbatim (e.g., $(TargetFrameworks))
        if (tfm.StartsWith("$"))
        {
            return tfm;
        }

        var tfmLower = tfm.ToLowerInvariant();

        // Handle old-style values like "v4.7.2"
        if (tfmLower.StartsWith("v"))
        {
            var version = tfmLower.Substring(1).Replace(".", "");
            var mapped = "net" + version;
            
            // Only add -windows suffix for net5.0+ desktop TFMs, never for net4x
            if (isDesktop && !mapped.EndsWith("-windows") && IsNet5OrLater(mapped))
            {
                mapped += "-windows";
            }
            return mapped;
        }

        // Already SDK-like
        if (tfmLower.StartsWith("net"))
            return tfm;

        // Fallback: no -windows for net4x
        return "net48";
    }

    private bool IsNet5OrLater(string tfm)
    {
        var lower = tfm.ToLowerInvariant();
        if (!lower.StartsWith("net"))
            return false;

        // Extract version part after "net"
        var versionPart = lower.Substring(3);
        
        // Remove any suffix like -windows
        var dashIndex = versionPart.IndexOf('-');
        if (dashIndex > 0)
            versionPart = versionPart.Substring(0, dashIndex);

        // net5.0, net6.0, net7.0, net8.0, net9.0+ have single digit major version
        // net4x have 2-3 digit versions: net40, net45, net451, net46, net461, net462, net47, net471, net472, net48, net481
        if (versionPart.Length >= 1 && char.IsDigit(versionPart[0]))
        {
            var firstDigit = versionPart[0] - '0';
            // net5.0+ starts with 5 or higher single digit
            // net4x starts with 4 and has at least 2 digits
            if (firstDigit >= 5)
                return true;
            if (firstDigit == 4 && versionPart.Length >= 2)
                return false; // net4x
        }

        return false;
    }

    private void AddPropertyIfExists(XElement propertyGroup, XElement root, XNamespace ns, string propertyName)
    {
        var element = root.Descendants(ns + propertyName).FirstOrDefault();
        if (element != null && !string.IsNullOrWhiteSpace(element.Value))
        {
            propertyGroup.Add(new XElement(propertyName, element.Value));
        }
    }

    private void AddPropertyIfExistsOld(XElement propertyGroup, XElement root, XNamespace ns, string propertyName)
    {
        var element = root.Descendants(ns + propertyName).FirstOrDefault();
        if (element != null && !string.IsNullOrWhiteSpace(element.Value))
        {
            propertyGroup.Add(new XElement(propertyName, element.Value));
        }
    }

    private void AddPropertyIfNotExists(XElement propertyGroup, string propertyName, XAttribute? condition, string value)
    {
        var newElement = new XElement(propertyName, value);
        if (condition != null)
        {
            newElement.Add(condition);
        }
        propertyGroup.Add(newElement);
    }

    private void PreservePropertyIfExists(XElement newGroup, XElement oldGroup, XNamespace ns, string propertyName)
    {
        var element = oldGroup.Elements(ns + propertyName).FirstOrDefault();
        if (element != null)
        {
            newGroup.Add(new XElement(propertyName, element.Value));
        }
    }

    private string ConvertFrameworkVersionCustomModern(string oldTfm)
    {
        // Handle variable tokens - preserve them verbatim
        if (oldTfm.StartsWith("$"))
        {
            return oldTfm;
        }

        // Map old-style versions to SDK-style
        // Examples: v4.5 → net45, v4.7.2 → net472
        var trimmed = oldTfm.Trim();

        if (trimmed.StartsWith("v", StringComparison.OrdinalIgnoreCase))
        {
            // Remove 'v' prefix and dots
            var version = trimmed.Substring(1).Replace(".", "");
            var newTfm = "net" + version;
            return newTfm;
        }

        // If it doesn't start with 'v', return as-is
        return trimmed;
    }

    private XElement CloneElementWithoutNamespace(XElement element)
    {
        var clone = new XElement(element.Name.LocalName);

        foreach (var attribute in element.Attributes())
        {
            if (!attribute.IsNamespaceDeclaration)
            {
                clone.SetAttributeValue(attribute.Name.LocalName, attribute.Value);
            }
        }

        foreach (var node in element.Nodes())
        {
            var clonedNode = CloneNodeWithoutNamespace(node);
            if (clonedNode != null)
            {
                clone.Add(clonedNode);
            }
        }

        return clone;
    }

    private XNode? CloneNodeWithoutNamespace(XNode node)
    {
        return node switch
        {
            XElement childElement => CloneElementWithoutNamespace(childElement),
            XCData cdata => new XCData(cdata.Value),
            XText text => new XText(text.Value),
            XComment comment => new XComment(comment.Value),
            _ => null
        };
    }

    private bool IsWinFormsInSdkProject(XElement root, XNamespace ns)
    {
        // Check for UseWindowsForms property
        var useWindowsForms = root.Descendants(ns + "UseWindowsForms").FirstOrDefault();
        if (useWindowsForms != null && useWindowsForms.Value.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    private string ConvertSdkToOldStyleFrameworkVersion(string sdkTfm)
    {
        // Handle variable tokens - preserve them verbatim
        if (sdkTfm.StartsWith("$"))
        {
            return sdkTfm;
        }

        // Map SDK-style versions to Old-style
        // Examples: net45 → v4.5, net472 → v4.7.2, net48-windows → v4.8
        var trimmed = sdkTfm.Trim().ToLowerInvariant();

        // Remove -windows suffix if present
        trimmed = trimmed.Replace("-windows", "");

        if (trimmed.StartsWith("net"))
        {
            // Remove 'net' prefix
            var version = trimmed.Substring(3);

            // Add dots back
            // net45 → 4.5, net472 → 4.7.2
            if (version.Length == 2)
            {
                // net40 → v4.0, net45 → v4.5
                return $"v{version[0]}.{version[1]}";
            }
            else if (version.Length == 3)
            {
                // net403 → v4.0.3, net451 → v4.5.1, net462 → v4.6.2, net472 → v4.7.2, net481 → v4.8.1
                return $"v{version[0]}.{version[1]}.{version[2]}";
            }
            else
            {
                // Fallback - return as-is
                return sdkTfm;
            }
        }

        // If it doesn't start with 'net', return as-is
        return trimmed;
    }

    private bool IsNetFrameworkTarget(string tfm)
    {
        if (tfm.StartsWith("$"))
            return true; // Assume variables are valid

        // Remove -windows suffix if present
        var baseTfm = tfm.Replace("-windows", "").ToLowerInvariant();

        // Check if it's in the range net40–net48
        var validTargets = new[]
        {
            "net40", "net403",
            "net45", "net451", "net452",
            "net46", "net461", "net462",
            "net47", "net471", "net472",
            "net48", "net481"
        };

        return validTargets.Contains(baseTfm);
    }

    private string AppendTfmValue(string currentTfms, string appendValue, bool isWinForms)
    {
        // Parse the append value to extract individual TFMs
        var appendTokens = ParseTfmTokens(appendValue);

        // Parse existing TFMs to extract individual tokens
        var existingTokens = ParseTfmTokens(currentTfms);

        // Combine and deduplicate
        var allTokens = new List<TfmToken>();
        allTokens.AddRange(existingTokens);

        foreach (var appendToken in appendTokens)
        {
            // Apply WinForms autocorrection for net5+
            var processedToken = appendToken;
            if (isWinForms && !processedToken.IsVariable)
            {
                processedToken.Value = ApplyWinFormsAutocorrection(processedToken.Value);
            }

            bool isDuplicate = false;

            foreach (var existingToken in allTokens)
            {
                if (AreTfmTokensEqual(processedToken, existingToken))
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                allTokens.Add(processedToken);
            }
        }

        // Sort tokens: variables first (in original order), then literals (sorted)
        var variables = allTokens.Where(t => t.IsVariable).ToList();
        var literals = allTokens.Where(t => !t.IsVariable)
                                .OrderBy(t => GetSortOrder(t.Value))
                                .ThenBy(t => t.Value.ToLowerInvariant())
                                .ToList();

        var sortedTokens = variables.Concat(literals).ToList();

        // Join tokens back into a semicolon-separated string
        return string.Join(";", sortedTokens.Select(t => t.Value));
    }

    private bool IsNetFrameworkToken(string value)
    {
        var lower = value.ToLowerInvariant();
        // Check if it looks like a .NET Framework version (net followed by digits and optional decimals)
        return lower.StartsWith("net") &&
               lower.Length > 3 &&
               char.IsDigit(lower[3]) &&
               !lower.Contains("-"); // Exclude net6.0-windows style
    }

    private string ApplyWinFormsAutocorrection(string tfm)
    {
        var lower = tfm.ToLowerInvariant();

        // Common .NET Framework versions that should have -windows suffix for WinForms
        // We'll add -windows if it's a modern .NET version (net5.0+) without existing platform suffix
        if (lower.StartsWith("net") && !lower.Contains("-"))
        {
            // Extract version number
            var versionPart = lower.Substring(3);

            // Check if it's a modern .NET version (5.0 or higher)
            if (versionPart.StartsWith("5") || versionPart.StartsWith("6") ||
                versionPart.StartsWith("7") || versionPart.StartsWith("8") ||
                versionPart.StartsWith("9"))
            {
                // Add -windows suffix for WinForms projects
                return tfm + "-windows";
            }
        }

        return tfm;
    }

    private List<TfmToken> ParseTfmTokens(string tfmValue)
    {
        var tokens = new List<TfmToken>();

        if (string.IsNullOrWhiteSpace(tfmValue))
        {
            return tokens;
        }

        var parts = tfmValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(p => p.Trim())
                           .Where(p => !string.IsNullOrEmpty(p));

        foreach (var part in parts)
        {
            var token = new TfmToken
            {
                Value = part,
                IsVariable = part.StartsWith("$")
            };

            tokens.Add(token);
        }

        return tokens;
    }

    private bool AreTfmTokensEqual(TfmToken token1, TfmToken token2)
    {
        // Variables require exact match (case-sensitive)
        if (token1.IsVariable && token2.IsVariable)
        {
            return token1.Value == token2.Value;
        }

        // If one is variable and other is not, they're not equal
        if (token1.IsVariable != token2.IsVariable)
        {
            return false;
        }

        // Literals are case-insensitive
        return string.Equals(token1.Value, token2.Value, StringComparison.OrdinalIgnoreCase);
    }

    private int GetSortOrder(string tfm)
    {
        var lower = tfm.ToLowerInvariant();

        // Extract version number for sorting
        // Priority: newer versions first
        if (lower.StartsWith("net"))
        {
            var versionPart = lower.Substring(3);

            // Try to parse version number
            if (versionPart.Length > 0 && char.IsDigit(versionPart[0]))
            {
                // Extract numeric part
                var numericPart = "";
                foreach (var ch in versionPart)
                {
                    if (char.IsDigit(ch) || ch == '.')
                    {
                        numericPart += ch;
                    }
                    else
                    {
                        break;
                    }
                }

                if (double.TryParse(numericPart, out var version))
                {
                    // Return negative to sort descending (newer first)
                    return -(int)(version * 10);
                }
            }
        }

        return 0;
    }

    private class TfmToken
    {
        public string Value { get; set; } = "";
        public bool IsVariable { get; set; }
    }

    private bool IsFrameworkReference(string referenceName)
    {
        // Common .NET Framework references that are automatically included by SDK
        var frameworkRefs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "System",
            "System.Core",
            "System.Data",
            "System.Data.DataSetExtensions",
            "System.Xml",
            "System.Xml.Linq",
            "Microsoft.CSharp",
            "System.Net.Http",
            "mscorlib",
            // WinForms references (handled by UseWindowsForms=true)
            "System.Drawing",
            "System.Windows.Forms",
            "System.Deployment",
            // WPF references (handled by UseWPF=true)
            "PresentationCore",
            "PresentationFramework",
            "WindowsBase",
            "System.Xaml",
            "UIAutomationProvider",
            "UIAutomationTypes",
            "ReachFramework",
            // Other common framework assemblies
            "System.Configuration",
            "System.Runtime.Serialization",
            "System.ServiceModel",
            "System.Transactions",
            "System.Web",
            "System.Web.Services"
        };

        // Extract the simple name (before comma for versioned references)
        var simpleName = referenceName.Split(',')[0].Trim();
        return frameworkRefs.Contains(simpleName);
    }

    #endregion
}
