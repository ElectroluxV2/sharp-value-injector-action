using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SharpValueInjector.App.Functions;

public static class FunctionsRegistry
{
    private const string IocPrefix = "function-";

    public static IServiceCollection AddFunctions(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddFunction<Base64Function>("base64");

    private static IServiceCollection AddFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TService>(
        this IServiceCollection services,
        string identifier
    ) where TService : class, IFunction =>
        services.AddKeyedTransient<TService>($"{IocPrefix}-{identifier}");

    public static T GetFunction<T>(this IServiceProvider serviceProvider, string identifier) where T : class, IFunction =>
        serviceProvider.GetRequiredKeyedService<T>($"{IocPrefix}-{identifier}");
}