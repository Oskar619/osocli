
using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace osocli
{
    public abstract class BaseCommand : Command
    {
        protected ServiceProvider ServiceProvider;

        protected readonly CancellationTokenSource CancellationTokenSource;

        public BaseCommand(string name, string description)
            : base(name, description)
        {
            CancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Allows to override service configuration by specific command.
        /// </summary>
        /// <param name="collection">A service collection.</param>
        protected virtual void ConfigureServices(IServiceCollection collection) { }

        protected void InitializeServices<TOptions>(TOptions options)
            where TOptions : class
        {
            if (ServiceProvider == null)
            {
                IServiceCollection collection = new ServiceCollection();
                collection.AddSingleton(new OptionsReference(options));
                collection.AddOptions<TOptions>();
                ConfigureServices(collection);
                ServiceProvider = collection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// Creates an option for the command.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available name for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <param name="aliases">Define extra aliases for the command.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateOption<T>(string name, string description, string[] aliases = null)
        {
            var option = new Option(name, description)
            {
                Argument = new Argument
                {
                    ArgumentType = typeof(T),
                }
            };

            if(aliases != null)
            {
                foreach(var alias in aliases)
                {
                    option.AddAlias(alias);
                }
            }
            return option;
        }

        /// <summary>
        /// Creates a hidden option for the command.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available names for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <param name="aliases">Define extra aliases for the command.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateHiddenOption<T>(string name, string description, string[] aliases = null)
        {
            var option = CreateOption<T>(name, description, aliases);
            option.Argument.IsHidden = true;
            option.IsHidden = true;
            return option;
        }

        /// <summary>
        /// Creates an option for the command with a default value.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available name for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="aliases">Define extra aliases for the command.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateOptionWithDefault<T>(string name, string description, T defaultValue, string[] aliases = null)
        {
            var option = new Option(name, description);
            option.Argument = new Argument
            {
                ArgumentType = typeof(T),
            };
            option.Argument.SetDefaultValue(defaultValue);
            if (aliases != null)
            {
                foreach (var alias in aliases)
                {
                    option.AddAlias(alias);
                }
            }
            return option;
        }

        /// <summary>
        /// Creates a hidden option for the command with a default value.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available name for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <param name="aliases">Define extra aliases for the command.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateHiddenOptionWithDefault<T>(string name, string description, T defaultValue, string[] aliases = null)
        {
            var option = CreateOptionWithDefault(name, description, defaultValue, aliases);
            option.Argument.IsHidden = true;
            option.IsHidden = true;
            return option;
        }
    }

    public abstract class BaseCommand<TOptions> : BaseCommand
        where TOptions : class
    {


        public BaseCommand(string name, string description)
            : base(name, description)
        {
            Handler = CommandHandler.Create<TOptions>(TaskHandler);
        }

        /// <summary>
        /// The action handler for the command.
        /// </summary>
        /// <param name="scope">The service scope with all the dependencies.</param>
        /// <returns>The awaitable task.</returns>
        public abstract Task<int> HandleAction(IServiceScope scope, TOptions commandOptions, ILogger logger, CancellationToken cancellationToken);

        private async Task<int> TaskHandler(TOptions options)
        {
            InitializeServices(options);
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetService<ILogger>();
                var result = await HandleAction(scope, options, logger, CancellationTokenSource.Token);
                return result;
            }
            finally
            {
                CancellationTokenSource.Cancel();
            }
        }
    }
}
