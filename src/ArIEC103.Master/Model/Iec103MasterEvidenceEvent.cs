// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using ArIEC103.Core.Model;

namespace ArIEC103.Master.Model;

public sealed class Iec103MasterEvidenceEvent
{
    public long SequenceNumber { get; init; }
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public Iec103MasterState State { get; init; } = Iec103MasterState.Created;
    public FrameDirection Direction { get; init; }
    public string DirectionText => Direction == FrameDirection.MasterToSlave ? "TX" : Direction == FrameDirection.SlaveToMaster ? "RX" : "STATE";
    public string Category { get; init; } = "Info";
    public string DataClass { get; init; } = "-";
    public string PollingReason { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;

    /// <summary>
    /// Operator-first explanation for users who do not read raw hex. This is the primary UI
    /// output; raw hex remains available as evidence, not as the main language.
    /// </summary>
    public string OperatorMessage { get; init; } = string.Empty;
    public string ProtocolMeaning { get; init; } = string.Empty;
    public string OperatorAction { get; init; } = string.Empty;
    public string RawHex { get; init; } = string.Empty;

    /// <summary>
    /// Exception metadata is captured as diagnostic evidence, not thrown to the UI thread.
    /// This keeps field sessions recoverable while still preserving Visual-Studio-like detail.
    /// </summary>
    public string ExceptionType { get; init; } = string.Empty;
    public string ExceptionMessage { get; init; } = string.Empty;
    public string ExceptionStackTrace { get; init; } = string.Empty;

    public int? ResponseTimeMs { get; init; }
    public Ft12FrameDecode? Frame { get; init; }

    public bool? Acd => Frame?.LinkControl?.Acd;
    public bool? Dfc => Frame?.LinkControl?.Dfc;
    public string? AsduType => Frame?.Asdu?.TypeName;
    public string? Cot => Frame?.Asdu?.CauseName;
    public int? FunctionType => Frame?.Asdu?.FunctionType;
    public int? InformationNumber => Frame?.Asdu?.InformationNumber;

    // User-defined mapping profile result. These are not built-in vendor semantics.
    public bool IsRelayValue { get; init; }
    public bool IsRelayEdgeEvent { get; init; }
    public bool IsMappedSignal { get; init; }
    public string SignalKey { get; init; } = string.Empty;
    public string SignalName { get; init; } = string.Empty;
    public string SignalGroup { get; init; } = string.Empty;
    public string SignalType { get; init; } = string.Empty;
    public string SignalDisplayValue { get; init; } = string.Empty;
    public string SignalRawValue { get; init; } = string.Empty;
    public string PreviousSignalValue { get; init; } = string.Empty;
    public string EdgeReason { get; init; } = string.Empty;
    public string MappingProfileName { get; init; } = string.Empty;
    public string RelayTimestampText { get; init; } = string.Empty;
    public bool RelayTimestampInvalid { get; init; }

    // Backward-compatible aliases used by older UI/report code.
    public string? SemanticLabel => string.IsNullOrWhiteSpace(SignalName) ? null : SignalName;
    public string? SemanticCategory => string.IsNullOrWhiteSpace(SignalGroup) ? null : SignalGroup;
    public string? SemanticState => string.IsNullOrWhiteSpace(SignalDisplayValue) ? null : SignalDisplayValue;
    public string? SemanticConfidence => IsMappedSignal ? "user-profile" : null;
    public string? ProfileName => string.IsNullOrWhiteSpace(MappingProfileName) ? null : MappingProfileName;
}
