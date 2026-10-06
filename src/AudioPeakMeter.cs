using System;
using System.Runtime.InteropServices;

namespace SleepyFox
{
    /// <summary>
    /// Пик звука на устройстве вывода по умолчанию (WASAPI, IAudioMeterInformation).
    /// Нужен ровно для одного: отличить «видео играет» от «в комнате тихо».
    /// Если измерить не удалось, возвращаем -1 и НИЧЕГО не нажимаем —
    /// слепая клавиша play/pause может запустить видео вместо паузы.
    /// </summary>
    internal static class AudioPeakMeter
    {
        /// <summary>Всё, что тише этого, считаем тишиной.</summary>
        public const float SilenceThreshold = 0.005f;

        private const int EDataFlowRender = 0; // eRender
        private const int ERoleMultimedia = 1; // eMultimedia
        private const int ClsCtxAll = 0x17;    // CLSCTX_ALL

        // CLSID_MMDeviceEnumerator
        [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
        private class MMDeviceEnumeratorComObject
        {
        }

        // IID_IMMDeviceEnumerator
        [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDeviceEnumerator
        {
            [PreserveSig]
            int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

            [PreserveSig]
            int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice endpoint);

            [PreserveSig]
            int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);

            [PreserveSig]
            int RegisterEndpointNotificationCallback(IntPtr client);

            [PreserveSig]
            int UnregisterEndpointNotificationCallback(IntPtr client);
        }

        // IID_IMMDevice
        [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IMMDevice
        {
            [PreserveSig]
            int Activate(ref Guid iid, int clsCtx, IntPtr activationParams,
                         [MarshalAs(UnmanagedType.IUnknown)] out object instance);

            [PreserveSig]
            int OpenPropertyStore(int stgmAccess, out IntPtr properties);

            [PreserveSig]
            int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

            [PreserveSig]
            int GetState(out int state);
        }

        // IID_IAudioMeterInformation
        [ComImport, Guid("C02216F6-8C67-4B5B-9D00-D008E73E0064"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IAudioMeterInformation
        {
            [PreserveSig]
            int GetPeakValue(out float peak);

            [PreserveSig]
            int GetMeteringChannelCount(out int channelCount);

            [PreserveSig]
            int GetChannelsPeakValues(int channelCount, [Out] float[] peakValues);

            [PreserveSig]
            int QueryHardwareSupport(out int hardwareSupportMask);
        }

        /// <summary>
        /// Пик звука (0..1) на устройстве вывода по умолчанию.
        /// При любой ошибке возвращает -1 и текст ошибки.
        /// </summary>
        public static float TryGetPeakValue(out string error)
        {
            error = null;

            object enumeratorObject = null;
            IMMDeviceEnumerator enumerator = null;
            IMMDevice device = null;
            object meterObject = null;

            try
            {
                enumeratorObject = new MMDeviceEnumeratorComObject();
                enumerator = (IMMDeviceEnumerator)enumeratorObject;

                int hr = enumerator.GetDefaultAudioEndpoint(EDataFlowRender, ERoleMultimedia, out device);
                if (hr != 0 || device == null)
                {
                    error = "нет устройства вывода по умолчанию (HRESULT 0x" + hr.ToString("X8") + ")";
                    return -1f;
                }

                Guid iid = typeof(IAudioMeterInformation).GUID;
                object instance;
                hr = device.Activate(ref iid, ClsCtxAll, IntPtr.Zero, out instance);
                if (hr != 0 || instance == null)
                {
                    error = "не открылся измеритель звука (HRESULT 0x" + hr.ToString("X8") + ")";
                    return -1f;
                }

                meterObject = instance;
                IAudioMeterInformation meter = (IAudioMeterInformation)instance;

                float peak;
                hr = meter.GetPeakValue(out peak);
                if (hr != 0)
                {
                    error = "не прочитался пик звука (HRESULT 0x" + hr.ToString("X8") + ")";
                    return -1f;
                }

                return peak;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return -1f;
            }
            finally
            {
                ReleaseQuietly(meterObject);
                ReleaseQuietly(device);
                ReleaseQuietly(enumerator);
            }
        }

        private static void ReleaseQuietly(object comObject)
        {
            if (comObject == null)
            {
                return;
            }

            try
            {
                if (Marshal.IsComObject(comObject))
                {
                    Marshal.ReleaseComObject(comObject);
                }
            }
            catch (Exception)
            {
                // освобождение COM-объекта не должно ломать саму проверку
            }
        }
    }
}
