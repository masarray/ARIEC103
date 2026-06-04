// Copyright 2026 Ari Sulistiono
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ArIEC103.Core.Mapping;
using ArIEC103.Core.Model;
using ArIEC103.Desktop.ViewModels;
using ArIEC103.Master;
using ArIEC103.Master.Model;
using ArIEC103.Master.Reporting;
using ArIEC103.Master.Transport;
using Microsoft.Win32;

namespace ArIEC103.Desktop;

public partial class MainWindow : Window
{
    private CancellationTokenSource? _sessionCancellation;
    private Iec103MasterRunResult? _lastResult;
    private int _txCount;
    private int _rxCount;
    private int _class1Count;
    private int _class2Count;
    private int _noDataCount;
    private int _dpiCount;
    private long _visibleEvidenceDropped;
    private long _visibleRelayEventsDropped;
    private long _visibleLogLinesDropped;
    private long _visibleDiagnosticsDropped;
    private Iec103SignalMappingProfile _mappingProfile = Iec103SignalMappingProfile.Empty;
    private readonly List<RelayEventRow> _allRelayEventRows = new();
    private IByteTransport? _activeTransport;
    private bool _stopRequested;
    private string _selectedFrameExplanation = "Select a frame. This panel translates raw bytes into commissioning meaning.";
    private EvidenceRow? _selectedFrameRow;
    private string? _pinnedProtocolMapKey;
    private bool _statusHistoryExpanded = true;

    private const int MaxVisibleEvidenceRows = 1200;
    private const int MaxVisibleRelayEventRows = 800;
    private const int MaxVisibleFindingRows = 300;
    private const int MaxVisibleDiagnosticRows = 500;
    private const int MaxSessionLogLines = 600;
    private const int MaxUiFlushPerTick = 250;

