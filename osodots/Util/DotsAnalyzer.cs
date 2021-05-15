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
        private static readonly string[] ExcludeFolders = new string[] { "Editor", "Library" };

        public static async Task<Dictionary<string, DotsProperties>> AnalyzeDirectoryAsync(DirectoryInfo workspace, CancellationToken cancellationToken, string outFile = null)
        {
            if (workspace != null)
            {   
                var analyzerResult = await AnalyzeWorkspaceInternalAsync(workspace, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                // this.analyzerCache.SetData(analyzerResult);
                if (outFile != null)
                {
                    var jsonData = JsonConvert.SerializeObject(analyzerResult);
                    await File.WriteAllTextAsync(outFile, jsonData);
                }
                return analyzerResult;
            }
            return null;
        }

        public static async Task<Dictionary<string, DotsProperties>> AnalyzeFilesAsync(FileInfo[] files, CancellationToken cancellationToken)
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
            return analyzerResult;
        }

        private static DirectoryInfo[] GetValidDirectoriesInRoot(DirectoryInfo root)
        {
            return root.EnumerateDirectories("*", SearchOption.AllDirectories).Where(d => !ExcludeFolders.Contains(d.Name) && d.EnumerateFiles("*.cs", SearchOption.TopDirectoryOnly).Any()).ToArray();
        }

        private static async Task<Dictionary<string, DotsProperties>> AnalyzeWorkspaceInternalAsync(DirectoryInfo workspace, CancellationToken cancellationToken)
        {
            var analyzerResult = new Dictionary<string, DotsProperties>();
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
            var result = GetDotsPropertiesFromFile(root, file.FullName, cancellationToken);
            return result;
        }

        private static DotsProperties GetDotsPropertiesFromFile(SyntaxNode root, string filePath, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var entityArchetypeExpressions = root.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(d => d.Expression.ToString().EndsWith(DotsConstants.CreateEntityArchetype)).ToList();

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
                            FileKey = filePath,
                            TypeName = typeName
                        };
                        authoringComponent.LoadFrom(classDeclaration, attributes);
                        authoringComponents.Add(authoringComponent);
                        break;

                    case ClassAnalysisType.System:
                        var system = new SystemClassMetadata
                        {
                            Group = DotsConstants.SimulationSystemGroup,
                            FileKey = filePath,
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
                            FileKey = filePath,
                            TypeName = typeName,
                        };

                        componentSystemGroup.LoadFrom(classDeclaration, attributes);
                        systemGroups.Add(componentSystemGroup);
                        break;

                    case ClassAnalysisType.Component:
                        var component = new ComponentMetaData
                        {
                            FileKey = filePath,
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
                FriendlyName = Path.GetFileNameWithoutExtension(filePath),
                AuthoringComponents = authoringComponents.ToDictionary(i => i.Name),
                Components = components.ToDictionary(i => i.Name),
                SystemGroups = systemGroups.ToDictionary(i => i.Name),
                Systems = systems.ToDictionary(i => i.Name)
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
        public const string OrderLast = nameof(OrderLast);
        public const string OrderFirst = nameof(OrderFirst);
        public const string CreateEntityArchetype = nameof(CreateEntityArchetype);
    }
}
