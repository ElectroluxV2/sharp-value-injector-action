using SharpValueInjector.Shared;

namespace SharpValueInjector.Tests;

using static Utils;

public class UtilsTests
{
    [Test]
    public async Task Should_convert_bytes_to_human_string()
    {
        await Assert.That(BytesToString(0L)).IsEqualTo("0B");
        await Assert.That(BytesToString(1_024L)).IsEqualTo("1KiB");
        await Assert.That(BytesToString(1_024L * 1_024)).IsEqualTo("1MiB");
        await Assert.That(BytesToString(1_024L * 1_024 * 1_024)).IsEqualTo("1GiB");
        await Assert.That(BytesToString(1_024L * 1_024 * 1_024 * 1_024)).IsEqualTo("1TiB");
        await Assert.That(BytesToString(1_024L * 1_024 * 1_024 * 1_024 * 1_024)).IsEqualTo("1PiB");
        await Assert.That(BytesToString(1_024L * 1_024 * 1_024 * 1_024 * 1_024 * 1_024)).IsEqualTo("1EiB");
        await Assert.That(BytesToString(5_823_996_738L)).IsEqualTo("5.42GiB");
        await Assert.That(BytesToString(long.MaxValue)).IsEqualTo("8EiB");
        await Assert.That(BytesToString(long.MinValue+1)).IsEqualTo("-8EiB");
    }
}