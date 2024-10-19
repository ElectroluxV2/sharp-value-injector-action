using System.Globalization;

namespace SharpValueInjector.Shared;

public static class Utils
{
    public static string BytesToString(long value)
    {
        var (suffix, readable) = Math.Abs(value) switch
        {
            >= 0x1000000000000000 => (suffix: "EiB", readable: value >> 50),
            >= 0x4000000000000 => (suffix: "PiB", readable: value >> 40),
            >= 0x10000000000 => (suffix: "TiB", readable: value >> 30),
            >= 0x40000000 => (suffix: "GiB", readable: value >> 20),
            >= 0x100000 => (suffix: "MiB", readable: value >> 10),
            >= 0x400 => (suffix: "KiB", readable: value),
            _ => (suffix: "B", readable: value * 1024), // Multiplying by value we divide later
        };

        return ((double) readable / 1024).ToString($"0.##{suffix}", CultureInfo.InvariantCulture);
    }
}