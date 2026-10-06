using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace SleepyFox
{
    /// <summary>
    /// Единственное место в программе, которое вообще ходит в сеть.
    /// Запускается только по явному выбору пользователя в меню трея
    /// («Проверить обновления»): один синхронный запрос к GitHub за номером
    /// последнего выпуска и ничего больше. Ни при запуске, ни по таймеру
    /// сеть не трогается, никакие данные о пользователе не отправляются.
    /// </summary>
    internal static class UpdateCheck
    {
        private const string LatestReleaseApi =
            "https://api.github.com/repos/AppleFoxx/sleepy-fox/releases/latest";

        private const string ReleasesPage =
            "https://github.com/AppleFoxx/sleepy-fox/releases/latest";

        /// <summary>GitHub требует заголовок User-Agent и без него отвечает 403.</summary>
        private const string UserAgent = "The-Sleepy-Fox";

        private const int TimeoutMilliseconds = 7000;
        private const string DialogTitle = "The Sleepy Fox";

        public static void CheckForUpdates()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;

                string error;
                string body = DownloadLatestRelease(out error);

                if (body == null)
                {
                    ReportFailure(error);
                    return;
                }

                string tag = ExtractTagName(body);
                int[] serverParts;
                if (tag == null || !TryParseVersion(tag, out serverParts))
                {
                    ReportFailure("в ответе GitHub не нашёлся номер версии");
                    return;
                }

                int[] myParts;
                if (!TryParseVersion(SleepyFoxContext.AppVersion, out myParts))
                {
                    ReportFailure("непонятная своя версия: " + SleepyFoxContext.AppVersion);
                    return;
                }

                Logger.Log("проверка обновлений: у меня " + SleepyFoxContext.AppVersion
                    + ", на GitHub " + tag);

                if (CompareVersions(serverParts, myParts) > 0)
                {
                    DialogResult answer = MessageBox.Show(
                        "Вышла версия " + tag + ". У вас " + SleepyFoxContext.AppVersion + "."
                        + Environment.NewLine + Environment.NewLine
                        + "Открыть страницу загрузки?",
                        DialogTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                    if (answer == DialogResult.Yes)
                    {
                        OpenReleasesPage();
                    }
                }
                else
                {
                    MessageBox.Show("У вас последняя версия (" + SleepyFoxContext.AppVersion + ").",
                        DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                // Никакое исключение не должно вылезти наружу.
                try
                {
                    ReportFailure(ex.Message);
                }
                catch (Exception)
                {
                    // даже показать сообщение не вышло — молчим, лиса работает дальше
                }
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // ---------------------------------------------------------------- сеть

        /// <summary>
        /// Один синхронный GET к GitHub. Возвращает тело ответа либо null,
        /// а причину неудачи кладёт в error.
        /// </summary>
        private static string DownloadLatestRelease(out string error)
        {
            error = null;
            HttpWebRequest request = null;
            WebResponse response = null;
            Stream stream = null;
            StreamReader reader = null;

            try
            {
                request = (HttpWebRequest)WebRequest.Create(LatestReleaseApi);
                request.UserAgent = UserAgent;
                request.Method = "GET";
                request.Timeout = TimeoutMilliseconds;
                request.ReadWriteTimeout = TimeoutMilliseconds;

                response = request.GetResponse();
                stream = response.GetResponseStream();

                if (stream == null)
                {
                    error = "GitHub вернул пустой ответ";
                    return null;
                }

                reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
            catch (WebException ex)
            {
                error = DescribeWebError(ex);
                return null;
            }
            catch (Exception ex)
            {
                error = ShortMessage(ex);
                return null;
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                else if (stream != null)
                {
                    stream.Close();
                }

                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>Короткая причина, понятная человеку без сетевой терминологии.</summary>
        private static string DescribeWebError(WebException ex)
        {
            if (ex.Status == WebExceptionStatus.Timeout)
            {
                return "истекло время ожидания";
            }

            if (ex.Status == WebExceptionStatus.NameResolutionFailure
                || ex.Status == WebExceptionStatus.ConnectFailure
                || ex.Status == WebExceptionStatus.ProxyNameResolutionFailure)
            {
                return "нет связи с GitHub";
            }

            HttpWebResponse httpResponse = ex.Response as HttpWebResponse;
            if (httpResponse != null)
            {
                int code = (int)httpResponse.StatusCode;
                string reason = httpResponse.StatusDescription;
                try
                {
                    httpResponse.Close();
                }
                catch (Exception)
                {
                    // закрыть ответ не вышло — не повод падать
                }

                if (reason != null && reason.Length > 0)
                {
                    return "GitHub ответил " + code.ToString(CultureInfo.InvariantCulture)
                        + " " + reason;
                }

                return "GitHub ответил " + code.ToString(CultureInfo.InvariantCulture);
            }

            return ShortMessage(ex);
        }

        private static string ShortMessage(Exception ex)
        {
            if (ex.Message != null && ex.Message.Length > 0)
            {
                return ex.Message;
            }

            return "неизвестная ошибка";
        }

        // ---------------------------------------------------------------- разбор ответа

        /// <summary>
        /// Достаёт значение поля "tag_name" из JSON простым разбором строки:
        /// находит имя поля, двоеточие и текст между кавычками.
        /// </summary>
        private static string ExtractTagName(string json)
        {
            if (json == null)
            {
                return null;
            }

            string marker = "\"tag_name\"";
            int markerIndex = json.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            int colonIndex = json.IndexOf(':', markerIndex + marker.Length);
            if (colonIndex < 0)
            {
                return null;
            }

            int firstQuote = json.IndexOf('"', colonIndex + 1);
            if (firstQuote < 0)
            {
                return null;
            }

            int secondQuote = json.IndexOf('"', firstQuote + 1);
            if (secondQuote < 0)
            {
                return null;
            }

            return json.Substring(firstQuote + 1, secondQuote - firstQuote - 1);
        }

        /// <summary>«v1.0.4» → массив чисел 1, 0, 4.</summary>
        private static bool TryParseVersion(string text, out int[] parts)
        {
            parts = null;

            if (text == null)
            {
                return false;
            }

            string value = text.Trim();
            if (value.Length == 0)
            {
                return false;
            }

            if (value[0] == 'v' || value[0] == 'V')
            {
                value = value.Substring(1);
            }

            if (value.Length == 0)
            {
                return false;
            }

            string[] pieces = value.Split('.');
            int[] numbers = new int[pieces.Length];

            for (int i = 0; i < pieces.Length; i++)
            {
                int number;
                if (!int.TryParse(pieces[i], NumberStyles.Integer, CultureInfo.InvariantCulture,
                        out number))
                {
                    return false;
                }

                if (number < 0)
                {
                    return false;
                }

                numbers[i] = number;
            }

            parts = numbers;
            return true;
        }

        /// <summary>Сравнение по числам, а не по строке: 1.0.10 новее, чем 1.0.9.</summary>
        private static int CompareVersions(int[] left, int[] right)
        {
            int length = left.Length > right.Length ? left.Length : right.Length;

            for (int i = 0; i < length; i++)
            {
                int a = i < left.Length ? left[i] : 0;
                int b = i < right.Length ? right[i] : 0;

                if (a != b)
                {
                    return a < b ? -1 : 1;
                }
            }

            return 0;
        }

        // ---------------------------------------------------------------- мелочи

        private static void ReportFailure(string reason)
        {
            if (reason == null || reason.Length == 0)
            {
                reason = "неизвестная ошибка";
            }

            Logger.Log("проверка обновлений не удалась: " + reason);

            MessageBox.Show("Не удалось проверить обновления: " + reason
                + ". Программа работает как обычно.",
                DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void OpenReleasesPage()
        {
            try
            {
                Process.Start(ReleasesPage);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть браузер: " + ShortMessage(ex),
                    DialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
