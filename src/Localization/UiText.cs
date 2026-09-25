using System.Globalization;
using System.Resources;

namespace SidebarDiagnostics.App.Localization;

public static class UiText
{
    internal static ResourceManager Resources { get; } = new(
        "SidebarDiagnostics.App.Localization.Strings", typeof(UiText).Assembly);

    public static string Get(string key, CultureInfo? culture = null) =>
        Resources.GetString(key, culture ?? CultureInfo.CurrentUICulture) ?? key;

    public static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), arguments);

    public static string Translate(string text) => text switch
    {
        "Primary" => Get("TextPrimary"),
        "Pip-Boy Green" => Get("TextPipBoyGreen"),
        "Amber" => Get("TextAmber"),
        "Ice Blue" => Get("TextIceBlue"),
        "Cyan" => Get("TextCyan"),
        "Red" => Get("TextRed"),
        "Purple" => Get("TextPurple"),
        "Gold" => Get("TextGold"),
        "Rose" => Get("TextRose"),
        "Not tested" => Get("TextNotTested"),
        "Testing" => Get("TextTesting"),
        "Starting" => Get("TextStarting"),
        "Collecting system data" => Get("TextCollectingSystemData"),
        "Detecting hardware sensors" => Get("TextDetectingHardwareSensors"),
        "Global shortcuts are initializing." => Get("TextShortcutsInitializing"),
        "Value" => Get("TextValue"),
        "External JSON metric" => Get("TextExternalJsonMetric"),
        "Network" => Get("TextNetwork"),
        "Model" => Get("TextModel"),
        "Vendor" => Get("TextVendor"),
        "Architecture" => Get("TextArchitecture"),
        "Load" => Get("TextLoad"),
        "Logical processors" => Get("TextLogicalProcessors"),
        "Used" => Get("TextUsed"),
        "Free" => Get("TextFree"),
        "Total" => Get("TextTotal"),
        "Format" => Get("TextFormat"),
        "VRAM used" => Get("TextVRAMUsed"),
        "VRAM total" => Get("TextVRAMTotal"),
        "VRAM free" => Get("TextVRAMFree"),
        "Shared memory used" => Get("TextSharedMemoryUsed"),
        "Type" => Get("TextType"),
        "MAC address" => Get("TextMACAddress"),
        "Link speed" => Get("TextLinkSpeed"),
        "Download" => Get("TextDownload"),
        "Upload" => Get("TextUpload"),
        "IP addresses" => Get("TextIPAddresses"),
        "External IP address" => Get("TextExternalIPAddress"),
        "Volume" => Get("TextVolume"),
        "Storage device" => Get("TextStorageDevice"),
        "Motherboard" => Get("TextMotherboard"),
        "Controller" => Get("TextController"),
        "RAM" => Get("TextRAM"),
        "Live monitoring" => Get("TextLiveMonitoring"),
        "Time only" => Get("TextTimeOnly"),
        "Month and day" => Get("TextMonthAndDay"),
        "Short date" => Get("TextShortDate"),
        "Long date" => Get("TextLongDate"),
        "None" => Get("TextNone"),
        "Left" => Get("TextLeft"),
        "Right" => Get("TextRight"),
        "Available" => Get("TextAvailable"),
        "Unavailable" => Get("TextUnavailable"),
        _ => text
    };
}
