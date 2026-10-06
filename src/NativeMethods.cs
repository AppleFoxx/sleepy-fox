using System;
using System.Runtime.InteropServices;

namespace SleepyFox
{
    /// <summary>
    /// Тонкая обёртка над Win32: сколько времени пользователь не трогал мышь и клавиатуру,
    /// отправка мультимедийной клавиши play/pause и освобождение иконок трея.
    /// </summary>
    internal static class NativeMethods
    {
        public const byte VkMediaPlayPause = 0xB3;

        public const uint KeyEventExtendedKey = 0x0001;
        public const uint KeyEventKeyUp = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        private struct LastInputInfo
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetLastInputInfo(ref LastInputInfo plii);

        [DllImport("kernel32.dll")]
        private static extern uint GetTickCount();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Миллисекунды с момента последнего ввода мышью или клавиатурой.
        /// Разница считается в uint, поэтому переполнение GetTickCount раз в 49 суток не мешает.
        /// </summary>
        public static long GetIdleMilliseconds()
        {
            LastInputInfo info = new LastInputInfo();
            info.cbSize = (uint)Marshal.SizeOf(typeof(LastInputInfo));
            info.dwTime = 0;

            if (!GetLastInputInfo(ref info))
            {
                return 0;
            }

            uint now = GetTickCount();
            uint idle = now - info.dwTime;
            return (long)idle;
        }

        /// <summary>Минуты с момента последнего ввода мышью или клавиатурой.</summary>
        public static double GetIdleMinutes()
        {
            return GetIdleMilliseconds() / 60000.0;
        }

        /// <summary>
        /// Нажимает мультимедийную клавишу play/pause.
        /// Клавиша работает как переключатель, поэтому жать её можно только тогда,
        /// когда точно известно, что звук сейчас идёт.
        /// </summary>
        public static void SendPlayPauseKey()
        {
            keybd_event(VkMediaPlayPause, 0, KeyEventExtendedKey, UIntPtr.Zero);
            System.Threading.Thread.Sleep(60);
            keybd_event(VkMediaPlayPause, 0, KeyEventExtendedKey | KeyEventKeyUp, UIntPtr.Zero);
        }
    }
}
