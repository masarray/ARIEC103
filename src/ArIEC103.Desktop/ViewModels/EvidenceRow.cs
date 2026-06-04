// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using ArIEC103.Master.Model;

namespace ArIEC103.Desktop.ViewModels;

public sealed class EvidenceRow
{
    public EvidenceRow(Iec103MasterEvidenceEvent item)
    {
        Source = item;
        Sequence = item.SequenceNumber;
        Time = item.TimestampUtc.ToLocalTime().ToString("HH:mm:ss.fff");
        Direction = item.DirectionText;
        State = item.State.ToString();
        DataClass = item.DataClass;
        ResponseTime = item.ResponseTimeMs.HasValue ? item.ResponseTimeMs.Value + " ms" : "-";
        Summary = item.Summary;
        Detail = item.Detail;
        OperatorMessage = string.IsNullOrWhiteSpace(item.OperatorMessage) ? item.Summary : item.OperatorMessage;
        ProtocolMeaning = string.IsNullOrWhiteSpace(item.ProtocolMeaning) ? item.Detail : item.ProtocolMeaning;
        OperatorAction = item.OperatorAction;
        RawHex = string.IsNullOrWhiteSpace(item.RawHex) ? "-" : item.RawHex;
        PollingReason = item.PollingReason;
        Category = item.Category;
        Acd = item.Acd.HasValue ? (item.Acd.Value ? "1" : "0") : "-";
        Dfc = item.Dfc.HasValue ? (item.Dfc.Value ? "1" : "0") : "-";
        AsduType = item.AsduType ?? "-";
        Cot = item.Cot ?? "-";
        Fun = item.FunctionType.HasValue ? item.FunctionType.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        Inf = item.InformationNumber.HasValue ? item.InformationNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        FunInf = Fun == "-" && Inf == "-" ? "-" : $"{Fun}/{Inf}";
        SemanticLabel = item.SignalName;
        SemanticCategory = item.SignalGroup;
        SemanticState = item.SignalDisplayValue;
        ProfileName = item.MappingProfileName;
        RelayTime = string.IsNullOrWhiteSpace(item.RelayTimestampText) ? "-" : item.RelayTimestampText;
        Edge = item.IsRelayEdgeEvent ? item.EdgeReason : "-";
        Mapped = item.IsMappedSignal ? "Yes" : "No";
        SignalOrAddress = BuildSignalOrAddress();
        ReadableMeaning = BuildReadableMeaning(item);
    }

    public Iec103MasterEvidenceEvent Source { get; }
    public long Sequence { get; }
    public string Time { get; }
    public string Direction { get; }
    public string State { get; }
    public string DataClass { get; }
    public string ResponseTime { get; }
    public string Summary { get; }
    public string Detail { get; }
    public string OperatorMessage { get; }
    public string ProtocolMeaning { get; }
    public string OperatorAction { get; }
    public string RawHex { get; }
    public string PollingReason { get; }
    public string Category { get; }
    public string Acd { get; }
    public string Dfc { get; }
    public string AsduType { get; }
    public string Cot { get; }
    public string Fun { get; }
    public string Inf { get; }
    public string FunInf { get; }
    public string SemanticLabel { get; }
    public string SemanticCategory { get; }
    public string SemanticState { get; }
    public string ProfileName { get; }
    public string RelayTime { get; }
    public string Edge { get; }
    public string Mapped { get; }
    public string SignalOrAddress { get; }
    public string ReadableMeaning { get; }

    private string BuildSignalOrAddress()
    {
        if (!string.IsNullOrWhiteSpace(SemanticLabel))
        {
            return SemanticLabel;
        }

        return FunInf == "-" ? "-" : "FUN/INF " + FunInf;
    }

    private string BuildReadableMeaning(Iec103MasterEvidenceEvent item)
    {
        if (item.IsRelayValue)
        {
            var signal = string.IsNullOrWhiteSpace(SemanticLabel) ? BuildSignalOrAddress() : SemanticLabel;
            var value = string.IsNullOrWhiteSpace(SemanticState) ? item.SignalRawValue : SemanticState;
            var source = string.IsNullOrWhiteSpace(Cot) || Cot == "-" ? DataClass : Cot;
            return string.IsNullOrWhiteSpace(value)
                ? $"Relay value received: {signal} ({source})."
                : $"Relay value received: {signal} = {value} ({source}).";
        }

        if (!string.IsNullOrWhiteSpace(OperatorMessage))
        {
            return OperatorMessage;
        }

        if (!string.IsNullOrWhiteSpace(ProtocolMeaning))
        {
            return ProtocolMeaning;
        }

        return string.IsNullOrWhiteSpace(Summary) ? Detail : Summary;
    }
}
