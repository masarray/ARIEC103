// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using ArIEC103.Master.Model;

namespace ArIEC103.Desktop.ViewModels;

public sealed class RelayEventRow
{
    public RelayEventRow(Iec103RelayEventLogEntry item)
    {
        Sequence = item.EvidenceSequenceNumber;
        RelayTime = BuildSoeTime(item);
        Signal = item.SignalName;
        Previous = string.IsNullOrWhiteSpace(item.PreviousValue) ? "-" : item.PreviousValue;
        NewValue = item.NewValue;
        Reason = item.EdgeReason;
        Fun = item.FunctionType.HasValue ? item.FunctionType.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        Inf = item.InformationNumber.HasValue ? item.InformationNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        FunInf = Fun == "-" && Inf == "-" ? "-" : $"{Fun}/{Inf}";
        Type = item.SignalType;
        Cot = item.CauseOfTransmission;
        Mapped = item.IsMapped ? "Yes" : "No";
        RawHex = item.RawHex;
    }

    public long Sequence { get; }
    public string RelayTime { get; }
    public string Signal { get; }
    public string Previous { get; }
    public string NewValue { get; }
    public string Reason { get; }
    public string Fun { get; }
    public string Inf { get; }
    public string FunInf { get; }
    public string Type { get; }
    public string Cot { get; }
    public string Mapped { get; }
    public string RawHex { get; }

    private static string BuildSoeTime(Iec103RelayEventLogEntry item)
    {
        if (string.IsNullOrWhiteSpace(item.RelayTimeText) ||
            item.RelayTimeText == "-" ||
            item.RelayTimeText.Contains("No relay timestamp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "no timestamp";
        }

        var localDate = item.ArrivalTimeUtc.ToLocalTime().ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        return localDate + " " + item.RelayTimeText;
    }
}

