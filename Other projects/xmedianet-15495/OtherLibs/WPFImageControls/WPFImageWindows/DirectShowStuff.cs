using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace WPFImageWindows
{

    public enum CameraControlProperty
    {
        Pan = 0,
        Tilt,
        Roll,
        Zoom,
        Exposure,
        Iris,
        Focus
    }

    [Flags]
    public enum CameraControlFlags
    {
        None = 0x0,
        Auto = 0x0001,
        Manual = 0x0002,
        Relative = 0x0010,
    }


    public enum LogitechPropertySet : int
    {
        KSPROPERTY_LP1_VERSION = 0,
        KSPROPERTY_LP1_DIGITAL_PAN = 1,
        KSPROPERTY_LP1_DIGITAL_TILT = 2,
        KSPROPERTY_LP1_DIGITAL_ZOOM = 3,
        KSPROPERTY_LP1_DIGITAL_PANTILTZOOM = 4,
        KSPROPERTY_LP1_EXPOSURE_TIME = 5,
        KSPROPERTY_LP1_FACE_TRACKING = 6,
        KSPROPERTY_LP1_LED = 7,
        KSPROPERTY_LP1_FINDFACE = 8,
        KSPROPERTY_LP1_LAST = 8,
    }


    public enum LVUVC_LP1_FACE_TRACKING_MODE
    {
        LVUVC_LP1_FACE_TRACKING_MODE_OFF,
        LVUVC_LP1_FACE_TRACKING_MODE_SINGLE,
        LVUVC_LP1_FACE_TRACKING_MODE_MULTIPLE,
    }


    public enum LVUVC_LP1_LED_MODE : ulong
    {
        LVUVC_LP1_LED_MODE_OFF,
        LVUVC_LP1_LED_MODE_ON,
        LVUVC_LP1_LED_MODE_BLINKING,
        LVUVC_LP1_LED_MODE_AUTO,
    }


    public enum LVUVC_LP1_FINDFACE_MODE
    {
        LVUVC_LP1_FINDFACE_MODE_NO_CHANGE,
        LVUVC_LP1_FINDFACE_MODE_OFF,
        LVUVC_LP1_FINDFACE_MODE_ON,
    }


    public enum LVUVC_LP1_FINDFACE_RESET
    {
        LVUVC_LP1_FINDFACE_RESET_NONE,
        LVUVC_LP1_FINDFACE_RESET_DEFAULT,
        LVUVC_LP1_FINDFACE_RESET_FACE,
    }

    public struct KSPROPERTY_LP1_HEADER
    {
        public uint ulFlags;
        public uint ulReserved1;
        public uint ulReserved2;
    }

    public struct KSPROPERTY_LP1_VERSION_S
    {
        KSPROPERTY_LP1_HEADER Header;
        ushort usMajor;
        ushort usMinor;
    }

    public struct KSPROPERTY_LP1_DIGITAL_PAN_S
    {
        KSPROPERTY_LP1_HEADER Header;
        int lPan;
    }

    public struct KSPROPERTY_LP1_DIGITAL_TILT_S
    {
        KSPROPERTY_LP1_HEADER Header;
        int lTilt;
    }

    public struct KSPROPERTY_LP1_DIGITAL_ZOOM_S
    {
        KSPROPERTY_LP1_HEADER Header;
        uint ulZoom;
    }

    public struct KSPROPERTY_LP1_DIGITAL_PANTILTZOOM_S
    {
        KSPROPERTY_LP1_HEADER Header;
        int lPan;
        int lTilt;
        uint ulZoom;
    }

    public struct KSPROPERTY_LP1_EXPOSURE_TIME_S
    {
        KSPROPERTY_LP1_HEADER Header;
        uint ulExposureTime;
    }

    public struct KSPROPERTY_LP1_FACE_TRACKING_S
    {
        KSPROPERTY_LP1_HEADER Header;
        uint ulMode;			// See LVUVC_LP1_FACE_TRACKING_MODE
    }

    public struct KSPROPERTY_LP1_LED_S
    {
        KSPROPERTY_LP1_HEADER Header;
        uint ulMode;			// See LVUVC_LP1_LED_MODE
        uint ulFrequency;
    }

    public struct _KSPROPERTY_LP1_FINDFACE_S
    {
        KSPROPERTY_LP1_HEADER Header;
        uint ulMode;			// See LVUVC_LP1_FINDFACE_MODE
        uint ulReset;		// See LVUVC_LP1_FINDFACE_RESET
    }

    public class LogitechGuids
    {
        public static Guid LogitechPropSetGuid = new Guid("CAAE4966-272C-44a9-B792-71953F89DB2B");
    }


    [ComImport, System.Security.SuppressUnmanagedCodeSecurity, Guid("C6E13370-30AC-11d0-A18C-00A0C9118956"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IAMCameraControl
    {
        [PreserveSig]
        int GetRange(
            [In] CameraControlProperty Property,
            [Out] out int pMin,
            [Out] out int pMax,
            [Out] out int pSteppingDelta,
            [Out] out int pDefault,
            [Out] out CameraControlFlags pCapsFlags
            );

        [PreserveSig]
        int Set(
            [In] CameraControlProperty Property,
            [In] int lValue,
            [In] CameraControlFlags Flags
            );

        [PreserveSig]
        int Get(
            [In] CameraControlProperty Property,
            [Out] out int lValue,
            [Out] out CameraControlFlags Flags
            );
    }

    public enum KSPropertySupport
    {
        Get = 1,
        Set = 2
    }

   
    [ComImport, System.Security.SuppressUnmanagedCodeSecurity, Guid("31EFAC30-515C-11d0-A9AA-00AA0061BE93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IKsPropertySet
    {
        [PreserveSig]
        int Set(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidPropSet,
            [In] int dwPropID,
            [In] IntPtr pInstanceData,
            [In] int cbInstanceData,
            [In] IntPtr pPropData,
            [In] int cbPropData
            );

        [PreserveSig]
        int Get(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidPropSet,
            [In] int dwPropID,
            [In] IntPtr pInstanceData,
            [In] int cbInstanceData,
            [In, Out] IntPtr pPropData,
            [In] int cbPropData,
            [Out] out int pcbReturned
            );

        [PreserveSig]
        int QuerySupported(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid guidPropSet,
            [In] int dwPropID,
            [Out] out KSPropertySupport pTypeSupport
            );
    }
}
