# Privacy policy / Политика приватности

**The Sleepy Fox collects nothing.**

* The application makes **no network connections** on its own: no telemetry, no automatic update checks, no analytics, no crash reports. Nothing runs at startup or on a timer.
* The one exception is the **Проверить обновления** ("Check for updates") item in the tray menu, and it runs only if you pick it yourself. It sends a single request to GitHub (`https://api.github.com/repos/AppleFoxx/sleepy-fox/releases/latest`) and reads the latest release number, so it can tell you whether a newer version exists. Nothing about you is sent — no identifiers, no settings, no usage data — and the request never happens at startup or on a timer. If you never pick that item, the application never accesses the network.
* It reads two things locally: when the mouse and keyboard were last used, and whether the default audio output is currently producing sound.
* It writes only local files, in `%APPDATA%\SleepyFox\`: `settings.ini`, `state.txt` and `sleepyfox.log`.
* No personal data leaves your computer, and the authors receive nothing about you.

Русский: приложение **ничего не собирает** и само в сеть не ходит: ни телеметрии,
ни автоматической проверки обновлений, ни аналитики, ни отчётов об ошибках — ни при запуске,
ни по таймеру. Единственное исключение — пункт меню трея **«Проверить обновления»**, и только
если вы выберете его сами: тогда приложение один раз спрашивает у GitHub
(`https://api.github.com/repos/AppleFoxx/sleepy-fox/releases/latest`) номер последней версии,
чтобы сказать, вышла ли новая. Никакие данные о вас при этом не отправляются — ни
идентификаторы, ни настройки, ни сведения об использовании; в фоне и при запуске этот запрос
не делается никогда. Не нажимать этот пункт — значит вообще не обращаться к сети. Локально
приложение смотрит только две вещи: когда последний раз трогали мышь и клавиатуру, и есть ли
сейчас звук на устройстве вывода. Пишет только свои файлы в `%APPDATA%\SleepyFox\`. Никакие
личные данные никуда не уходят.

The project is open source (MIT). Questions and issues: <https://github.com/AppleFoxx/sleepy-fox/issues>.
