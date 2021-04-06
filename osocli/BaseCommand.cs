using Microsoft.Extensions.DependencyInjection;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading;
using System.Threading.Tasks;

namespace osocli
{
    public abstract class BaseCommand<TOptions> : Command
        where TOptions : class
    {
        protected ServiceProvider ServiceProvider;

        private readonly CancellationTokenSource cancellationTokenSource;

        public BaseCommand(string name, string description)
            : base(name, description)
        {
            cancellationTokenSource = new CancellationTokenSource();
            Handler = CommandHandler.Create<TOptions>(TaskHandler);
        }

        /// <summary>
        /// Allows to override service configuration by specific command.
        /// </summary>
        /// <param name="collection">A service collection.</param>
        internal virtual void ConfigureServices(ServiceCollection collection) { }

        private void InitializeServices()
        {
            if (ServiceProvider == null)
            {
                var collection = new ServiceCollection();
                ConfigureServices(collection);
                ServiceProvider = collection.BuildServiceProvider();
            }
        }

        /// <summary>
        /// The action handler for the command.
        /// </summary>
        /// <param name="scope">The service scope with all the dependencies.</param>
        /// <returns>The awaitable task.</returns>
        internal abstract Task<int> HandleAction(IServiceScope scope, TOptions commandOptions, ILogger logger, CancellationToken cancellationToken);

        private async Task<int> TaskHandler(TOptions options)
        {
            InitializeServices();
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var logger = scope.ServiceProvider.GetService<ILogger>();
                var result = await HandleAction(scope, options, logger, cancellationTokenSource.Token);
                return result;
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// Creates an option for the command.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available name for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateOption<T>(string name, string description)
        {
            var option = new Option(name, description)
            {
                Argument = new Argument
                {
                    ArgumentType = typeof(T),
                }
            };
            return option;
        }

        /// <summary>
        /// Creates a hidden option for the command.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available names for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateHiddenOption<T>(string name, string description)
        {
            var option = CreateOption<T>(name, description);
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
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateOptionWithDefault<T>(string name, string description, T defaultValue)
        {
            var option = new Option(name, description);
            option.Argument = new Argument
            {
                ArgumentType = typeof(T),
            };
            option.Argument.SetDefaultValue(defaultValue);
            return option;
        }

        /// <summary>
        /// Creates a hidden option for the command with a default value.
        /// </summary>
        /// <typeparam name="T">The argument type for the option.</typeparam>
        /// <param name="name">The available name for this command option.</param>
        /// <param name="description">Description of the option.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>Returns an option created from the given values.</returns>
        protected static Option CreateHiddenOptionWithDefault<T>(string name, string description, T defaultValue)
        {
            var option = CreateOptionWithDefault(name, description, defaultValue);
            option.Argument.IsHidden = true;
            option.IsHidden = true;
            return option;
        }
    }
}
