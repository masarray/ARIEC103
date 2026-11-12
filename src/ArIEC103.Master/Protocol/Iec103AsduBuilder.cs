// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

namespace ArIEC103.Master.Protocol;

public static class Iec103AsduBuilder
{
    public static byte[] GeneralInterrogation(byte commonAddress, byte scanQualifier = 0)
    {
        // Type 7, VSQ 1, COT 9, CA, FUN=255, INF=0, scan qualifier.
        return new[] { (byte)0x07, (byte)0x81, (byte)0x09, commonAddress, (byte)0xFF, (byte)0x00, scanQualifier };
    }

    public static byte[] ClockSynchronization(byte commonAddress, DateTime localTime)
    {
        // Type 6, VSQ 1, COT 8, CA, FUN=255, INF=0, CP56Time2a (seven octets).
        var time = EncodeCp56Time2a(localTime);
        return new[]
        {
            (byte)0x06, (byte)0x81, (byte)0x08, commonAddress, (byte)0xFF, (byte)0x00,
            time[0], time[1], time[2], time[3], time[4], time[5], time[6]
        };
    }


    public static byte[] GeneralCommand(byte commonAddress, byte functionType, byte informationNumber, byte commandQualifier = 1, byte returnInformationIdentifier = 0)
    {
        // Type 20, VSQ 1, COT 20, CA, FUN, INF, DCO, RII.
        // The relay returns the RII as SIN of the ASDU 1 that confirms the command.
        // Project/relay-specific command semantics must be validated by the user mapping/profile.
        return new[] { (byte)0x14, (byte)0x81, (byte)0x14, commonAddress, functionType, informationNumber, commandQualifier, returnInformationIdentifier };
    }

    public static byte[] ProtectionResetCommand(byte commonAddress, byte functionType = 255, byte informationNumber = 19)
    {
        return GeneralCommand(commonAddress, functionType, informationNumber, commandQualifier: 1);
    }

    public static byte[] EncodeCp32Time2a(DateTime localTime)
    {
        var t = localTime.Kind == DateTimeKind.Utc ? localTime.ToLocalTime() : localTime;
        var milliseconds = checked((ushort)((t.Second * 1000) + t.Millisecond));

        return new[]
        {
            (byte)(milliseconds & 0xFF),
            (byte)((milliseconds >> 8) & 0xFF),
            (byte)(t.Minute & 0x3F),
            (byte)(t.Hour & 0x1F),
            (byte)0x00
        };
    }

    public static byte[] EncodeCp56Time2a(DateTime localTime)
    {
        var t = localTime.Kind == DateTimeKind.Utc ? localTime.ToLocalTime() : localTime;
        var milliseconds = checked((ushort)((t.Second * 1000) + t.Millisecond));
        // Day of week 1 = Monday .. 7 = Sunday.
        var dayOfWeek = t.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)t.DayOfWeek;

        return new[]
        {
            (byte)(milliseconds & 0xFF),
            (byte)((milliseconds >> 8) & 0xFF),
            (byte)(t.Minute & 0x3F),
            (byte)(t.Hour & 0x1F),
            (byte)((t.Day & 0x1F) | (dayOfWeek << 5)),
            (byte)(t.Month & 0x0F),
            (byte)(t.Year % 100)
        };
    }
}
