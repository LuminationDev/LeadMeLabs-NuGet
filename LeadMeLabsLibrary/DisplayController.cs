using System;
using System.Runtime.InteropServices;

/**
 * Code adapted from https://www.c-sharpcorner.com/uploadfile/GemingLeader/changing-display-settings-programmatically/
 */

[StructLayout(LayoutKind.Sequential)]
public struct POINTL
{
    [MarshalAs(UnmanagedType.I4)]
    public int x;
    [MarshalAs(UnmanagedType.I4)]
    public int y;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct DEVMODE
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmDeviceName;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 dmSpecVersion;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 dmDriverVersion;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 dmSize;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 dmDriverExtra;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmFields;
    public POINTL dmPosition;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmDisplayOrientation;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmDisplayFixedOutput;
    [MarshalAs(UnmanagedType.I2)]
    public Int16 dmColor;
    [MarshalAs(UnmanagedType.I2)]
    public Int16 dmDuplex;
    [MarshalAs(UnmanagedType.I2)]
    public Int16 dmYResolution;
    [MarshalAs(UnmanagedType.I2)]
    public Int16 dmTTOption;
    [MarshalAs(UnmanagedType.I2)]
    public Int16 dmCollate;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string dmFormName;
    [MarshalAs(UnmanagedType.U2)]
    public UInt16 dmLogPixels;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmBitsPerPel;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmPelsWidth;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmPelsHeight;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmDisplayFlags;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmDisplayFrequency;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmICMMethod;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmICMIntent;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmMediaType;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmDitherType;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmReserved1;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmReserved2;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmPanningWidth;
    [MarshalAs(UnmanagedType.U4)]
    public UInt32 dmPanningHeight;
}

public static class DisplayController
{
    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DISP_CHANGE_BADMODE = -2;
    private const int DISP_CHANGE_RESTART = 1;

    [DllImport("User32.dll")]
    private static extern int ChangeDisplaySettings(
        ref DEVMODE devMode, int flags
    );

    [DllImport("User32.dll")]
    private static extern bool EnumDisplaySettings(
        string deviceName, int modeNum, ref DEVMODE devMode
    );
    
    public static void EnumerateSupportedModes()
    {
        DEVMODE mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);
        int modeIndex = 0; // 0 = The first mode

        Console.WriteLine("Supported Modes:");
        while (EnumDisplaySettings(null, modeIndex, ref mode))
        {
            Console.WriteLine("\t{0} by {1}, {2} bit, {3} degrees, {4} hertz",
                mode.dmPelsWidth, mode.dmPelsHeight,
                mode.dmBitsPerPel,
                mode.dmDisplayOrientation * 90,
                mode.dmDisplayFrequency);
            modeIndex++; // Move to the next mode
        }
    }

    public static bool IsDisplayModeSupported(int width, int height, int bitCount)
    {
        DEVMODE mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);

        int modeIndex = 0;
        while (EnumDisplaySettings(null, modeIndex, ref mode))
        {
            Console.WriteLine("Current Mode:\n\t" +
                              "{0} by {1}, {2} bit, {3} degrees, {4} hertz",
                mode.dmPelsWidth, mode.dmPelsHeight,
                mode.dmBitsPerPel,
                mode.dmDisplayOrientation * 90,
                mode.dmDisplayFrequency);
            if (mode.dmPelsWidth == width && mode.dmPelsHeight == height && mode.dmBitsPerPel == bitCount)
            {
                return true;
            }

            modeIndex++;
        }

        return false;
    }

    public static void WriteCurrentSettings()
    {
        DEVMODE mode = new DEVMODE();
        mode.dmSize = (ushort)Marshal.SizeOf(mode);

        if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref mode))
        {
            Console.WriteLine("Current Mode:\n\t" +
                "{0} by {1}, {2} bit, {3} degrees, {4} hertz",
                mode.dmPelsWidth, mode.dmPelsHeight,
                mode.dmBitsPerPel,
                mode.dmDisplayOrientation * 90,
                mode.dmDisplayFrequency);
        }
    }
    
    const int DM_BITSPERPEL = 0x40000;
    const int DM_DISPLAYFLAGS = 0x200000;
    const int DM_DISPLAYFREQUENCY = 0x400000;
    const int DM_PELSHEIGHT = 0x100000;
    const int DM_PELSWIDTH = 0x80000;
    const int DM_POSITION = 0x20;
    const int DM_DISPLAYORIENTATION = 0x80;
    const int DM_DISPLAYFIXEDOUTPUT = 0x20000000;

    public static bool ChangeDisplaySettings(int width, int height, int bitCount)
    {
        DEVMODE originalMode = new DEVMODE();
        originalMode.dmSize = (ushort)Marshal.SizeOf(originalMode);

        // Retrieving current settings to edit them
        EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref originalMode);

        // Making a copy of the current settings to allow resetting to the original mode
        DEVMODE newMode = originalMode;

        // Changing the settings
        newMode.dmPelsWidth = (uint)width;
        newMode.dmPelsHeight = (uint)height;
        newMode.dmBitsPerPel = (uint)bitCount;
        if ((width / height) > (16 / 9))
        {
            newMode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL | DM_DISPLAYFLAGS | DM_DISPLAYORIENTATION | DM_DISPLAYFIXEDOUTPUT;
        }
        else
        {
            newMode.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_DISPLAYFREQUENCY | DM_BITSPERPEL | DM_DISPLAYFLAGS | DM_DISPLAYORIENTATION;
        }

        // Capturing the operation result
        int result = ChangeDisplaySettings(ref newMode, 0);
        if (result == DISP_CHANGE_SUCCESSFUL)
        {
            return true;
        }
        else if (result == DISP_CHANGE_BADMODE)
        {
            return false;
        }
        else if (result == DISP_CHANGE_RESTART)
        {
            return false;
        }
        else
        {
            return false;
        }
    }
}