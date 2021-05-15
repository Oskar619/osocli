using Microsoft.Extensions.DependencyInjection;
using osocli;
using osodots.Util;
using System;
using System.IO;
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
            var analyzer = scope.ServiceProvider.GetService<DotsAnalyzer>();
            if(commandOptions.Workspace == null)
            {
                Console.Write("Error: Workspace not specified.");
                return -1;
            }
            if (commandOptions.Files != null)
            {
                await analyzer.AnalyzeFilesAsync(commandOptions.Workspace.FullName, commandOptions.Files, cancellationToken);
            }
            else
            {
                Console.WriteLine($"analyzing workspace: {commandOptions.Workspace.FullName}");
                await analyzer.AnalyzeWorkspaceAsync(commandOptions.Workspace, cancellationToken, commandOptions.OutputFile?.FullName);
            }
            return 0;
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<DotsAnalyzer>();
            collection.AddSingleton<DotsAnalyzerCache>();
        }
    }

    public class AnalyzeCommandOptions : WorkspaceOptions
    {
        public FileInfo[] Files { get; set; }
        public FileInfo OutputFile { get; set; }
    }

    public class WorkspaceOptions
    {
        public DirectoryInfo Workspace { get; set; }
    }
}
