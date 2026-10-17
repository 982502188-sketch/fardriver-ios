using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FarDriver.Ble;
using Plugin.BLE;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace FarDriver.Services
{
    public class BleService : IBleService
    {
        private IDevice _device;
        private ICharacteristic _charNotify;
        private ICharacteristic _charCommand;
        private ICharacteristic _charUpgrade;
        private IService _service;
        private byte _seq;
        private readonly object _seqLock = new object();

        public bool IsConnected => _device != null && _device.State == Plugin.BLE.Abstractions.DeviceState.Connected;

        public event Action<string> OnLog;
        public event Action<string> OnStatus;
        public event Action<string> OnError;
        public event Action<byte[]> OnPacket;
        public event Action<string> OnDeviceFound;

        public async Task ScanAsync(int timeoutMs = 10000, CancellationToken ct = default)
        {
            try
            {
                OnStatus?.Invoke("扫描中...");
                var adapter = CrossBluetoothLE.Current.Adapter;
                if (!CrossBluetoothLE.Current.State.Equals(BluetoothState.On))
                {
                    OnError?.Invoke("请先打开蓝牙");
                    return;
                }
                adapter.DeviceDiscovered += OnDeviceDiscovered;
                adapter.ScanTimeout = timeoutMs;
                await adapter.StartScanningForDevicesAsync(cancellationToken: ct);
                adapter.DeviceDiscovered -= OnDeviceDiscovered;
                OnStatus?.Invoke($"扫描完成，发现 {adapter.DiscoveredDevices.Count()} 台设备");
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"扫描失败: {ex.Message}");
            }
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs e)
        {
            var name = e.Device.Name ?? "";
            if (name.StartsWith(BleUuids.DeviceNamePrefix, StringComparison.OrdinalIgnoreCase)
                || name.Contains("FOC")
                || name.Contains("YUANQU"))
            {
                OnDeviceFound?.Invoke($"{name}|{e.Device.Id}|{e.Device.Rssi}");
                OnLog?.Invoke($"[发现] {name}  RSSI={e.Device.Rssi}");
            }
        }

        public async Task<bool> ConnectAsync(Guid deviceId, CancellationToken ct = default)
        {
            try
            {
                OnStatus?.Invoke("正在连接...");
                var adapter = CrossBluetoothLE.Current.Adapter;
                var target = adapter.DiscoveredDevices.FirstOrDefault(d => d.Id == deviceId)
                             ?? adapter.DiscoveredDevices.FirstOrDefault(d => d.Name?.StartsWith("YuanQu") == true);
                if (target == null)
                {
                    OnError?.Invoke("未找到目标设备，请先扫描");
                    return false;
                }
                await adapter.ConnectToDeviceAsync(target, new ConnectParameters(autoConnect: true, forceBond: false), ct);
                _device = target;
                OnLog?.Invoke($"[连接] 成功: {target.Name}");
                OnStatus?.Invoke("已连接，正在发现服务...");

                if (!await DiscoverServiceAsync(ct))
                {
                    OnError?.Invoke("未找到 0xFFE0 服务，可能不是远驱控制器");
                    await DisconnectAsync(ct);
                    return false;
                }

                if (_charNotify != null)
                {
                    _charNotify.ValueUpdated += OnNotifyValueUpdated;
                    await _charNotify.StartUpdatesAsync();
                    OnLog?.Invoke($"[订阅] {_charNotify.Id}");
                }

                OnStatus?.Invoke("控制器已连接");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"连接失败: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> DiscoverServiceAsync(CancellationToken ct)
        {
            var services = await _device.DiscoverServices();
            foreach (var s in services)
            {
                OnLog?.Invoke($"[服务] {s.Id}");
            }

            _service = services.FirstOrDefault(s => s.Id.ToString().ToLower().Contains("ffe0"));
            if (_service == null)
                return false;

            var chars = await _service.DiscoverCharacteristics();
            foreach (var c in chars)
            {
                var id = c.Id.ToString().ToLower();
                OnLog?.Invoke($"  [特征] {c.Id}  properties={c.CharacteristicProperties}");
                if (id.Contains("ffec")) _charNotify = c;
                else if (id.Contains("fff7")) _charCommand = c;
                else if (id.Contains("fff8")) _charUpgrade = c;
            }

            if (_charNotify == null)
                _charNotify = chars.FirstOrDefault(c => c.CanNotify);
            if (_charCommand == null)
                _charCommand = chars.FirstOrDefault(c => c.CanWrite) ?? chars.FirstOrDefault(c => c.CanWriteWithoutResponse);
            if (_charUpgrade == null)
                _charUpgrade = chars.LastOrDefault(c => c.CanWriteWithoutResponse);

            return _charNotify != null || _charCommand != null;
        }

        private void OnNotifyValueUpdated(object sender, CharacteristicUpdatedEventArgs e)
        {
            var d = e.Characteristic.Value;
            if (d == null || d.Length == 0) return;
            OnPacket?.Invoke(d);
            OnLog?.Invoke($"[RX] {ToHex(d)}");
        }

        private byte NextSeq()
        {
            lock (_seqLock)
            {
                _seq = (byte)((_seq + 1) & 0xFF);
                return _seq;
            }
        }

        public async Task SendAsync(byte[] packet, CancellationToken ct = default)
        {
            if (_charCommand == null)
            {

cat > ~/fardriver_maui/FarDriver/Views/SplashPage.xaml << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="FarDriver.Views.SplashPage"
             BackgroundColor="#2196F3">
    <VerticalStackLayout HorizontalOptions="Center" VerticalOptions="Center">
        <Label Text="远驱电控" TextColor="White" FontSize="28" FontAttributes="Bold" />
        <Label Text="FarDriver Motor Controller" TextColor="White" FontSize="14" Opacity="0.8" />
    </VerticalStackLayout>
</ContentPage>
