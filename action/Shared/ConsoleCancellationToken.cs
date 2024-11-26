namespace Shared;

public record ConsoleCancellationToken(CancellationToken Token)
{
    public static implicit operator CancellationToken(ConsoleCancellationToken wrapper) => wrapper.Token;
}