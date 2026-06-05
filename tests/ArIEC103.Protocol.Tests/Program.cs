// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using System.Threading.Channels;
using ArIEC103.Core.Model;
using ArIEC103.Core.Parsing;
using ArIEC103.Master;
using ArIEC103.Master.Model;
using ArIEC103.Master.Protocol;
using ArIEC103.Master.Transport;

namespace ArIEC103.Protocol.Tests;

internal static class Program
{
    private static async Task<int> Main()
    {
        var tests = new (string Name, Func<Task> Run)[]
        {
            ("FT1.2 parser accepts valid fixed secondary NO DATA frame", TestParserAcceptsValidFixedNoData),
            ("FT1.2 parser detects checksum mismatch", TestParserDetectsChecksumMismatch),
            ("FT1.2 reader resynchronizes after noise bytes", TestReaderResynchronizesAfterNoise),
            ("Master FCB is held after timeout and advances only after valid response", TestFcbHeldAfterTimeout),
            ("Master FCB is held after invalid checksum response", TestFcbHeldAfterInvalidChecksum)
        };

        var failed = 0;
        foreach (var test in tests)
        {
            try
            {
                await test.Run().ConfigureAwait(false);
                Console.WriteLine($"PASS {test.Name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.Error.WriteLine($"FAIL {test.Name}");
                Console.Error.WriteLine("     " + ex.Message);
            }
        }

        Console.WriteLine(failed == 0
            ? $"Protocol smoke tests passed: {tests.Length}/{tests.Length}"
            : $"Protocol smoke tests failed: {failed}/{tests.Length}");

        return failed == 0 ? 0 : 1;
    }

    private static Task TestParserAcceptsValidFixedNoData()
    {
        var parser = new Ft12Parser();
        var frame = parser.Decode(SecondaryFixed(functionCode: 9, acd: false));

        AssertEqual(Ft12FrameFormat.FixedLength, frame.Format, "frame format");
        AssertTrue(frame.IsChecksumValid, "checksum must be valid");
        AssertNotNull(frame.LinkControl, "link control must decode");
        AssertFalse(frame.LinkControl!.Prm, "secondary response must have PRM=0");
        AssertEqual(9, frame.LinkControl.FunctionCode, "secondary function code");
        return Task.CompletedTask;
    }

    private static Task TestParserDetectsChecksumMismatch()
    {
        var parser = new Ft12Parser();
        var frame = SecondaryFixed(functionCode: 9, acd: false);
        frame[3] ^= 0x7F;
        var decoded = parser.Decode(frame);

        AssertFalse(decoded.IsChecksumValid, "checksum must be invalid");
        AssertTrue(decoded.Issues.Any(x => x.Contains("Checksum", StringComparison.OrdinalIgnoreCase)), "checksum issue must be recorded");
        return Task.CompletedTask;
    }

    private static async Task TestReaderResynchronizesAfterNoise()
    {
        await using var transport = new ScriptedTransport();
        transport.EnqueueIncoming(new byte[] { 0x00, 0xFF, 0x33 });
        transport.EnqueueIncoming(SecondaryFixed(functionCode: 9, acd: true));

        var reader = new Ft12StreamReader(transport);
        var raw = await reader.ReadFrameAsync(timeoutMs: 200, CancellationToken.None).ConfigureAwait(false);

        AssertNotNull(raw, "reader must return a frame after noise");
        AssertEqual(5, raw!.Length, "fixed frame length");
        AssertEqual((byte)0x10, raw[0], "first returned byte must be FT1.2 fixed-frame start");
    }

    private static async Task TestFcbHeldAfterTimeout()
    {
        await using var transport = new ScriptedTransport();
        transport.ScriptWriteResponse(null); // first Class 2 request times out
        transport.ScriptWriteResponse(SecondaryFixed(functionCode: 9, acd: false)); // second request succeeds
        transport.ScriptWriteResponse(SecondaryFixed(functionCode: 9, acd: false)); // third request proves FCB advanced

        var session = CreateShortTimeoutSession(transport);
        await session.RequestClass2Async("timeout test - first", CancellationToken.None).ConfigureAwait(false);
        await session.RequestClass2Async("timeout test - second", CancellationToken.None).ConfigureAwait(false);
        await session.RequestClass2Async("timeout test - third", CancellationToken.None).ConfigureAwait(false);

        AssertEqual(3, transport.Writes.Count, "write count");
        AssertEqual((byte)0x5B, transport.Writes[0][1], "first Class 2 control must use FCB=0");
        AssertEqual((byte)0x5B, transport.Writes[1][1], "second Class 2 control must still use FCB=0 after timeout");
        AssertEqual((byte)0x7B, transport.Writes[2][1], "third Class 2 control must use FCB=1 after one valid response");
    }

    private static async Task TestFcbHeldAfterInvalidChecksum()
    {
        var badResponse = SecondaryFixed(functionCode: 9, acd: false);
        badResponse[3] ^= 0x11;

        await using var transport = new ScriptedTransport();
        transport.ScriptWriteResponse(badResponse);
        transport.ScriptWriteResponse(SecondaryFixed(functionCode: 9, acd: false));
        transport.ScriptWriteResponse(SecondaryFixed(functionCode: 9, acd: false));

        var session = CreateShortTimeoutSession(transport);
        await session.RequestClass2Async("invalid checksum test - first", CancellationToken.None).ConfigureAwait(false);
        await session.RequestClass2Async("invalid checksum test - second", CancellationToken.None).ConfigureAwait(false);
        await session.RequestClass2Async("invalid checksum test - third", CancellationToken.None).ConfigureAwait(false);

        AssertEqual(3, transport.Writes.Count, "write count");
        AssertEqual((byte)0x5B, transport.Writes[0][1], "first Class 2 control must use FCB=0");
        AssertEqual((byte)0x5B, transport.Writes[1][1], "second Class 2 control must still use FCB=0 after invalid response");
        AssertEqual((byte)0x7B, transport.Writes[2][1], "third Class 2 control must use FCB=1 after valid response");
    }

    private static Iec103MasterSession CreateShortTimeoutSession(ScriptedTransport transport)
    {
        return new Iec103MasterSession(new Iec103MasterSettings
        {
            UseSimulatedSlave = true,
            ResponseTimeoutMs = 25,
            TimeoutRecoveryBackoffMs = 0,
            MaxConsecutiveTimeoutsBeforeResetFcb = 99,
            ResetFcbAfterTimeoutBurst = false,
            Class2PollIntervalMs = 100,
            LinkAddress = 1,
            CommonAddress = 1
        }, transport);
    }

    private static byte[] SecondaryFixed(int functionCode, bool acd)
    {
        var control = (byte)(functionCode & 0x0F);
        if (acd)
        {
            control |= 0x20;
        }

        return Ft12FrameBuilder.Fixed(control, linkAddress: 1);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertFalse(bool condition, string message) => AssertTrue(!condition, message);

    private static void AssertNotNull(object? value, string message)
    {
        if (value is null)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void AssertEqual<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
        }
    }

    private sealed class ScriptedTransport : IByteTransport
    {
        private readonly Queue<byte[]?> _scriptedResponses = new();
        private readonly Channel<byte> _rx = Channel.CreateUnbounded<byte>();
        private bool _isOpen = true;

        public bool IsOpen => _isOpen;
        public List<byte[]> Writes { get; } = new();

        public ValueTask OpenAsync(CancellationToken cancellationToken)
        {
            _isOpen = true;
            return ValueTask.CompletedTask;
        }

        public ValueTask CloseAsync(CancellationToken cancellationToken)
        {
            _isOpen = false;
            return ValueTask.CompletedTask;
        }

        public void Dispose() => _isOpen = false;

        public ValueTask DisposeAsync()
        {
            _isOpen = false;
            return ValueTask.CompletedTask;
        }

        public void ScriptWriteResponse(byte[]? response) => _scriptedResponses.Enqueue(response);

        public void EnqueueIncoming(IReadOnlyList<byte> bytes)
        {
            foreach (var b in bytes)
            {
                _rx.Writer.TryWrite(b);
            }
        }

        public ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
        {
            Writes.Add(buffer.ToArray());
            if (_scriptedResponses.Count > 0)
            {
                var response = _scriptedResponses.Dequeue();
                if (response is not null)
                {
                    EnqueueIncoming(response);
                }
            }

            return ValueTask.CompletedTask;
        }

        public async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (buffer.Length == 0)
            {
                return 0;
            }

            var first = await _rx.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            buffer.Span[0] = first;
            var count = 1;
            while (count < buffer.Length && _rx.Reader.TryRead(out var next))
            {
                buffer.Span[count++] = next;
            }

            return count;
        }
    }
}
