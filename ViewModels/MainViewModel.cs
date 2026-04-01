using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using LibreHardwareMonitor.Hardware;
using Spexts.Helpers;
using Spexts.Models;

namespace Spexts.ViewModels;

public class MainViewModel : BaseViewModel, IDisposable
{
    private Computer? _computer;
    private DispatcherTimer? _timer;
    private readonly UpdateVisitor _visitor = new();

    // Panel ViewModels
    public SystemInfoViewModel SystemInfo { get; } = new();
    public MotherboardViewModel Motherboard { get; } = new();
    public CpuViewModel Cpu { get; } = new();
    public MemoryViewModel Memory { get; } = new();
    public GpuViewModel Gpu { get; } = new();
    public StorageViewModel Storage { get; } = new();
    public BatteryViewModel Battery { get; } = new();
    public SecurityViewModel Security { get; } = new();
    public NetworkViewModel Network { get; } = new();

    // ═══════════ EXPORT ═══════════

    public RelayCommand ExportCommand { get; }

    private string _exportFormat = "TXT";
    public string ExportFormat
    {
        get => _exportFormat;
        set => SetProperty(ref _exportFormat, value);
    }

    public List<string> ExportFormats { get; } = ["TXT", "JSON"];

    // ═══════════ STATUS ═══════════

    private bool _isLoading = true;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string _statusText = "Initializing hardware scan...";
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    // ═══════════ WINDOW CHROME COMMANDS ═══════════

    public RelayCommand MinimizeCommand { get; }
    public RelayCommand MaximizeCommand { get; }
    public RelayCommand CloseCommand { get; }

    public MainViewModel()
    {
        ExportCommand = new RelayCommand(_ => ExportReport());
        MinimizeCommand = new RelayCommand(_ =>
        {
            if (Application.Current.MainWindow != null)
                Application.Current.MainWindow.WindowState = WindowState.Minimized;
        });
        MaximizeCommand = new RelayCommand(_ =>
        {
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.WindowState =
                    Application.Current.MainWindow.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
            }
        });
        CloseCommand = new RelayCommand(_ =>
        {
            Application.Current.MainWindow?.Close();
        });

        IsAdmin = App.IsAdmin;
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        // Step 1: Initialize LibreHardwareMonitor on background thread
        StatusText = "Initializing hardware monitor...";
        await Task.Run(() =>
        {
            try
            {
                _computer = new Computer
                {
                    IsCpuEnabled = true,
                    IsGpuEnabled = true,
                    IsMemoryEnabled = true,
                    IsMotherboardEnabled = true,
                    IsStorageEnabled = true,
                    IsBatteryEnabled = true,
                    IsControllerEnabled = true,
                };
                _computer.Open();
                _computer.Accept(_visitor);
            }
            catch
            {
                // LHM may fail without admin — continue with WMI-only data
            }
        });

        // Step 2: Load all panels concurrently via Task.Run
        StatusText = "Scanning system components...";
        await Task.WhenAll(
            Task.Run(() => SystemInfo.Load()),
            Task.Run(() => Motherboard.Load()),
            Task.Run(() => Cpu.Load(_computer)),
            Task.Run(() => Memory.Load()),
            Task.Run(() => Gpu.Load(_computer)),
            Task.Run(() => Storage.Load()),
            Task.Run(() => Battery.Load(_computer)),
            Task.Run(() => Security.Load()),
            Task.Run(() => Network.Load())
        );

        IsLoading = false;
        StatusText = "All systems scanned";

        // Step 3: Start live polling timer (1.5s)
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1.5)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        // Update LHM sensors on background thread
        await Task.Run(() =>
        {
            try { _computer?.Accept(_visitor); }
            catch { }
        });

        // Update live rows on UI thread (we're already on UI after await)
        Cpu.UpdateLive(_computer);
        Gpu.UpdateLive(_computer);
    }

    private void ExportReport()
    {
        ExportHelper.Export(ExportFormat,
            ("OS & System", SystemInfo.Rows),
            ("Motherboard & BIOS", Motherboard.Rows),
            ("CPU Details", Cpu.Rows),
            ("Memory (RAM)", Memory.Rows),
            ("GPU & Display", Gpu.Rows),
            ("Storage Drives", Storage.Rows),
            ("Battery", Battery.Rows),
            ("Security & Tweaks", Security.Rows),
            ("Network Diagnostics", Network.Rows)
        );
    }

    public void Dispose()
    {
        _timer?.Stop();
        try { _computer?.Close(); }
        catch { }
        GC.SuppressFinalize(this);
    }
}

// LHM Visitor pattern
public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer) => computer.Traverse(this);
    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (var sub in hardware.SubHardware)
            sub.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}
