namespace SharpValueInjector.App.Functions;

public interface IFunction
{
    public ValueTask<bool> TryExecuteAsync(string input, out string output);
}
