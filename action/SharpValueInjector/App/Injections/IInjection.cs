namespace SharpValueInjector.App.Injections;

public interface IInjection
{
    /// <remarks>
    /// This method MUST be awaited only once
    /// </remarks>
    /// <returns>Value that is going to be used when injecting to the output file or evaluating function result</returns>
    public ValueTask<string> ProvisionInjectionValueAsync(CancellationToken cancellationToken = default);

    /// <remarks>
    /// This method MUST be awaited only once
    /// </remarks>
    /// <summary>
    ///   If the injection value should not be logged, this method should return some replacement.
    /// </summary>
    /// <returns>Value that is going to be used when logging</returns>
    public ValueTask<string> ProvisionLogValueAsync(CancellationToken cancellationToken = default) => ProvisionInjectionValueAsync(cancellationToken);

    public bool SupportsExpressions => true;
}
