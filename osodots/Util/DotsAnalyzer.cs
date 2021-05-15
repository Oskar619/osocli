using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using osodots.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osodots.Util
{
    public class DotsAnalyzer
    {
        private readonly DotsAnalyzerCache analyzerCache;

        private static string[] ExcludeFolders = new string[] { "Editor", "Library" };

        public DotsAnalyzer(DotsAnalyzerCache cache)
        {
            this.analyzerCache = cache;
        }

        public async Task AnalyzeWorkspaceAsync(DirectoryInfo workspace, CancellationToken cancellationToken, string outFile = null)
        {
            if (workspace != null)
            {
                var analyzerResult = await AnalyzeWorkspaceInternalAsync(workspace, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                this.analyzerCache.SetAnalyzedData(workspace.FullName, analyzerResult);
                if (outFile != null)
                {
                    var jsonData = JsonConvert.SerializeObject(analyzerResult);
                    await File.WriteAllTextAsync(outFile, jsonData);
                }
            }
        }

        public async Task AnalyzeFilesAsync(string workspacePath, FileInfo[] files, CancellationToken cancellationToken)
        {
            var analyzerResult = new Dictionary<string, DotsProperties>();

            foreach (var file in files)
            {
                var result = await AnalyzeFileAsync(file, cancellationToken);
                if (result != null && result.HasAnyDotsData)
                {
                    if (!analyzerResult.ContainsKey(file.FullName))
                    {
                        analyzerResult.Add(file.FullName, result);
                    }
                }
            }

            if (analyzerResult.Any())
            {
                this.analyzerCache.AppendAnalyzedData(workspacePath, analyzerResult);
            }
        }

        public async Task AnalyzeWorkspaceIfNoCacheAvailableAsync(DirectoryInfo workspace, CancellationToken cancellationToken)
        {
            if (!analyzerCache.IsCached(workspace.FullName) && workspace != null)
            {
                return;
            }

            if (workspace != null)
            {
                var result = await AnalyzeWorkspaceInternalAsync(workspace, cancellationToken);
                if (result != null)
                {
                    this.analyzerCache.SetAnalyzedData(workspace.FullName, result);
                }
            }
        }

        private static DirectoryInfo[] GetValidDirectoriesInRoot(DirectoryInfo root)
        {
            return root.EnumerateDirectories("*", SearchOption.AllDirectories).Where(d => !ExcludeFolders.Contains(d.Name) && d.EnumerateFiles("*.cs", SearchOption.TopDirectoryOnly).Any()).ToArray();
        }

        private static async Task<Dictionary<string, DotsProperties>> AnalyzeWorkspaceInternalAsync(DirectoryInfo workspace, CancellationToken cancellationToken)
        {
            var analyzerResult = new Dictionary<string, DotsProperties>();
            // var vworkspace = workspace as VisualStudioWorkspace;
            var validDirectories = GetValidDirectoriesInRoot(workspace);
            foreach (var dir in validDirectories)
            {
                foreach (var csharpFile in dir.EnumerateFiles("*.cs", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var result = await AnalyzeFileAsync(csharpFile, cancellationToken);
                    if (result != null && result.HasAnyDotsData)
                    {
                        if (!analyzerResult.ContainsKey(csharpFile.FullName))
                        {
                            analyzerResult.Add(csharpFile.FullName, result);
                        }
                    }
                }
            }

            return analyzerResult;
        }

        private static async Task<DotsProperties> AnalyzeFileAsync(FileInfo file, CancellationToken cancellationToken)
        {
            if (!file.Exists || file.Extension.ToLower() != ".cs")
            {
                return null;
            }

            var csharpText = await File.ReadAllTextAsync(file.FullName, cancellationToken: cancellationToken);
            var tree = CSharpSyntaxTree.ParseText(csharpText, cancellationToken: cancellationToken);
            var root = tree.GetCompilationUnitRoot(cancellationToken);
            var result = Analyze(root, file.FullName, cancellationToken);
            return result;
        }

        private static DotsProperties Analyze(SyntaxNode root, string filePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var systemGroups = new List<SystemGroupClassMetadata>();
            var components = new List<ComponentMetaData>();
            var systems = new List<SystemClassMetadata>();
            var authoringComponents = new List<AuthoringComponentMetadata>();
            var systemGroupLink = new Dictionary<string, List<string>>();

            foreach (var classDeclaration in classDeclarations)
            {
                var type = GetClassAnalysisTypeFromDeclaration(classDeclaration, out var typeName);
                var attributes = classDeclaration.DescendantNodes().OfType<AttributeListSyntax>().FirstOrDefault();
                switch (type)
                {
                    case ClassAnalysisType.Authoring:
                        var authoringComponent = new AuthoringComponentMetadata
                        {
                            Path = filePath,
                            TypeName = typeName
                        };
                        authoringComponent.LoadFrom(classDeclaration, attributes);
                        authoringComponents.Add(authoringComponent);
                        break;

                    case ClassAnalysisType.System:
                        var system = new SystemClassMetadata
                        {
                            Group = DotsConstants.SimulationSystemGroup,
                            Path = filePath,
                            TypeName = typeName
                        };

                        system.LoadFrom(classDeclaration, attributes);
                        if (!string.IsNullOrWhiteSpace(system.Group) && system.Group != DotsConstants.SimulationSystemGroup)
                        {
                            if (!systemGroupLink.TryGetValue(system.Group, out var systemsInGroup))
                            {
                                systemsInGroup = new List<string>();
                                systemGroupLink.Add(system.Group, systemsInGroup);
                            }

                            systemsInGroup.Add(system.Name);
                        }

                        systems.Add(system);
                        break;

                    case ClassAnalysisType.SystemGroup:
                        var componentSystemGroup = new SystemGroupClassMetadata
                        {
                            Path = filePath,
                            TypeName = typeName,
                        };

                        componentSystemGroup.LoadFrom(classDeclaration, attributes);
                        systemGroups.Add(componentSystemGroup);
                        break;

                    case ClassAnalysisType.Component:
                        var component = new ComponentMetaData
                        {
                            Path = filePath,
                            TypeName = typeName
                        };

                        component.LoadFrom(classDeclaration, attributes);
                        components.Add(component);
                        break;
                }

                foreach (var group in systemGroups)
                {
                    if (systemGroupLink.TryGetValue(group.Name, out var systemsInGroup))
                    {
                        group.Systems = systemsInGroup.ToArray();
                    }
                }
            }

            return new DotsProperties
            {
                AuthoringComponents = authoringComponents.ToArray(),
                Components = components.ToArray(),
                SystemGroups = systemGroups.ToArray(),
                Systems = systems.ToArray()
            };
        }

        private static ClassAnalysisType GetClassAnalysisTypeFromDeclaration(ClassDeclarationSyntax declaration, out string typeName)
        {
            var types = declaration.BaseList?.Types;
            typeName = DotsConstants.NonSet;
            if (types != null)
            {
                foreach (var baseType in types)
                {
                    typeName = baseType.Type.ToString();
                    if (typeName.Equals(DotsConstants.IConvertGameObjectToEntity))
                    {
                        return ClassAnalysisType.Authoring;
                    }

                    if (typeName.Equals(DotsConstants.ComponentSystemGroup))
                    {
                        return ClassAnalysisType.SystemGroup;
                    }

                    if (typeName.Equals(DotsConstants.ComponentSystem) || typeName.Equals(DotsConstants.SystemBase))
                    {
                        return ClassAnalysisType.System;
                    }

                    if (typeName.Equals(DotsConstants.IComponentData) || typeName.Equals(DotsConstants.IBufferElementData))
                    {
                        return ClassAnalysisType.Component;
                    }
                }
            }
            return ClassAnalysisType.NotSupported;
        }
    }

    public static class DotsConstants
    {
        public const string SimulationSystemGroup = nameof(SimulationSystemGroup);
        public const string UpdateBefore = nameof(UpdateBefore);
        public const string UpdateAfter = nameof(UpdateAfter);
        public const string UpdateInGroup = nameof(UpdateInGroup);
        public const string SystemBase = nameof(SystemBase);
        public const string ComponentSystem = nameof(ComponentSystem);
        public const string IComponentData = nameof(IComponentData);
        public const string IBufferElementData = nameof(IBufferElementData);
        public const string ComponentSystemGroup = nameof(ComponentSystemGroup);
        public const string IConvertGameObjectToEntity = nameof(IConvertGameObjectToEntity);
        public const string NonSet = nameof(NonSet);
        public const string EntitiesForeach = "Entities.ForEach";
        public const string Convert = nameof(Convert);
    }
}
