using Microsoft.Extensions.DependencyInjection;
using osocli;
using osodots.Util;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osodots
{
    public class Analyze : BaseCommand<AnalyzeCommandOptions>
    {
        public const string CommandName = "analyze";
        public const string CommandDescription = "Analyzes a directory for DOTS information.";
        public Analyze()
            : base(CommandName, CommandDescription)
        {
            AddOption(CreateOption<DirectoryInfo>("--workspace", "Directory to analyze.", new string[] { "-w" }));
            AddOption(CreateOption<FileInfo[]>("--files", "Files to analyze", new string[] { "-f" }));
            AddOption(CreateOption<FileInfo>("--output-file", "OutputFile to Analyze", new string[] { "-o" }));
        }

        public override async Task<int> HandleAction(IServiceScope scope, AnalyzeCommandOptions commandOptions, ILogger logger, CancellationToken cancellationToken)
        {
            var cache = scope.ServiceProvider.GetService<DotsAnalyzerCache>();
            cache.SetCacheId(commandOptions.GetAssetsFolder());
            if(commandOptions.Workspace != null)
            {
                var assetsFolder = commandOptions.Workspace;
                if (assetsFolder.Name != "Assets")
                {
                    assetsFolder = assetsFolder.EnumerateDirectories("Assets", SearchOption.AllDirectories).FirstOrDefault();
                }

                if (assetsFolder == null)
                {
                    throw new InvalidOperationException($"Could not find the Assets Folder in the given workspace directory. If you wish to get data from files not attached to a Unity Project, send the files in the arguments by using the -f argument.");
                }

                Console.WriteLine($"analyzing workspace: {assetsFolder.FullName}");
                var result = await DotsAnalyzer.AnalyzeDirectoryAsync(assetsFolder, cancellationToken, commandOptions.OutputFile?.FullName);
                if (cache.IsEnabled)
                {
                    cache.SetData(result);
                }
            }
            else if (commandOptions.Files != null)
            {
                var analyzerResult = await DotsAnalyzer.AnalyzeFilesAsync(commandOptions.Files, cancellationToken);

                if (cache.IsEnabled)
                {
                    cache.AppendData(analyzerResult);
                }
            }
            else
            {
                throw new Exception($"Please provide either a workspace (-w) or a file(s) (-f)");
            }
            return 0;
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<DotsAnalyzerCache>();
        }

        
    }

    public class AnalyzeCommandOptions : WorkspaceOptions
    {
        public FileInfo[] Files { get; set; }
        public FileInfo OutputFile { get; set; }
        public override string GetAssetsFolder()
        {
            if(Workspace != null)
            {
                return base.GetAssetsFolder();
            }
            else
            {
                return GetAssetsFolderFromFile(Files);
            }
        }
        private static string GetAssetsFolderFromFile(FileInfo[] files)
        {
            var fileDirectory = files.FirstOrDefault(f => f.DirectoryName.Contains("\\Assets\\"))?.Directory;
            while (fileDirectory != null && fileDirectory.Parent.Name != "Assets")
            {
                fileDirectory = fileDirectory.Parent;
            }
            if (fileDirectory != null)
            {
                return fileDirectory.FullName;
            }
            return null;
        }
    }

    public class WorkspaceOptions
    {
        public DirectoryInfo Workspace { get; set; }

        public virtual string GetAssetsFolder()
        {
            var assetsFolder = Workspace;
            if (assetsFolder.Name != "Assets")
            {
                assetsFolder = assetsFolder.EnumerateDirectories("Assets", SearchOption.AllDirectories).FirstOrDefault();
            }

            if (assetsFolder == null)
            {
                throw new InvalidOperationException($"Could not find the Assets Folder in the given workspace directory. If you wish to get data from files not attached to a Unity Project, send the files in the arguments by using the -f argument.");
            }
            return assetsFolder.FullName;
        }
    }
}
