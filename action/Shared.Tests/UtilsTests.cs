namespace Shared.Tests;

using static Utils;

public class UtilsTests
{
    [Test]
    [Arguments(0L, "0B")]
    [Arguments(1_024L, "1KiB")]
    [Arguments(1_024L * 1_024, "1MiB")]
    [Arguments(1_024L * 1_024 * 1_024, "1GiB")]
    [Arguments(1_024L * 1_024 * 1_024 * 1_024, "1TiB")]
    [Arguments(1_024L * 1_024 * 1_024 * 1_024 * 1_024, "1PiB")]
    [Arguments(1_024L * 1_024 * 1_024 * 1_024 * 1_024 * 1_024, "1EiB")]
    [Arguments(5_823_996_738L, "5.42GiB")]
    [Arguments(long.MaxValue, "8EiB")]
    [Arguments(long.MinValue + 1, "-8EiB")]
    public async Task Should_convert_bytes_to_human_string(long bytes, string expected)
    {
        await Assert.That(BytesToString(bytes)).IsEqualTo(expected);
    }
}