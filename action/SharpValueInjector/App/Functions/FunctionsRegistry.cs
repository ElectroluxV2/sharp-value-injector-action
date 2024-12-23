using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace SharpValueInjector.App.Functions;

public static class FunctionsRegistry
{
    private const string IocPrefix = "function";

    public static IServiceCollection AddFunctions(this IServiceCollection serviceCollection) =>
        serviceCollection
            .AddFunction<ToBase64Function>("ToBase64");

    private static IServiceCollection AddFunction<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFunction>(
        this IServiceCollection services,
        string identifier
    ) where TFunction : class, IFunction =>
        services.AddKeyedTransient<IFunction, TFunction>(GetKeyedServiceName(identifier));

    public static IFunction GetFunction(this IServiceProvider serviceProvider, string identifier)
    {
        return serviceProvider.GetKeyedService<IFunction>(GetKeyedServiceName(identifier)) ?? throw new NotImplementedException($"Function '{identifier}' not found");
    }

    private static string GetKeyedServiceName(string identifier) => $"{IocPrefix}-{identifier.ToLowerInvariant()}";
}