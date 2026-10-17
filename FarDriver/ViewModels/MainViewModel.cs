using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using FarDriver.Services;

namespace FarDriver.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IBleService _ble;
    private string _selectedDevice;
    private string _status = "未连接";
    private string _log = "";

    public ObservableCollection<string> Devices { get; } = new ObservableCollection<string>();

    public string SelectedDevice
    {
        get => _selectedDevice;
        set { _selectedDevice = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string Log
    {
        get => _log;
        set { _log = value; OnPropertyChanged(); }
    }

    public MainViewModel()
    {
        _ble = Application.Current!.Services.GetService<IBleService>();
        _ble.OnLog += s => { Log += s + "\n"; };
        _ble.OnStatus += s => { Status = s; };
        _ble.OnError += s => { Log += "[ERR] " + s + "\n"; };
        _ble.OnDeviceFound += s =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Devices.Add(s);
            });
        };
    }

    public ICommand ScanCommand => new Command(async () =>
    {
        Devices.Clear();
        await _ble.ScanAsync();
    });

    public ICommand ConnectCommand => new Command(async () =>
    {
        if (string.IsNullOrEmpty(SelectedDevice)) { Status = "请先选择设备"; return; }
        var parts = SelectedDevice.Split('|');
        if (parts.Length > 1 && Guid.TryParse(parts[1].Trim(), out var id))
            await _ble.ConnectAsync(id);
        else
            Status = "设备 ID 解析失败";
    });

    public ICommand DisconnectCommand => new Command(async () => await _ble.DisconnectAsync());

    public ICommand ReadParametersCommand => new Command(async () => await _ble.ReadParametersAsync());

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string n = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
