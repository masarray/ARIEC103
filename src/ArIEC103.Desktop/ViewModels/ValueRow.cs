// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using ArIEC103.Master.Model;

namespace ArIEC103.Desktop.ViewModels;

public sealed class ValueRow
{
    public ValueRow(Iec103ValuePoint item)
    {
        Key = item.Key;
        Signal = item.SignalName;
        Group = item.SignalGroup;
        Value = item.DisplayValue;
        RelayTime = item.RelayTimeText;
        Fun = item.FunctionType.HasValue ? item.FunctionType.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        Inf = item.InformationNumber.HasValue ? item.InformationNumber.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-";
        FunInf = Fun == "-" && Inf == "-" ? "-" : $"{Fun}/{Inf}";
        Type = item.SignalType;
        Cot = item.CauseOfTransmission;
        Mapped = item.IsMapped ? "Yes" : "No";
        RawHex = item.RawHex;
    }

    public string Key { get; }
    public string Signal { get; }
    public string Group { get; }
    public string Value { get; }
    public string RelayTime { get; }
    public string Fun { get; }
    public string Inf { get; }
    public string FunInf { get; }
    public string Type { get; }
    public string Cot { get; }
    public string Mapped { get; }
    public string RawHex { get; }
}
