namespace OpenPronounceSharp.WinApp.Model
{
    public class WaveInDeviceItem
    {
        public int DeviceNumber { get; private set; }
        public string DisplayName { get; private set; }

        public WaveInDeviceItem(int deviceNumber, string productName)
        {
            this.DeviceNumber = deviceNumber;

            this.DisplayName = deviceNumber == -1
                ? $"Default ({productName})"
                : $"Device {deviceNumber} ({productName})";
        }

        public override string ToString() => DisplayName;
    }
}
