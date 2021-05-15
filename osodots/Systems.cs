using Microsoft.Extensions.DependencyInjection;
using osocli;
using osodots.Util;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace osodots
{
    public class Systems : BaseCommand<WorkspaceOptions>
    {
        public const string CommandName = "systems";
        public const string CommandDescription = "Displays the systems info. Can be queried by component, group, etc.";
        public Systems()
            : base(CommandName, CommandDescription)
        {
            AddOption(CreateOption<DirectoryInfo>("--workspace", "Directory to analyze.", new string[] { "-w" }));
        }

        public override async Task<int> HandleAction(IServiceScope scope, WorkspaceOptions commandOptions, ILogger logger, CancellationToken cancellationToken)
        {
            var analyzer = scope.ServiceProvider.GetService<DotsAnalyzer>();
            await analyzer.AnalyzeWorkspaceIfNoCacheAvailableAsync(commandOptions.Workspace, cancellationToken);
            return 0;
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<DotsAnalyzer>();
            collection.AddSingleton<DotsAnalyzerCache>();
        }
    }
}