    private readonly ConcurrentQueue<Iec103MasterEvidenceEvent> _pendingEvidence = new();
    private readonly ConcurrentQueue<Iec103MasterFinding> _pendingFindings = new();
    private readonly Queue<string> _sessionLogLines = new();
    private readonly DispatcherTimer _uiFlushTimer;
    private readonly DispatcherTimer _ledDecayTimer;
    private readonly Dictionary<FrameworkElement, DateTime> _ledPulseTimes = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        _uiFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(120)
        };
        _uiFlushTimer.Tick += (_, _) => FlushUiQueues();
        _uiFlushTimer.Start();
        _ledDecayTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(90)
        };
        _ledDecayTimer.Tick += (_, _) => DecayLedPulses();
        _ledDecayTimer.Start();
        RefreshPorts();
        AppendSessionLog("ArIEC103 desktop shell initialized. Ready for single connection IEC-103 master test.");
        AppendSessionLog("Output model: Operator/Engineer views first, raw hex remains available in Frame Trace for protocol transparency.");
        Loaded += (_, _) =>
        {
            MainTabControl.SelectedIndex = 0;
            UpdateSegmentedNav(false);
        };
        SizeChanged += (_, _) => UpdateSegmentedNav(false);
    }

    public ObservableCollection<EvidenceRow> EvidenceRows { get; } = new();
    public ObservableCollection<EvidenceRow> FrameTraceRows { get; } = new();
    public ObservableCollection<FindingRow> FindingRows { get; } = new();
    public ObservableCollection<ValueRow> ValueRows { get; } = new();
    public ObservableCollection<RelayEventRow> RelayEventRows { get; } = new();
    public ObservableCollection<AssessmentRow> AssessmentRows { get; } = new();
    public ObservableCollection<DiagnosticRow> DiagnosticRows { get; } = new();
    public ObservableCollection<ProtocolMapLine> SelectedProtocolMapLines { get; } = new();
    public ObservableCollection<HexSegment> SelectedHexSegments { get; } = new();
    public ObservableCollection<StatusHistoryRow> StatusHistoryRows { get; } = new();

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void OpenSetup_Click(object sender, RoutedEventArgs e)
    {
        SetupOverlay.Visibility = Visibility.Visible;
    }

    private void CloseSetup_Click(object sender, RoutedEventArgs e)
    {
        SetupOverlay.Visibility = Visibility.Collapsed;
    }

    private void RefreshPorts()
    {
        var previous = PortComboBox.SelectedItem as string;
        PortComboBox.Items.Clear();

        var ports = SerialPort.GetPortNames()
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ports.Length == 0)
        {
            PortComboBox.Items.Add("COM1");
        }
        else
        {
            foreach (var port in ports)
            {
                PortComboBox.Items.Add(port);
            }
        }

        PortComboBox.SelectedItem = !string.IsNullOrWhiteSpace(previous) && PortComboBox.Items.Contains(previous)
            ? previous
            : PortComboBox.Items[0];
    }

    private async void Start_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionCancellation != null)
        {
            return;
        }

        Iec103MasterSettings settings;
        int durationSeconds;
        try
        {
            settings = BuildSettingsFromUi();
            durationSeconds = ReadInt(DurationBox, "Session timeout", 0, 86400);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Invalid settings", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ClearSessionView(clearLog: false);
        _stopRequested = false;
        SetRunUiState(isRunning: true);
        _lastResult = null;
        _sessionCancellation = new CancellationTokenSource();
        SessionSubtitleText.Text = settings.SerialSummary;
        UpdateStableHeader("Monitoring", settings.UseSimulatedSlave
            ? "Demo relay mode active. Monitoring continuously until Stop."
            : "Serial master session active. Monitoring continuously until Stop.");
        AppendSessionLog("Starting master session: " + settings.SerialSummary);
        AppendSessionLog("Target mode: " + (settings.UseSimulatedSlave ? "generic relay demo simulation" : "Real serial COM relay"));
        AppendSessionLog("Polling profile: Class 2 normal cycle; Class 1 only when ACD=1 or bounded GI follow-up.");
        AppendSessionLog(_mappingProfile.HasSignals ? $"Mapping profile loaded: {_mappingProfile.ProfileName} ({_mappingProfile.Signals.Count} signals)." : "No mapping profile loaded. Value/Event views will show raw FUN/INF names.");

        try
        {
            await using var transport = CreateTransport(settings);
            _activeTransport = transport;
            var session = new Iec103MasterSession(settings, transport, _mappingProfile);
            session.EvidenceReceived += OnEvidenceReceived;
            session.FindingRaised += OnFindingRaised;

            var result = durationSeconds <= 0
                ? await session.RunAsync(_sessionCancellation.Token).ConfigureAwait(false)
                : await session.RunForAsync(TimeSpan.FromSeconds(durationSeconds), _sessionCancellation.Token).ConfigureAwait(false);
            _lastResult = result;

            await Dispatcher.InvokeAsync(() =>
            {
                ApplyFinalResult(result);
                AppendSessionLog("Monitor session completed: " + result.CompletionReason);
            });
        }
        catch (OperationCanceledException)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateStableHeader("Stopped", "Session stopped by user.");
                AppendSessionLog("Session stopped by user.");
            });
        }
        catch (Exception ex) when (_stopRequested || _sessionCancellation?.IsCancellationRequested == true)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateStableHeader("Stopped", "Session stopped and transport was closed safely.");
                AppendSessionLog("Session stopped while transport was closing: " + ex.Message);
                AddUiDiagnostic("Warning", "Desktop", "IEC103-DESKTOP-STOP-CLOSE", "Session stopped while transport was closing", ex.Message, "Usually safe during Stop/Force Close. If repeated, check USB/serial driver stability.", ex);
            });
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                UpdateStableHeader("Faulted", ex.Message);
                AppendSessionLog("Fault captured in Diagnostics: " + ex.Message);
                AddUiDiagnostic("Error", "Desktop", "IEC103-DESKTOP-SESSION-FAULT", "Master session fault", ex.Message, "Select this diagnostic row and copy detail if escalation/debugging is needed.", ex);
            });
        }
        finally
        {
            await Dispatcher.InvokeAsync(() =>
            {
                _activeTransport = null;
                _stopRequested = false;
                _sessionCancellation?.Dispose();
                _sessionCancellation = null;
                SetRunUiState(isRunning: false);
            });
        }
    }

    private async void Stop_Click(object sender, RoutedEventArgs e)
    {
        if (_sessionCancellation is null)
        {
            SetRunUiState(isRunning: false);
            return;
        }

        _stopRequested = true;
        _sessionCancellation.Cancel();
        StopButton.IsEnabled = true;
        StopButton.ToolTip = "Force close transport";
        UpdateStableHeader("Stopping", "Closing active transport safely.");
        AppendSessionLog("Stop requested by user. Active transport close requested.");

        await TryCloseActiveTransportAsync("Stop request");
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearSessionView(clearLog: true);

    private void ExportMarkdown_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult == null)
        {
            MessageBox.Show(this, "No completed session result is available yet.", "Export evidence", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Export ArIEC103 Evidence Report",
            Filter = "Markdown report (*.md)|*.md|All files (*.*)|*.*",
            FileName = "ArIEC103-master-evidence.md",
            AddExtension = true,
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var markdown = new MasterMarkdownReportWriter().Write(_lastResult, maxEvents: 1000);
        File.WriteAllText(dialog.FileName, markdown, Encoding.UTF8);
        AppendSessionLog("Evidence report exported: " + dialog.FileName);
        MessageBox.Show(this, "Evidence report exported successfully.", "Export evidence", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private Iec103MasterSettings BuildSettingsFromUi()
    {
        var port = (PortComboBox.SelectedItem as string)?.Trim();
        if (string.IsNullOrWhiteSpace(port))
        {
            throw new InvalidOperationException("COM port is required.");
        }

        var settings = Iec103MasterSettings.CreateDefault();
        settings.UseSimulatedSlave = IsDemoModeSelected();
        settings.TargetProfile = settings.UseSimulatedSlave ? "generic relay demo slave" : "IEC-103 protection relay";
        settings.PortName = port;
        settings.BaudRate = ReadComboInt(BaudComboBox, "Baudrate");
        settings.LinkAddress = checked((byte)ReadInt(LinkAddressBox, "Link Address", 0, 255));
        settings.CommonAddress = checked((byte)ReadInt(CommonAddressBox, "Common Address", 0, 255));
        settings.ResponseTimeoutMs = ReadInt(TimeoutBox, "Timeout", 100, 60000);
        settings.Class2PollIntervalMs = ReadInt(Class2IntervalBox, "Class 2 interval", 50, 60000);
        settings.MaxClass1DrainFrames = ReadInt(MaxDrainBox, "Max Class 1 drain", 1, 512);
        settings.ResetRemoteLinkOnConnect = ResetRemoteLinkCheckBox.IsChecked == true;
        settings.ResetFcbOnConnect = ResetFcbCheckBox.IsChecked == true;
        settings.SendClockSyncOnConnect = ClockSyncCheckBox.IsChecked == true;
        settings.SendGeneralInterrogationOnConnect = GiCheckBox.IsChecked == true;
        settings.RequestClass2ImmediatelyAfterStartup = Class2StartupCheckBox.IsChecked == true;
        settings.MappingProfilePath = MappingProfilePathBox.Text.Trim();

        var serialMode = (SerialModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "8E1";
        settings.DataBits = 8;
        settings.StopBits = StopBits.One;
        settings.Parity = serialMode switch
        {
            "8N1" => Parity.None,
            "8O1" => Parity.Odd,
            _ => Parity.Even
        };

        return settings;
    }

    private async Task TryCloseActiveTransportAsync(string reason)
    {
        var transport = _activeTransport;
        if (transport is null)
        {
            return;
        }

        try
        {
            await transport.CloseAsync(CancellationToken.None).ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() => AppendSessionLog($"Transport closed: {reason}."));
        }
        catch (Exception ex)
        {
            await Dispatcher.InvokeAsync(() =>
            {
                AppendSessionLog($"Transport close warning: {ex.Message}");
                AddUiDiagnostic("Warning", "Transport", "IEC103-TRANSPORT-CLOSE", "Transport close warning", ex.Message, "Stop/Force Close requested. If COM port remains locked, unplug/replug the USB converter or restart the app.", ex);
            });
        }
    }

    private IByteTransport CreateTransport(Iec103MasterSettings settings)
    {
        return settings.UseSimulatedSlave
            ? new SimulatedRelayTransport(settings)
            : new SerialByteTransport(settings);
    }

    private bool IsDemoModeSelected()
    {
        var mode = (TransportModeComboBox.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        return mode.Contains("demo", StringComparison.OrdinalIgnoreCase) || mode.Contains("simulated", StringComparison.OrdinalIgnoreCase);
    }

    private static int ReadComboInt(ComboBox comboBox, string label)
    {
        var value = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
        if (!int.TryParse(value, out var number))
        {
            throw new InvalidOperationException(label + " is invalid.");
        }

        return number;
    }

    private static int ReadInt(TextBox textBox, string label, int min, int max)
    {
        if (!int.TryParse(textBox.Text.Trim(), out var number))
        {
            throw new InvalidOperationException(label + " must be a number.");
        }

        if (number < min || number > max)
        {
            throw new InvalidOperationException($"{label} must be between {min} and {max}.");
        }

        return number;
    }

    private void OnEvidenceReceived(object? sender, Iec103MasterEvidenceEvent item)
    {
        // Do not render one WPF row per protocol event immediately. High-volume polling can
        // produce thousands of frames; the UI consumes this queue in timed batches.
        _pendingEvidence.Enqueue(item);
    }

    private void OnFindingRaised(object? sender, Iec103MasterFinding finding)
    {
        _pendingFindings.Enqueue(finding);
    }

    private void FlushUiQueues()
    {
        var processed = 0;
        while (processed < MaxUiFlushPerTick && _pendingEvidence.TryDequeue(out var item))
        {
            ApplyEvidenceToUi(item);
            processed++;
        }

        var findingProcessed = 0;
        while (findingProcessed < 50 && _pendingFindings.TryDequeue(out var finding))
        {
            ApplyFindingToUi(finding);
            findingProcessed++;
        }

        UpdateBufferStatus();
    }

    private void ApplyEvidenceToUi(Iec103MasterEvidenceEvent item)
    {
        var row = new EvidenceRow(item);

        if (ShouldShowInOperatorEvidence(item, row))
        {
            EvidenceRows.Add(row);
            while (EvidenceRows.Count > MaxVisibleEvidenceRows)
            {
                EvidenceRows.RemoveAt(0);
                _visibleEvidenceDropped++;
            }
        }

        if (ShouldShowInFrameTrace(row))
        {
            FrameTraceRows.Add(row);
            while (FrameTraceRows.Count > MaxVisibleEvidenceRows)
            {
                FrameTraceRows.RemoveAt(0);
                _visibleEvidenceDropped++;
            }
        }

        UpdateLiveCounters(item);
        UpdateValueAndEventViews(item);
        if (IsDiagnosticEvidence(item))
        {
            PulseLed(DiagLed);
            AddDiagnosticRow(new DiagnosticRow(item));
            UpdateStableHeader("Attention", ChooseOperatorStatus(item));
        }

        // Do not push every protocol state into the top session card. High-volume
        // polling alternates Class 2/Class 1 states quickly and makes Auto-sized WPF
        // layouts appear to flicker. The header shows stable session phase only;
        // detailed per-frame state belongs in Operator Evidence / Frame Trace.

        if (item.Category == "Error" || item.Category == "Warning" || item.Category == "RX Warning" || IsImportantSessionNote(item))
        {
            AppendSessionLog($"#{item.SequenceNumber} {item.State}: {item.Summary} - {item.Detail}");
        }
    }

    private static bool ShouldShowInFrameTrace(EvidenceRow row)
    {
        return row.RawHex != "-" &&
               (row.Direction.Equals("TX", StringComparison.OrdinalIgnoreCase) ||
                row.Direction.Equals("RX", StringComparison.OrdinalIgnoreCase));
    }

    private static bool ShouldShowInOperatorEvidence(Iec103MasterEvidenceEvent item, EvidenceRow row)
    {
        if (IsDiagnosticEvidence(item) || item.IsRelayValue || item.IsRelayEdgeEvent)
        {
            return true;
        }

        var text = string.Join(" ", item.Summary, item.Detail, item.OperatorMessage, item.OperatorAction, item.ProtocolMeaning);
        if (text.Contains("General Interrogation", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("GI ", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Clock", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Reset", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("ACD=1", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("event-drain", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("DFC", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("NO DATA", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Suppress repetitive normal Class 2 request/response noise from the operator view.
        return false;
    }

    private static bool IsImportantSessionNote(Iec103MasterEvidenceEvent item)
    {
        var text = string.Join(" ", item.Summary, item.Detail, item.OperatorMessage);
        return text.Contains("General Interrogation", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("GI ", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Fault", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Assessment", StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyFindingToUi(Iec103MasterFinding finding)
    {
        FindingRows.Add(new FindingRow(finding));
        while (FindingRows.Count > MaxVisibleFindingRows)
        {
            FindingRows.RemoveAt(0);
        }

        FindingCountText.Text = FindingRows.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        PulseLed(DiagLed);
        AddDiagnosticRow(new DiagnosticRow(finding));
        AppendSessionLog($"Finding [{finding.Severity}] {finding.Id}: {finding.Title}");
    }

    private static string ChooseOperatorStatus(Iec103MasterEvidenceEvent item)
    {
        if (!string.IsNullOrWhiteSpace(item.OperatorMessage))
        {
            return string.IsNullOrWhiteSpace(item.OperatorAction)
                ? item.OperatorMessage
                : item.OperatorMessage + " " + item.OperatorAction;
        }

        return string.IsNullOrWhiteSpace(item.Detail) ? item.Summary : item.Detail;
    }

    private void UpdateLiveCounters(Iec103MasterEvidenceEvent item)
    {
        if (item.Direction == FrameDirection.MasterToSlave)
        {
            _txCount++;
            PulseLed(TxLed);
        }
        else if (item.Direction == FrameDirection.SlaveToMaster)
        {
            _rxCount++;
            PulseLed(RxLed);
        }

        if (string.Equals(item.DataClass, "Class 1", StringComparison.OrdinalIgnoreCase) && item.Direction == FrameDirection.MasterToSlave)
        {
            _class1Count++;
            PulseLed(Class1Led);
        }

        if (string.Equals(item.DataClass, "Class 2", StringComparison.OrdinalIgnoreCase) && item.Direction == FrameDirection.MasterToSlave)
        {
            _class2Count++;
            PulseLed(Class2Led);
        }

        if (item.Summary.Contains("NO DATA", StringComparison.OrdinalIgnoreCase) || item.Detail.Contains("NO DATA", StringComparison.OrdinalIgnoreCase))
        {
            _noDataCount++;
        }

        if (item.Frame?.Asdu?.TypeId == 1 || item.Frame?.Asdu?.TypeId == 2)
        {
            _dpiCount++;
            PulseLed(EventLed);
        }

        TxRxText.Text = $"{_txCount} / {_rxCount}";
        ClassPollText.Text = $"{_class1Count} / {_class2Count}";
        NoDataText.Text = _noDataCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        DpiText.Text = _dpiCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private void PulseLed(FrameworkElement led)
    {
        if (led == null)
        {
            return;
        }

        led.Opacity = 1.0;
        _ledPulseTimes[led] = DateTime.UtcNow;
    }

    private void DecayLedPulses()
    {
        if (_ledPulseTimes.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var pair in _ledPulseTimes.ToArray())
        {
            if ((now - pair.Value).TotalMilliseconds >= 180)
            {
                pair.Key.Opacity = 0.28;
                _ledPulseTimes.Remove(pair.Key);
            }
        }
    }

    private void ApplyFinalResult(Iec103MasterRunResult result)
    {
        FlushUiQueues();
        TxRxText.Text = $"{result.Counters.TxFrames} / {result.Counters.RxFrames}";
        ClassPollText.Text = $"{result.Counters.Class1Requests} / {result.Counters.Class2Requests}";
        NoDataText.Text = result.Counters.NoDataResponses.ToString(System.Globalization.CultureInfo.InvariantCulture);
        DpiText.Text = result.Counters.DpiEvents.ToString(System.Globalization.CultureInfo.InvariantCulture);
        FindingCountText.Text = result.Findings.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        UpdateStableHeader(result.CompletedNormally ? "Completed" : "Faulted",
            $"Assessment: {result.Assessment.OverallStatus} ({result.Assessment.Score}/100). {result.CompletionReason}");
        ExportMarkdownButton.IsEnabled = true;

        AssessmentRows.Clear();
        foreach (var item in result.Assessment.Items)
        {
            AssessmentRows.Add(new AssessmentRow(item));
        }
        AppendSessionLog($"Assessment: {result.Assessment.OverallStatus} ({result.Assessment.Score}/100) - {result.Assessment.Summary}");

        ValueRows.Clear();
        foreach (var value in result.ValuePoints.Select(x => new ValueRow(x)).OrderBy(GetValueRowSortRank).ThenBy(x => x.Group, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Fun).ThenBy(x => x.Inf).ThenBy(x => x.Signal, StringComparer.OrdinalIgnoreCase))
        {
            ValueRows.Add(value);
        }

        _allRelayEventRows.Clear();
        foreach (var ev in result.EventLog)
        {
            _allRelayEventRows.Add(new RelayEventRow(ev));
        }
        ApplyRelayEventFilter();

        foreach (var finding in result.Findings)
        {
            if (!FindingRows.Any(x => x.Id == finding.Id && x.Title == finding.Title))
            {
                FindingRows.Add(new FindingRow(finding));
            }
        }
    }

    private void EvidenceGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as DataGrid)?.SelectedItem is not EvidenceRow row)
        {
            _selectedFrameRow = null;
            SelectedDetailText.Text = "Select evidence row to inspect decoded meaning.";
            SelectedRawText.Text = "-";
            _selectedFrameExplanation = "Select a frame. This panel translates raw bytes into commissioning meaning.";
            SelectedLineSummaryText.Text = _selectedFrameExplanation;
            SelectedLineDirectionText.Text = "Select a frame";
            SelectedLineSummaryText.Text = "The selected IEC-103 frame will be decoded into link layer, ASDU, address, value/time, and integrity groups.";
            SelectedProtocolMapLines.Clear();
            SelectedHexSegments.Clear();
            return;
        }

        _selectedFrameRow = row;
        _pinnedProtocolMapKey = null;
        if (PinProtocolMapCheckBox != null)
        {
            PinProtocolMapCheckBox.IsChecked = false;
        }

        var explanation = BuildCompactFrameExplanation(row);
        SelectedDetailText.Text = explanation + Environment.NewLine + Environment.NewLine + "Raw: " + row.RawHex;
        SelectedRawText.Text = row.RawHex;
        _selectedFrameExplanation = explanation;
        SelectedLineSummaryText.Text = "Hover or click a protocol group. The panel stays stable; linked raw/meaning groups are highlighted without rewriting the inspector.";
        SelectedLineDirectionText.Text = BuildLineMonitorTitle(row);
        SelectedLineSummaryText.Text = BuildLineMonitorSummary(row);
        RebuildProtocolMap(row);
    }

    private static string BuildCompactFrameExplanation(EvidenceRow row)
    {
        var parts = new List<string>();
        parts.Add(row.ReadableMeaning);

        if (!string.IsNullOrWhiteSpace(row.SignalOrAddress) && row.SignalOrAddress != "-")
        {
            parts.Add($"Address: {row.SignalOrAddress}.");
        }

        if (!string.IsNullOrWhiteSpace(row.SemanticState))
        {
            parts.Add($"Value: {row.SemanticState}.");
        }

        parts.Add($"Protocol: {row.Direction} {row.DataClass}, ASDU={row.AsduType}, COT={row.Cot}, FUN/INF={row.FunInf}, ACD={row.Acd}, DFC={row.Dfc}.");

        if (!string.IsNullOrWhiteSpace(row.PollingReason) && row.PollingReason != "-")
        {
            parts.Add($"Why it happened: {row.PollingReason}.");
        }

        if (!string.IsNullOrWhiteSpace(row.OperatorAction))
        {
            parts.Add($"Recommended action: {row.OperatorAction}.");
        }

        if (!string.IsNullOrWhiteSpace(row.RelayTime) && row.RelayTime != "-")
        {
            parts.Add($"Relay time: {row.RelayTime}.");
        }

        return string.Join(Environment.NewLine, parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string BuildLineMonitorTitle(EvidenceRow row)
    {
        var arrow = row.Direction.Equals("TX", StringComparison.OrdinalIgnoreCase)
            ? "TX → Master to relay"
            : row.Direction.Equals("RX", StringComparison.OrdinalIgnoreCase)
                ? "RX ← Relay to master"
                : row.Direction;
        var cls = row.DataClass == "-" ? "Link" : row.DataClass;
        var service = row.AsduType == "-" ? row.Summary : row.AsduType;
        return $"{arrow} · {cls} · {service}";
    }

    private static string BuildLineMonitorSummary(EvidenceRow row)
    {
        var parts = new List<string> { row.ReadableMeaning };
        if (!string.IsNullOrWhiteSpace(row.SignalOrAddress) && row.SignalOrAddress != "-") parts.Add(row.SignalOrAddress);
        if (!string.IsNullOrWhiteSpace(row.RelayTime) && row.RelayTime != "-") parts.Add("relay time " + row.RelayTime);
        return string.Join(" · ", parts.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private void RebuildProtocolMap(EvidenceRow row)
    {
        SelectedProtocolMapLines.Clear();
        SelectedHexSegments.Clear();

        foreach (var line in BuildProtocolMapLines(row))
        {
            SelectedProtocolMapLines.Add(line);
        }

        foreach (var segment in BuildHexSegments(row))
        {
            SelectedHexSegments.Add(segment);
        }
    }

    private static IEnumerable<ProtocolMapLine> BuildProtocolMapLines(EvidenceRow row)
    {
        var bytes = SplitHexBytes(row.RawHex);
        var directionMeaning = row.Direction.Equals("TX", StringComparison.OrdinalIgnoreCase)
            ? "Master-to-relay frame. This is a tester action, not a relay event."
            : row.Direction.Equals("RX", StringComparison.OrdinalIgnoreCase)
                ? "Relay-to-master frame. This is relay evidence returned to the tester."
                : "Session note or diagnostic entry.";

        yield return new ProtocolMapLine("direction", "Direction", directionMeaning, row.Direction);

        if (bytes.Length == 0)
        {
            yield return new ProtocolMapLine("summary", "No raw frame", "This row is a state/diagnostic note, not a physical IEC-103 frame.", "-");
            yield break;
        }

        if (bytes[0].Equals("E5", StringComparison.OrdinalIgnoreCase))
        {
            yield return new ProtocolMapLine("envelope", "Single char ACK", "IEC FT1.2 single-character acknowledgement. The relay accepted the previous link/action frame.", "E5");
            yield break;
        }

        if (bytes[0].Equals("10", StringComparison.OrdinalIgnoreCase) && bytes.Length >= 5)
        {
            yield return new ProtocolMapLine("envelope", "FT1.2 fixed frame", "Short IEC-103 link frame. Used for reset, Class 1/Class 2 request, ACK, or NO DATA response.", string.Join(" ", bytes.Take(1)));
            yield return new ProtocolMapLine("control", "Control field", BuildControlMeaning(row), bytes.ElementAtOrDefault(1) ?? "-");
            yield return new ProtocolMapLine("address", "Link address", "Relay/slave address on the serial IEC-103 link.", bytes.ElementAtOrDefault(2) ?? "-");
            yield return new ProtocolMapLine("check", "Integrity", "Checksum and stop byte. This proves what was actually transmitted on the wire.", string.Join(" ", bytes.Skip(3).Take(2)));
            yield break;
        }

        if (bytes[0].Equals("68", StringComparison.OrdinalIgnoreCase) && bytes.Length >= 9)
        {
            yield return new ProtocolMapLine("envelope", "FT1.2 variable frame", "Variable IEC-103 frame carrying an ASDU. The length bytes define the payload size and must match.", string.Join(" ", bytes.Take(4)));
            yield return new ProtocolMapLine("control", "Link control", BuildControlMeaning(row), string.Join(" ", bytes.Skip(4).Take(2)));
            yield return new ProtocolMapLine("asdu", "ASDU header", BuildAsduHeaderMeaning(row), string.Join(" ", bytes.Skip(6).Take(Math.Min(4, Math.Max(0, bytes.Length - 8)))));

            if (bytes.Length > 11)
            {
                yield return new ProtocolMapLine("object", "FUN / INF", BuildSignalAddressMeaning(row), string.Join(" ", bytes.Skip(10).Take(2)));
            }

            var payloadEnd = Math.Max(12, bytes.Length - 2);
            if (payloadEnd > 12)
            {
                yield return new ProtocolMapLine("payload", "Information element", BuildPayloadMeaning(row), string.Join(" ", bytes.Skip(12).Take(payloadEnd - 12)));
            }

            yield return new ProtocolMapLine("check", "Integrity", "Checksum and end byte close the frame. Keep this as audit evidence when discussing interoperability.", string.Join(" ", bytes.Skip(Math.Max(0, bytes.Length - 2)).Take(2)));
            yield break;
        }

        yield return new ProtocolMapLine("raw", "Raw IEC-103 bytes", "The analyzer preserved this frame as raw evidence, but it could not classify it into the normal fixed/variable FT1.2 structure.", string.Join(" ", bytes));
    }

    private static string BuildControlMeaning(EvidenceRow row)
    {
        if (row.Direction == "TX")
        {
            return row.DataClass.Contains("Class 1", StringComparison.OrdinalIgnoreCase)
                ? "Master asks for pending Class 1 event data. This should be done only during ACD=1 event drain or bounded GI follow-up."
                : row.DataClass.Contains("Class 2", StringComparison.OrdinalIgnoreCase)
                    ? "Master performs normal Class 2 background polling."
                    : string.IsNullOrWhiteSpace(row.ReadableMeaning) ? "Master link/control action." : row.ReadableMeaning;
        }

        if (row.Direction == "RX")
        {
            if (row.Acd == "1")
            {
                return "Relay response indicates ACD=1, meaning Class 1 data is pending and the master may drain event data.";
            }

            if (row.ProtocolMeaning.Contains("FC=9", StringComparison.OrdinalIgnoreCase) || row.ReadableMeaning.Contains("ACK", StringComparison.OrdinalIgnoreCase))
            {
                return "Relay acknowledges the link/application command.";
            }

            return string.IsNullOrWhiteSpace(row.ProtocolMeaning) ? "Relay link response." : row.ProtocolMeaning;
        }

        return string.IsNullOrWhiteSpace(row.ReadableMeaning) ? "Link-layer control information." : row.ReadableMeaning;
    }

    private static string BuildAsduHeaderMeaning(EvidenceRow row)
    {
        if (row.AsduType == "-" && row.Cot == "-")
        {
            return "No ASDU payload is present in this link frame.";
        }

        return $"ASDU={row.AsduType}, COT={row.Cot}. This tells the tester what kind of relay information is being transferred and why it was sent.";
    }

    private static string BuildSignalAddressMeaning(EvidenceRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.SemanticLabel))
        {
            return $"Mapped signal: {row.SemanticLabel}. Raw address remains {row.SignalOrAddress}.";
        }

        return row.FunInf == "-"
            ? "This ASDU has no decoded FUN/INF signal address."
            : $"Unmapped IEC-103 signal address {row.SignalOrAddress}. Add it to the user mapping profile to show a readable signal name.";
    }

    private static string BuildPayloadMeaning(EvidenceRow row)
    {
        var state = string.IsNullOrWhiteSpace(row.SemanticState) ? "state/value" : row.SemanticState;
        var time = string.IsNullOrWhiteSpace(row.RelayTime) || row.RelayTime == "-" ? "No relay timestamp decoded." : $"Relay timestamp: {row.RelayTime}.";

        if (row.AsduType.Contains("Measur", StringComparison.OrdinalIgnoreCase))
        {
            return $"Measurement payload. Decoded value/state: {state}. {time}";
        }

        if (row.AsduType.Contains("DPI", StringComparison.OrdinalIgnoreCase) || row.AsduType.Contains("time-tagged", StringComparison.OrdinalIgnoreCase))
        {
            return $"Protection/status event payload. Decoded state: {state}. {time}";
        }

        return $"Information element payload. Decoded state/value: {state}. {time}";
    }

    private static string[] SplitHexBytes(string rawHex)
    {
        return rawHex
            .Split(new[] { ' ', '|', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToArray();
    }

    private static IEnumerable<HexSegment> BuildHexSegments(EvidenceRow row)
    {
        var bytes = SplitHexBytes(row.RawHex);

        if (bytes.Length == 0)
        {
            yield break;
        }

        if (bytes[0].Equals("10", StringComparison.OrdinalIgnoreCase) && bytes.Length >= 5)
        {
            yield return new HexSegment("envelope", bytes[0], "FT1.2 fixed frame", "Fixed-length link frame envelope.");
            yield return new HexSegment("control", bytes[1], "Control", BuildControlMeaning(row));
            yield return new HexSegment("address", bytes[2], "Link address", "Relay/slave link address.");
            yield return new HexSegment("check", string.Join(" ", bytes.Skip(3).Take(2)), "Integrity", "Checksum and end byte.");
            yield break;
        }

        if (bytes[0].Equals("68", StringComparison.OrdinalIgnoreCase) && bytes.Length >= 9)
        {
            yield return new HexSegment("envelope", string.Join(" ", bytes.Take(4)), "FT1.2 variable frame", "Variable frame start and length block.");
            yield return new HexSegment("control", string.Join(" ", bytes.Skip(4).Take(2)), "Control + link", BuildControlMeaning(row));
            yield return new HexSegment("asdu", string.Join(" ", bytes.Skip(6).Take(Math.Min(4, Math.Max(0, bytes.Length - 8)))), "ASDU header", BuildAsduHeaderMeaning(row));

            if (bytes.Length > 11)
            {
                yield return new HexSegment("object", string.Join(" ", bytes.Skip(10).Take(2)), "Signal address", BuildSignalAddressMeaning(row));
            }

            var payloadEnd = Math.Max(12, bytes.Length - 2);
            if (payloadEnd > 12)
            {
                yield return new HexSegment("payload", string.Join(" ", bytes.Skip(12).Take(payloadEnd - 12)), "State / value / relay time", BuildPayloadMeaning(row));
            }

            yield return new HexSegment("check", string.Join(" ", bytes.Skip(Math.Max(0, bytes.Length - 2)).Take(2)), "Integrity", "Checksum and end byte.");
            yield break;
        }

        yield return new HexSegment("raw", string.Join(" ", bytes), "Raw frame", "Frame bytes are preserved as evidence. This frame is not recognized by the high-level mapper.");
    }

    private void HexSegment_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_pinnedProtocolMapKey != null && PinProtocolMapCheckBox?.IsChecked == true)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is HexSegment segment)
        {
            SetActiveProtocolMap(segment.Key);
        }
    }

    private void HexSegment_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_pinnedProtocolMapKey != null && PinProtocolMapCheckBox?.IsChecked == true)
        {
            return;
        }

        ClearActiveProtocolMap();
    }

    private void HexSegment_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is HexSegment segment)
        {
            _pinnedProtocolMapKey = segment.Key;
            if (PinProtocolMapCheckBox != null)
            {
                PinProtocolMapCheckBox.IsChecked = true;
            }
            SetActiveProtocolMap(segment.Key);
        }
    }

    private void ProtocolMapLine_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_pinnedProtocolMapKey != null && PinProtocolMapCheckBox?.IsChecked == true)
        {
            return;
        }

        if ((sender as FrameworkElement)?.DataContext is ProtocolMapLine line)
        {
            SetActiveProtocolMap(line.Key);
        }
    }

    private void ProtocolMapLine_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_pinnedProtocolMapKey != null && PinProtocolMapCheckBox?.IsChecked == true)
        {
            return;
        }

        ClearActiveProtocolMap();
    }

    private void ProtocolMapLine_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is ProtocolMapLine line)
        {
            _pinnedProtocolMapKey = line.Key;
            if (PinProtocolMapCheckBox != null)
            {
                PinProtocolMapCheckBox.IsChecked = true;
            }
            SetActiveProtocolMap(line.Key);
        }
    }

    private void ClearProtocolMapHighlight_Click(object sender, RoutedEventArgs e)
    {
        _pinnedProtocolMapKey = null;
        if (PinProtocolMapCheckBox != null)
        {
            PinProtocolMapCheckBox.IsChecked = false;
        }
        ClearActiveProtocolMap();
    }

    private void CopySelectedRawFrame_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFrameRow is null || string.IsNullOrWhiteSpace(_selectedFrameRow.RawHex) || _selectedFrameRow.RawHex == "-")
        {
            return;
        }

        Clipboard.SetText(_selectedFrameRow.RawHex);
        AppendSessionLog($"Copied raw frame #{_selectedFrameRow.Sequence} to clipboard.");
    }

    private void CopySelectedFrameDecode_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedFrameRow is null)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Frame #{_selectedFrameRow.Sequence} {BuildLineMonitorTitle(_selectedFrameRow)}");
        builder.AppendLine(BuildCompactFrameExplanation(_selectedFrameRow));
        builder.AppendLine();
        builder.AppendLine("Raw: " + _selectedFrameRow.RawHex);
        Clipboard.SetText(builder.ToString());
        AppendSessionLog($"Copied decoded frame #{_selectedFrameRow.Sequence} to clipboard.");
    }

    private void SetActiveProtocolMap(string key)
    {
        foreach (var line in SelectedProtocolMapLines)
        {
            line.IsActive = string.Equals(line.Key, key, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var segment in SelectedHexSegments)
        {
            segment.IsActive = string.Equals(segment.Key, key, StringComparison.OrdinalIgnoreCase);
        }
    }

    private void ClearActiveProtocolMap()
    {
        foreach (var line in SelectedProtocolMapLines)
        {
            line.IsActive = false;
        }

        foreach (var segment in SelectedHexSegments)
        {
            segment.IsActive = false;
        }
    }


    private void MainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, MainTabControl))
        {
            return;
        }

        ExportDataButton.IsEnabled = GetCurrentTabDataGrid() is not null;
        UpdateSegmentedNav(true);
    }

    private void SegmentedNav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is null)
        {
            return;
        }

        if (int.TryParse(button.Tag.ToString(), out var index) && index >= 0 && index < MainTabControl.Items.Count)
        {
            MainTabControl.SelectedIndex = index;
        }
    }

    private Button[] GetSegmentedNavButtons()
    {
        return new[]
        {
            NavOperatorButton,
            NavFrameButton,
            NavValueButton,
            NavEventButton,
            NavAssessmentButton,
            NavFindingsButton,
            NavDiagnosticsButton,
            NavNotesButton
        };
    }

    private void UpdateSegmentedNav(bool animated)
    {
        if (!IsLoaded || MainTabControl is null || SegmentedNavRoot is null || SegmentSlider is null || SegmentSliderTranslate is null)
        {
            return;
        }

        var buttons = GetSegmentedNavButtons();
        var index = Math.Clamp(MainTabControl.SelectedIndex, 0, buttons.Length - 1);
        var activeButton = buttons[index];

        if (activeButton.ActualWidth <= 0 || SegmentedNavRoot.ActualWidth <= 0)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateSegmentedNav(false)), DispatcherPriority.Loaded);
            return;
        }

        var position = activeButton.TransformToAncestor(SegmentedNavRoot).Transform(new Point(0, 0));
        var targetX = Math.Round(Math.Max(0, position.X));
        var targetWidth = Math.Round(Math.Max(72, activeButton.ActualWidth));

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var duration = animated ? TimeSpan.FromMilliseconds(150) : TimeSpan.Zero;

        SegmentSliderTranslate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation
        {
            To = targetX,
            Duration = duration,
            EasingFunction = ease
        });
        SegmentSlider.BeginAnimation(WidthProperty, new DoubleAnimation
        {
            To = targetWidth,
            Duration = duration,
            EasingFunction = ease
        });

        var activeBrush = Brushes.White;
        var inactiveBrush = (Brush)FindResource("Ink600Brush");

        for (var i = 0; i < buttons.Length; i++)
        {
            buttons[i].Foreground = i == index ? activeBrush : inactiveBrush;
            buttons[i].FontWeight = i == index ? FontWeights.Medium : FontWeights.Normal;
        }
    }

    private void ExportData_Click(object sender, RoutedEventArgs e)
    {
        var grid = GetCurrentTabDataGrid();
        if (grid is null)
        {
            MessageBox.Show(this, "The selected tab does not contain exportable grid data.", "Export Data", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var tabName = (MainTabControl.SelectedItem as TabItem)?.Header?.ToString() ?? "data";
        var safeName = string.Concat(tabName.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')).Trim('-');
        var dialog = new SaveFileDialog
        {
            Title = "Export selected tab data",
            Filter = "Tab-separated text (*.txt)|*.txt|All files (*.*)|*.*",
            FileName = $"ArIEC103-{safeName}.txt",
            AddExtension = true,
            DefaultExt = ".txt"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, BuildTabSeparatedText(grid), Encoding.UTF8);
        AppendSessionLog($"Data exported from {tabName}: {dialog.FileName}");
    }

    private DataGrid? GetCurrentTabDataGrid()
    {
        var header = (MainTabControl.SelectedItem as TabItem)?.Header?.ToString() ?? string.Empty;
        return header switch
        {
            "Operator Evidence" => EvidenceGrid,
            "Frame Trace" => FrameTraceGrid,
            "Value Viewer" => ValueGrid,
            "Event Log" => RelayEventGrid,
            "AutoTest Assessment" => AssessmentGrid,
            "Findings" => FindingsGrid,
            "Diagnostics" => DiagnosticsGrid,
            _ => null
        };
    }

    private static string BuildTabSeparatedText(DataGrid grid)
    {
        var visibleColumns = grid.Columns
            .Where(c => c.Visibility == Visibility.Visible)
            .OrderBy(c => c.DisplayIndex)
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine(string.Join("\t", visibleColumns.Select(c => EscapeTabValue(c.Header?.ToString() ?? string.Empty))));

        foreach (var item in grid.ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>())
        {
            var values = visibleColumns.Select(column => EscapeTabValue(ReadGridColumnValue(column, item)));
            builder.AppendLine(string.Join("\t", values));
        }

        return builder.ToString();
    }

    private static string ReadGridColumnValue(DataGridColumn column, object item)
    {
        if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding && binding.Path is not null)
        {
            var path = binding.Path.Path;
            if (!string.IsNullOrWhiteSpace(path))
            {
                var value = item.GetType().GetProperty(path)?.GetValue(item);
                return value?.ToString() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static string EscapeTabValue(string value)
    {
        return (value ?? string.Empty)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\t", " ", StringComparison.Ordinal)
            .Trim();
    }

    private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid)
        {
            return;
        }

        var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
        {
            return;
        }

        if (!row.IsSelected)
        {
            grid.SelectedItems.Clear();
            row.IsSelected = true;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child is not null)
        {
            if (child is T target)
            {
                return target;
            }

            child = VisualTreeHelper.GetParent(child);
        }

        return null;
    }

    private void BrowseMapping_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open IEC-103 Mapping Profile",
            Filter = "ArIEC103 mapping profile (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _mappingProfile = Iec103SignalMappingProfile.LoadFromFile(dialog.FileName);
            MappingProfilePathBox.Text = dialog.FileName;
            MappingProfileStatusText.Text = $"Loaded: {_mappingProfile.ProfileName} ({_mappingProfile.Signals.Count} signals)";
            AppendSessionLog("Mapping profile loaded: " + _mappingProfile.ProfileName);
        }
        catch (Exception ex)
        {
            AddUiDiagnostic("Warning", "Mapping", "IEC103-MAPPING-LOAD", "Mapping profile could not be loaded", ex.Message, "Check JSON syntax and ArIEC103 mapping profile schema.", ex);
            MessageBox.Show(this, ex.Message, "Mapping profile error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearMapping_Click(object sender, RoutedEventArgs e)
    {
        _mappingProfile = Iec103SignalMappingProfile.Empty;
        MappingProfilePathBox.Text = string.Empty;
        MappingProfileStatusText.Text = "No mapping profile loaded. Raw FUN/INF will be shown.";
        AppendSessionLog("Mapping profile cleared.");
    }

    private void UpdateValueAndEventViews(Iec103MasterEvidenceEvent item)
    {
        if (item.IsRelayValue)
        {
            var key = string.IsNullOrWhiteSpace(item.SignalKey)
                ? $"FUN{(item.FunctionType ?? 0):000}:INF{(item.InformationNumber ?? 0):000}"
                : item.SignalKey;

            UpsertValueRowStable(new ValueRow(new Iec103ValuePoint
            {
                Key = key,
                IsMapped = item.IsMappedSignal,
                SignalName = string.IsNullOrWhiteSpace(item.SignalName) ? $"FUN {item.FunctionType} / INF {item.InformationNumber}" : item.SignalName,
                SignalGroup = string.IsNullOrWhiteSpace(item.SignalGroup) ? "Unmapped" : item.SignalGroup,
                SignalType = item.SignalType,
                FunctionType = item.FunctionType,
                InformationNumber = item.InformationNumber,
                RawValue = item.SignalRawValue,
                DisplayValue = item.SignalDisplayValue,
                Source = item.Cot ?? string.Empty,
                CauseOfTransmission = item.Cot ?? string.Empty,
                AsduType = item.AsduType ?? string.Empty,
                RelayTimeText = item.RelayTimestampText,
                RelayTimeInvalid = item.RelayTimestampInvalid,
                ArrivalTimeUtc = item.TimestampUtc,
                RawHex = item.RawHex
            }));

            while (ValueRows.Count > 2000)
            {
                ValueRows.RemoveAt(ValueRows.Count - 1);
            }
        }

        if (item.IsRelayEdgeEvent)
        {
            var relayEventRow = new RelayEventRow(new Iec103RelayEventLogEntry
            {
                EvidenceSequenceNumber = item.SequenceNumber,
                RelayTimeText = item.RelayTimestampText,
                RelayTimeInvalid = item.RelayTimestampInvalid,
                ArrivalTimeUtc = item.TimestampUtc,
                IsMapped = item.IsMappedSignal,
                SignalName = string.IsNullOrWhiteSpace(item.SignalName) ? $"FUN {item.FunctionType} / INF {item.InformationNumber}" : item.SignalName,
                SignalGroup = string.IsNullOrWhiteSpace(item.SignalGroup) ? "Unmapped" : item.SignalGroup,
                SignalType = item.SignalType,
                FunctionType = item.FunctionType,
                InformationNumber = item.InformationNumber,
                PreviousValue = item.PreviousSignalValue,
                NewValue = item.SignalDisplayValue,
                EdgeReason = item.EdgeReason,
                CauseOfTransmission = item.Cot ?? string.Empty,
                AsduType = item.AsduType ?? string.Empty,
                RawHex = item.RawHex
            });

            _allRelayEventRows.Insert(0, relayEventRow);
            while (_allRelayEventRows.Count > MaxVisibleRelayEventRows)
            {
                _allRelayEventRows.RemoveAt(_allRelayEventRows.Count - 1);
                _visibleRelayEventsDropped++;
            }
            ApplyRelayEventFilter();
        }
    }


    private void EventLogFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyRelayEventFilter();
    }

    private void ApplyRelayEventFilter()
    {
        if (RelayEventRows is null)
        {
            return;
        }

        var filter = (EventLogFilterComboBox?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "All";
        RelayEventRows.Clear();

        foreach (var row in _allRelayEventRows)
        {
            if (ShouldIncludeRelayEvent(row, filter))
            {
                RelayEventRows.Add(row);
            }
        }
    }

    private static bool ShouldIncludeRelayEvent(RelayEventRow row, string filter)
    {
        if (filter.Equals("Digital status", StringComparison.OrdinalIgnoreCase))
        {
            return IsDigitalEvent(row);
        }

        if (filter.Equals("Analog", StringComparison.OrdinalIgnoreCase))
        {
            return IsAnalogEvent(row);
        }

        return true;
    }

    private static bool IsDigitalEvent(RelayEventRow row)
    {
        var text = string.Join(" ", row.Type, row.Cot, row.Signal, row.NewValue, row.Reason);
        return text.Contains("DPI", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("SPI", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("status", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("trip", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("pickup", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ON", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("OFF", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAnalogEvent(RelayEventRow row)
    {
        var text = string.Join(" ", row.Type, row.Cot, row.Signal, row.NewValue, row.Reason);
        return text.Contains("Measur", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Analog", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("current", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("voltage", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("Measurands", StringComparison.OrdinalIgnoreCase);
    }


    private void UpsertValueRowStable(ValueRow row)
    {
        var existingIndex = -1;
        for (var i = 0; i < ValueRows.Count; i++)
        {
            if (string.Equals(ValueRows[i].Key, row.Key, StringComparison.OrdinalIgnoreCase))
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex >= 0)
        {
            // Keep the row position stable while refreshing the value. This prevents
            // the Value Viewer from jumping up/down during high-volume polling.
            ValueRows[existingIndex] = row;
            return;
        }

        var insertAt = ValueRows.Count;
        var newRank = GetValueRowSortRank(row);
        for (var i = 0; i < ValueRows.Count; i++)
        {
            var current = ValueRows[i];
            var compare = GetValueRowSortRank(current).CompareTo(newRank);
            if (compare > 0 ||
                (compare == 0 && string.Compare(current.Group, row.Group, StringComparison.OrdinalIgnoreCase) > 0) ||
                (compare == 0 && string.Equals(current.Group, row.Group, StringComparison.OrdinalIgnoreCase) && string.Compare(current.Signal, row.Signal, StringComparison.OrdinalIgnoreCase) > 0))
            {
                insertAt = i;
                break;
            }
        }

        ValueRows.Insert(insertAt, row);
    }

    private static int GetValueRowSortRank(ValueRow row)
    {
        var text = string.Join(" ", row.Type, row.Cot, row.Signal, row.Group);
        if (text.Contains("DPI", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("SPI", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("time-tagged", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("status", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("trip", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("pickup", StringComparison.OrdinalIgnoreCase))
        {
            return 0; // digital / protection status first
        }

        if (text.Contains("measur", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("current", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("voltage", StringComparison.OrdinalIgnoreCase))
        {
            return 1; // analog / measurand after digital
        }

        return 2;
    }

    private static bool IsDiagnosticEvidence(Iec103MasterEvidenceEvent item)
    {
        return !string.IsNullOrWhiteSpace(item.ExceptionType)
               || item.Category.Contains("Error", StringComparison.OrdinalIgnoreCase)
               || item.Category.Contains("Warning", StringComparison.OrdinalIgnoreCase)
               || item.Category.Contains("Fault", StringComparison.OrdinalIgnoreCase)
               || item.Category.Contains("Diagnostic", StringComparison.OrdinalIgnoreCase)
               || item.Summary.Contains("timeout", StringComparison.OrdinalIgnoreCase)
               || item.Detail.Contains("exception", StringComparison.OrdinalIgnoreCase);
    }

    private void AddUiDiagnostic(string severity, string source, string code, string message, string detail, string recommendation, Exception? exception = null)
    {
        AddDiagnosticRow(new DiagnosticRow(severity, source, code, message, detail, recommendation, exception));
        UpdateBufferStatus();
    }

    private void AddDiagnosticRow(DiagnosticRow row)
    {
        DiagnosticRows.Add(row);
        while (DiagnosticRows.Count > MaxVisibleDiagnosticRows)
        {
            DiagnosticRows.RemoveAt(0);
            _visibleDiagnosticsDropped++;
        }
    }

    private void DiagnosticsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if ((sender as DataGrid)?.SelectedItem is not DiagnosticRow row)
        {
            DiagnosticDetailBox.Text = "Select a diagnostic row to view complete detail.";
            return;
        }

        DiagnosticDetailBox.Text = row.ToClipboardText();
    }

    private void CopySelectedDiagnostic_Click(object sender, RoutedEventArgs e)
    {
        if (DiagnosticsGrid.SelectedItem is DiagnosticRow row)
        {
            Clipboard.SetText(row.ToClipboardText());
            AppendSessionLog("Diagnostic row copied to clipboard.");
        }
    }

    private void CopyDiagnosticDetail_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(DiagnosticDetailBox.Text))
        {
            Clipboard.SetText(DiagnosticDetailBox.Text);
            AppendSessionLog("Diagnostic detail copied to clipboard.");
        }
    }

    private void UpdateStableHeader(string state, string detail)
    {
        StateText.Text = state;
        CompletionText.Text = "History below";
        StatusHistorySummaryText.Text = CompactSessionDetail(detail);
        StatusHistorySummaryText.ToolTip = string.IsNullOrWhiteSpace(detail) ? "-" : detail;
    }

    private static string CompactSessionDetail(string detail)
    {
        if (string.IsNullOrWhiteSpace(detail))
        {
            return "-";
        }

        var text = detail.Replace("Assessment:", "Assess:", StringComparison.OrdinalIgnoreCase)
            .Replace("Stopped by cancellation or requested duration.", "Stopped/duration reached.", StringComparison.OrdinalIgnoreCase)
            .Replace("Stopped by cancellation.", "Stopped by user.", StringComparison.OrdinalIgnoreCase);

        const int max = 74;
        return text.Length <= max ? text : text[..max] + "…";
    }

    private void SetRunUiState(bool isRunning)
    {
        StartButton.IsEnabled = !isRunning;
        StopButton.IsEnabled = isRunning;
        StopButton.ToolTip = isRunning ? "Disconnect and close transport" : "Disconnect and close transport";
        SetupButton.IsEnabled = !isRunning;
        SetupOverlay.Visibility = isRunning ? Visibility.Collapsed : SetupOverlay.Visibility;
        ExportMarkdownButton.IsEnabled = !isRunning && _lastResult != null;
        TransportModeComboBox.IsEnabled = !isRunning;
        PortComboBox.IsEnabled = !isRunning;
        BaudComboBox.IsEnabled = !isRunning;
        SerialModeComboBox.IsEnabled = !isRunning;
        LinkAddressBox.IsEnabled = !isRunning;
        CommonAddressBox.IsEnabled = !isRunning;
        DurationBox.IsEnabled = !isRunning;
        TimeoutBox.IsEnabled = !isRunning;
        Class2IntervalBox.IsEnabled = !isRunning;
        MaxDrainBox.IsEnabled = !isRunning;
        ResetRemoteLinkCheckBox.IsEnabled = !isRunning;
        ResetFcbCheckBox.IsEnabled = !isRunning;
        ClockSyncCheckBox.IsEnabled = !isRunning;
        GiCheckBox.IsEnabled = !isRunning;
        Class2StartupCheckBox.IsEnabled = !isRunning;
        MappingProfilePathBox.IsEnabled = !isRunning;
        BrowseMappingButton.IsEnabled = !isRunning;
        ClearMappingButton.IsEnabled = !isRunning;
    }

    private void ClearSessionView(bool clearLog)
    {
        EvidenceRows.Clear();
        FrameTraceRows.Clear();
        FindingRows.Clear();
        ValueRows.Clear();
        RelayEventRows.Clear();
        _allRelayEventRows.Clear();
        AssessmentRows.Clear();
        DiagnosticRows.Clear();
        while (_pendingEvidence.TryDequeue(out _)) { }
        while (_pendingFindings.TryDequeue(out _)) { }
        _visibleEvidenceDropped = 0;
        _visibleRelayEventsDropped = 0;
        _visibleLogLinesDropped = 0;
        _visibleDiagnosticsDropped = 0;
        _txCount = 0;
        _rxCount = 0;
        _class1Count = 0;
        _class2Count = 0;
        _noDataCount = 0;
        _dpiCount = 0;
        TxLed.Opacity = 0.28;
        RxLed.Opacity = 0.28;
        Class1Led.Opacity = 0.28;
        Class2Led.Opacity = 0.28;
        EventLed.Opacity = 0.28;
        DiagLed.Opacity = 0.28;
        TxRxText.Text = "0 / 0";
        ClassPollText.Text = "0 / 0";
        NoDataText.Text = "0";
        DpiText.Text = "0";
        FindingCountText.Text = "0";
        SelectedDetailText.Text = "Select evidence row to inspect decoded meaning.";
        SelectedRawText.Text = "-";
        _selectedFrameExplanation = "Select a frame. This panel translates raw bytes into commissioning meaning.";
        SelectedLineSummaryText.Text = _selectedFrameExplanation;
        SelectedProtocolMapLines.Clear();
        SelectedHexSegments.Clear();
        StatusHistorySummaryText.Text = "Visible session rows cleared.";
        UpdateBufferStatus();
        if (clearLog)
        {
            _sessionLogLines.Clear();
            SessionLogBox.Clear();
            StatusHistoryRows.Clear();
            AppendSessionLog("Session view cleared.");
        }
    }

    private void AppendSessionLog(string message)
    {
        _sessionLogLines.Enqueue($"{DateTime.Now:HH:mm:ss}  {message}");
        while (_sessionLogLines.Count > MaxSessionLogLines)
        {
            _sessionLogLines.Dequeue();
            _visibleLogLinesDropped++;
        }

        SessionLogBox.Text = string.Join(Environment.NewLine, _sessionLogLines);
        if (SessionLogBox.Text.Length > 0)
        {
            SessionLogBox.AppendText(Environment.NewLine);
        }
        SessionLogBox.ScrollToEnd();
        AddStatusHistoryRow(message);
        UpdateBufferStatus();
    }

    private void AddStatusHistoryRow(string message)
    {
        StatusHistoryRows.Insert(0, new StatusHistoryRow(DateTime.Now.ToString("HH:mm:ss"), ClassifyStatusMessage(message), message));
        while (StatusHistoryRows.Count > 160)
        {
            StatusHistoryRows.RemoveAt(StatusHistoryRows.Count - 1);
        }

        StatusHistorySummaryText.Text = CompactSessionDetail(message);
        StatusHistorySummaryText.ToolTip = message;
    }

    private static string ClassifyStatusMessage(string message)
    {
        if (message.Contains("fault", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("error", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("warning", StringComparison.OrdinalIgnoreCase))
        {
            return "Attention";
        }

        if (message.Contains("stopped", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("disconnect", StringComparison.OrdinalIgnoreCase))
        {
            return "Stopped";
        }

        if (message.Contains("starting", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("monitor", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("transport", StringComparison.OrdinalIgnoreCase))
        {
            return "Runtime";
        }

        return "Info";
    }

    private void ToggleStatusHistory_Click(object sender, RoutedEventArgs e)
    {
        _statusHistoryExpanded = !_statusHistoryExpanded;
        StatusHistoryPanel.Height = _statusHistoryExpanded ? double.NaN : 52;
        StatusHistoryGapRow.Height = _statusHistoryExpanded ? new GridLength(8) : new GridLength(0);
        StatusHistoryContentRow.Height = _statusHistoryExpanded ? new GridLength(118) : new GridLength(0);
        StatusHistoryGrid.Visibility = _statusHistoryExpanded ? Visibility.Visible : Visibility.Collapsed;
        StatusHistoryToggleText.Text = _statusHistoryExpanded ? "Hide" : "Show";
        StatusHistoryToggleIcon.Data = (Geometry)FindResource(_statusHistoryExpanded ? "ArIconChevronDownCircle" : "ArIconChevronUpCircle");
    }

    private void UpdateBufferStatus()
    {
        if (BufferStatusText == null)
        {
            return;
        }

        BufferStatusText.Text =
            $"Buffer: operator {EvidenceRows.Count}/{MaxVisibleEvidenceRows}, frames {FrameTraceRows.Count}/{MaxVisibleEvidenceRows}, events {RelayEventRows.Count}/{MaxVisibleRelayEventRows}, diagnostics {DiagnosticRows.Count}/{MaxVisibleDiagnosticRows}, queued {_pendingEvidence.Count}";
    }

}

public sealed record StatusHistoryRow(string Time, string Status, string Detail);
