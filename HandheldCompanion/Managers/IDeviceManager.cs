using System.Collections.Generic;

namespace HandheldCompanion.Managers
{
    public interface IDeviceManager
    {
        bool IsInitialized { get; set; }

        event DeviceManager.DInputDeviceArrivedEventHandler HidDeviceArrived;
        event DeviceManager.DInputDeviceRemovedEventHandler HidDeviceRemoved;
        event DeviceManager.InitializedEventHandler Initialized;
        event DeviceManager.GenericDeviceArrivedEventHandler UsbDeviceArrived;
        event DeviceManager.GenericDeviceRemovedEventHandler UsbDeviceRemoved;
        event DeviceManager.XInputDeviceArrivedEventHandler XUsbDeviceArrived;
        event DeviceManager.XInputDeviceRemovedEventHandler XUsbDeviceRemoved;

        PnPDetails FindDeviceFromHID(string InstanceId);
        PnPDetails FindDeviceFromUSB(string InstanceId);
        List<PnPDetails> GetDetails(ushort VendorId = 0, ushort ProductId = 0);
        PnPDetails GetDeviceByInterfaceId(string path);
        string? GetDeviceDesc(string PNPString);
        IList<System.Tuple<nuint, nuint>>? GetDeviceMemResources(string PNPString);
        string[]? GetDevices(System.Guid? classGuid);
        string GetManufacturerString(string path);
        PnPDetails GetPnPDeviceEx(string SymLink);
        string GetProductString(string path);
        byte GetXInputIndexAsync(string SymLink);
        void Start();
        void Stop();
        string SymLinkToInstanceId(string SymLink, string InterfaceGuid);
    }
}