namespace SharpValueInjector.App.Injections;

public interface IInjection
{
    /// <summary>
    /// This method can be called multiple times, so it should be idempotent.
    /// </summary>
    /// <returns>Value that is going to be used when injecting to the output file or evaluating function result</returns>
    public ValueTask<string> ProvisionInjectionValueAsync();

    /// <summary>
    ///   If the injection value should not be logged, this method should return some replacement.
    /// </summary>
    /// <returns>Value that is going to be used when logging</returns>
    public ValueTask<string> ProvisionLogValueAsync() => ProvisionInjectionValueAsync();
}
