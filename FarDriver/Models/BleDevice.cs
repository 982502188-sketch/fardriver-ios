namespace FarDriver.Models;

public class BleDevice
{
    public string Name { get; set; } = "";
    public string Id { get; set; } = "";
    public int Rssi { get; set; }
    public override string ToString() => Name;
}
