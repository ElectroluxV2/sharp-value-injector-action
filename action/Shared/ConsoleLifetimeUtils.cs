using System.Runtime.InteropServices;

namespace Shared;

public static class ConsoleLifetimeUtils
{
    private static readonly CancellationTokenSource CancellationTokenSource = new();
    
    public static CancellationToken CreateConsoleLifetimeBoundCancellationToken()
    {
        PosixSignalRegistration.Create(PosixSignal.SIGINT, HandlePosixSignal);
        PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandlePosixSignal);
        PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandlePosixSignal);
        
        return CancellationTokenSource.Token;
    }
    
    private static void HandlePosixSignal(PosixSignalContext context)
    {
        context.Cancel = true; // Prevents the application from getting killed by OS
        CancellationTokenSource.Cancel(); // Forward the cancellation to the application
    }
}