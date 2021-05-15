using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace osocli
{
    public interface IOptionsProvider<TOptions>
        where TOptions : class
    {
        public TOptions Value { get; }
    }

    public class OptionsProvider<TOptions> : IOptionsProvider<TOptions>
        where TOptions : class
    {
        public Lazy<TOptions> Options;
        public OptionsProvider(OptionsReference options)
        {
            Options = new Lazy<TOptions>(() =>
            {
                return options.Options as TOptions;
            });
        }

        TOptions IOptionsProvider<TOptions>.Value => Options.Value;
    }

    public class OptionsReference
    {
        public object Options;
        public OptionsReference(object options)
        {
            Options = options;
        }
    }

    public static class Extensions
    {
        public static IServiceCollection AddOptions<TOptions>(this IServiceCollection collection)
            where TOptions : class
        {
            collection.TryAddSingleton<IOptionsProvider<TOptions>, OptionsProvider<TOptions>>();
            return collection;
        }
    }
}
