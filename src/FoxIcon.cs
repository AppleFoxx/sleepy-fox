using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace SleepyFox
{
    /// <summary>
    /// Значок для трея и окон. Берётся по очереди, от лучшего к запасному:
    /// встроенный ресурс fox.ico (ключ /resource:src\fox.ico,fox.ico), затем
    /// значок самого exe-файла (ExtractAssociatedIcon) — он есть даже тогда,
    /// когда встроенного ресурса в сборке не оказалось, — и в самом конце
    /// значок, нарисованный кодом: ночное небо, месяц и буква Z.
    /// </summary>
    internal static class FoxIcon
    {
        private static readonly Color SkyColor = Color.FromArgb(255, 27, 42, 74);
        private static readonly Color MoonColor = Color.FromArgb(255, 255, 214, 102);
        private static readonly Color InkColor = Color.FromArgb(255, 235, 242, 255);

        public static Icon Create()
        {
            // Порядок попыток: встроенный ресурс, значок самого файла, рисование
            // кодом. Каждая попытка идёт в своём try/catch: если она не удалась,
            // управление молча переходит к следующей, а наружу не вылетает
            // ни одного исключения — без значка приложение остаться не должно.

            // 1. Настоящая иконка лисы из ресурсов сборки. Первой попыткой —
            // на другой сборке ресурс может быть на месте. Размер берём у
            // системы (SmallIconSize): это тот размер, который Windows ждёт
            // для значка в трее, поэтому картинка не будет мыльной.
            try
            {
                System.Reflection.Assembly asm = System.Reflection.Assembly.GetExecutingAssembly();
                using (System.IO.Stream s = asm.GetManifestResourceStream("fox.ico"))
                {
                    if (s != null)
                    {
                        return new Icon(s, System.Windows.Forms.SystemInformation.SmallIconSize);
                    }
                }
            }
            catch (Exception)
            {
                // Ресурса нет или он не читается — из-за значка падать не должны,
                // поэтому молча уходим к следующей попытке.
            }

            // 2. Основной путь: значок, привязанный к самому файлу программы.
            // Он есть у exe всегда, даже когда встроенного ресурса в сборке нет.
            try
            {
                string exe = System.Windows.Forms.Application.ExecutablePath;
                System.Drawing.Icon assoc = System.Drawing.Icon.ExtractAssociatedIcon(exe);
                if (assoc != null)
                {
                    // Конструктор с размером: Windows получает значок ровно того
                    // размера, какой ждёт для трея, а не растянутый.
                    return new System.Drawing.Icon(assoc, System.Windows.Forms.SystemInformation.SmallIconSize);
                }
            }
            catch (Exception)
            {
                // Файла нет, значка у него нет или он не читается — идём к рисованию.
            }

            // 3. Последний запасной вариант: значок рисуется кодом.
            try
            {
                return CreateDrawnIcon();
            }
            catch (Exception)
            {
                // Даже рисование не удалось — лучше вернуть пусто, чем уронить трей.
                return null;
            }
        }

        /// <summary>
        /// Запасной вариант: значок рисуется кодом — ночное небо, месяц и буква Z.
        /// </summary>
        private static Icon CreateDrawnIcon()
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
