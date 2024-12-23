using System.Text;

namespace SharpValueInjector.App.Functions;

public record ToBase64Function : IFunction
{
    public ValueTask<bool> TryExecuteAsync(string input, out string output)
    {
        output = Convert.ToBase64String(Encoding.UTF8.GetBytes(input), Base64FormattingOptions.None);
        return ValueTask.FromResult(true);
    }
}
