using Microsoft.Extensions.DependencyInjection;
using osocli;
using osodots.Util;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace osodots
{
    public class Initialize : BaseCommand<WorkspaceOptions>
    {
        public const string CommandName = "init";
        public const string CommandDescription = "Initializes a DOTS workspace. This will analyze the workspace if it hasn't been analyzed yet.";
        public Initialize()
            : base(CommandName, CommandDescription)
        {
            AddOption(CreateOption<DirectoryInfo>("--workspace", "Directory to analyze.", new string[] { "-w" }));
        }

        public override async Task<int> HandleAction(IServiceScope scope, WorkspaceOptions commandOptions, ILogger logger, CancellationToken cancellationToken)
        {
            var cacheData = scope.ServiceProvider.GetService<DotsAnalyzerCache>();
            var assetsFolder = commandOptions.GetAssetsFolder();
            cacheData.SetCacheId(assetsFolder);

            if(cacheData.Analysis == null)
            {
                var workspace = new DirectoryInfo(assetsFolder);
                var result = await DotsAnalyzer.AnalyzeDirectoryAsync(workspace, cancellationToken);
                cacheData.SetData(result);
            }
            return 0;
        }

        protected override void ConfigureServices(IServiceCollection collection)
        {
            collection.AddSingleton<DotsAnalyzerCache>();
        }
    }
}
