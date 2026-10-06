using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SleepyFox
{
    /// <summary>
    /// Значок трея рисуется программно: ночное небо, месяц и буква Z.
    /// Настоящую иконку лисы добавим позже, для этого достаточно заменить Create().
    /// </summary>
    internal static class FoxIcon
    {
        private static readonly Color SkyColor = Color.FromArgb(255, 27, 42, 74);
        private static readonly Color MoonColor = Color.FromArgb(255, 255, 214, 102);
        private static readonly Color InkColor = Color.FromArgb(255, 235, 242, 255);

        public static Icon Create()
        {
            using (Bitmap bitmap = new Bitmap(32, 32))
            {
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    graphics.Clear(Color.Transparent);

                    // ночное небо
                    using (SolidBrush sky = new SolidBrush(SkyColor))
                    {
                        graphics.FillEllipse(sky, 0, 0, 31, 31);
                    }

                    // месяц: круг, из которого вырезан второй круг цветом неба
                    using (SolidBrush moon = new SolidBrush(MoonColor))
                    {
                        graphics.FillEllipse(moon, 4, 5, 17, 17);
                    }
                    using (SolidBrush sky = new SolidBrush(SkyColor))
                    {
                        graphics.FillEllipse(sky, 11, 2, 17, 17);
                    }

                    // буква Z — «спать»
                    using (Font font = new Font("Segoe UI", 11f, FontStyle.Bold, GraphicsUnit.Pixel))
                    using (SolidBrush ink = new SolidBrush(InkColor))
                    {
                        StringFormat format = new StringFormat();
                        format.Alignment = StringAlignment.Center;
                        format.LineAlignment = StringAlignment.Center;
                        graphics.DrawString("Z", font, ink, new RectangleF(14f, 16f, 16f, 14f), format);
                    }
                }

                // GetHicon отдаёт неуправляемый дескриптор — копируем иконку и освобождаем его
                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon temporary = Icon.FromHandle(handle))
                    {
                        return (Icon)temporary.Clone();
                    }
                }
                finally
                {
                    NativeMethods.DestroyIcon(handle);
                }
            }
        }
    }
}
