<p align="center">
  <img src="docs/logo.png" width="100" alt="The Sleepy Fox">
</p>

# The Sleepy Fox

A tiny Windows tray app for a common situation: you fall asleep in front of a laptop while a
video plays. Windows never sleeps while video is playing, so the machine runs all night.

**What it does.** At night, if the mouse and keyboard have been idle long enough, the fox pauses
the video and some minutes later hibernates the machine (or sleeps it — your choice). Before
pressing play/pause it checks that sound is actually playing: the key is a toggle, so pressing
it blind could *start* a video instead of pausing one.

`150 KB` · no installer · no services · no admin rights · MIT

## Screenshots

<p align="center">
  <img src="docs/screenshots/menu.png" width="230" alt="Tray menu">
  <img src="docs/screenshots/settings.png" width="310" alt="Settings">
  <img src="docs/screenshots/journal.png" width="590" alt="Journal of a real night: pause at 23:58, hibernation at 00:13">
</p>

## Download and run

Take `SleepyFox.exe` from the [latest release](https://github.com/AppleFoxx/sleepy-fox/releases/latest),
run it, right-click the tray icon. **For the first night** tick *Только показывать, ничего не
делать* (dry run): the fox only writes to its log, so you can see what it would do without risking
a surprise hibernation. Read **Журнал…** in the morning, then untick it.

Windows 7 SP1+ (32/64-bit) with .NET Framework 4.8 — already on Windows 10 (1903+) and Windows 11.
For hibernation: `powercfg /hibernate on` in an elevated command prompt. The window text is Russian.

Settings live in `%APPDATA%\SleepyFox\settings.ini` as plain text: the night window (23:00→07:00),
the idle threshold (60 min), the delay after the pause (15 min), hibernate or sleep, "pause only
when sound is playing", dry run, the check interval (5 min) and autostart. Nothing is written
outside `%APPDATA%\SleepyFox\` — no shortcuts, no scheduled tasks, no services.

## Honest limitations

* **Both checks are heuristics.** A quiet scene looks like silence (no pause — the machine still
  hibernates, just with the video playing); *any* sound — music, a call, a notification — counts,
  so the fox may pause something else; and sitting still with headphones for an hour looks like
  falling asleep.
* **The play/pause key is global** — with several players open it can hit the wrong one.
* **Playback is not resumed**, on purpose: the same toggle could start the video again.
* **Hibernation may be unavailable** (turned off, blocked by policy, or another program holds a
  power request). The fox logs that and retries; it cannot force the machine down.
* **No network access, one exception:** the **Проверить обновления** button asks GitHub once for
  the latest release number and sends nothing about you.
* **Not code-signed yet**, so SmartScreen may warn — see [Code signing policy](#code-signing-policy).

*Verified: builds on GitHub Actions and with the in-box `csc.exe`; a whole night on a live
Windows 11 laptop (07 → 08.10.2026) — paused at 23:58, hibernated at 00:13, slept until morning.
The first real night (06 → 07.10) exposed a bug — the app took its **own** media-key press for the
user coming back; fixed in 1.0.7. Not verified: Windows 10; there is no macOS version.*

## Code signing policy

**Releases are not signed yet**, so SmartScreen may warn. An application for free code signing was
submitted to the [SignPath Foundation](https://signpath.org); they asked us to come back once the
project shows more usage. This section will be updated either way.

When signing is in place: free code signing provided by [SignPath.io](https://about.signpath.io),
certificate by [SignPath Foundation](https://signpath.org). Committers, reviewers and approvers:
[Katya (AppleFoxx)](https://github.com/AppleFoxx) — every release is approved by hand. This program
will not transfer any information to other networked systems unless specifically requested by the
user or the person installing or operating it — see [PRIVACY.md](PRIVACY.md).

## Build from source

`build.cmd` (in-box C# 5 compiler, no Visual Studio, no NuGet) or
`msbuild src\SleepyFox.csproj /p:Configuration=Release`. The sources are deliberately plain C# 5.

## License

MIT — see [LICENSE](LICENSE).

---

# The Sleepy Fox (сонная лиса) — по-русски

<p align="center">
  <img src="docs/logo.png" width="80" alt="Сонная лиса">
</p>

Маленькая программа в трее: засыпаешь перед компьютером под видео, видео играет, Windows из-за
этого не засыпает сама, и компьютер работает до утра.

**Что она делает.** Ночью, если мышь и клавиатуру давно не трогали, лиса ставит видео на паузу,
а ещё через время отправляет компьютер в гибернацию (или в сон — как выбрано). Перед нажатием она
слушает: клавиша play/pause — переключатель, и слепое нажатие могло бы, наоборот, включить видео.

`150 КБ` · без установщика · без служб · без прав администратора · MIT

## Скачать и запустить

Взять `SleepyFox.exe` из [последнего релиза](https://github.com/AppleFoxx/sleepy-fox/releases/latest),
запустить, правый щелчок по значку в трее. **На первую ночь** включить «Только показывать, ничего
не делать» (режим проверки) — лиса только пишет в журнал, что сделала бы.

Windows 7 SP1 и новее (32/64 бита), .NET Framework 4.8 — на Windows 10 (с 1903) и 11 уже стоит.
Для гибернации: `powercfg /hibernate on` от администратора.

Настройки лежат в `%APPDATA%\SleepyFox\settings.ini` простым текстом: ночное окно (23:00→07:00),
порог (60 мин), действие через (15 мин), гибернация или сон, «пауза только если идёт звук», режим
проверки, проверка каждые 5 мин, автозапуск. Больше нигде ничего не пишется.

## Честные ограничения

* **Обе проверки — догадки.** Тихая сцена выглядит как тишина (паузы не будет, но компьютер всё
  равно уснёт позже, просто с играющим видео); любой звук — музыка, звонок, уведомление —
  считается «видео играет», и лиса может поставить на паузу не то; а час неподвижности в наушниках
  выглядит как сон.
* **Клавиша play/pause общая для системы** — если открыто несколько плееров, пауза может достаться
  не тому.
* **Обратно видео не включается** — намеренно: та же клавиша могла бы запустить его снова.
* **Гибернация может быть недоступна** (выключена, запрещена политикой, другое приложение держит
  запрос). Лиса пишет это в журнал и пробует снова; заставить систему уснуть она не может.
* **В сеть программа не ходит** — кроме кнопки **«Проверить обновления»**: она один раз спрашивает
  у GitHub номер последней версии и ничего о вас не отправляет.
* **Подписи кода пока нет** — SmartScreen может предупредить. См. ниже.

*Проверено: сборка в GitHub Actions и встроенным `csc.exe`; целая ночь на живом ноутбуке с
Windows 11 (07 → 08.10.2026) — пауза в 23:58, гибернация в 00:13, машина проспала до утра. Первая
настоящая ночь (06 → 07.10) вскрыла ошибку — приложение засчитывало **своё же** нажатие за
возвращение человека; исправлено в 1.0.7. Не проверено: Windows 10; версии под macOS нет.*

## Политика подписи кода

**Релизы пока без подписи**, поэтому SmartScreen может предупредить. Заявку подали в
[SignPath Foundation](https://signpath.org); там попросили вернуться, когда проект наберёт больше
пользователей. Раздел обновим в любом случае.

Когда подпись появится: бесплатная подпись кода — от [SignPath.io](https://about.signpath.io),
сертификат — [SignPath Foundation](https://signpath.org). Коммиттеры, ревьюеры и утверждающие:
[Катя (AppleFoxx)](https://github.com/AppleFoxx), каждый релиз вручную. Программа не передаёт
данные другим сетевым системам, если этого явно не запросил пользователь — см.
[PRIVACY.md](PRIVACY.md).

## Сборка из исходников

`build.cmd` (встроенный компилятор C# 5, без Visual Studio и NuGet) или
`msbuild src\SleepyFox.csproj /p:Configuration=Release`. Исходники намеренно на простом C# 5.

## Лицензия

MIT, см. [LICENSE](LICENSE).
