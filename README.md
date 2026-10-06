# The Sleepy Fox

A tiny Windows tray app for the very common situation: you fall asleep in front of a
laptop while watching a video. The video keeps playing, so Windows never goes to sleep
on its own, and the laptop runs until morning.

**The Sleepy Fox** sits in the notification area, and at night — when the mouse and the
keyboard have been untouched for a while — it pauses the video and, some minutes later,
puts the machine into hibernation (or sleep, your choice).

No installers, no services, no NuGet packages, no admin rights, and — under no
circumstances — desktop shortcuts.

---

## What it does

1. Every few minutes (5 by default) it asks Windows two questions:
   * is it night now, according to the night window you configured (e.g. 23:00 → 07:00);
   * how long has it been since the last mouse or keyboard input.
2. If it is night and nobody has touched the machine for the "fell asleep" threshold
   (60 minutes by default), it marks the moment.
   * If pausing is enabled, it measures the peak audio level on the default output
     device via WASAPI. **Only if sound is actually playing** (peak > 0.005) does it
     send the multimedia play/pause key. The key is a toggle, so pressing it blindly
     could start a video instead of pausing one — the sound check prevents that.
3. After the "go to sleep after" delay (15 minutes by default) with still no input, it
   calls `Application.SetSuspendState` with either `Hibernate` or `Suspend`.
4. Any mouse movement or keypress before that resets the mark and is written to the log —
   so watching a 3-hour movie with the mouse untouched is not mistaken for falling asleep
   until you actually stop touching it.
5. "Dry run" mode (checkbox *Только показывать, ничего не делать*) makes the fox log what
   it *would* do and touch nothing at all.

## Requirements

* Windows 7 SP1 or newer, 64-bit or 32-bit.
* .NET Framework 4.8 — already present on Windows 10 (1903+) and Windows 11. On older
  systems you may need to install it.
* Hibernation enabled, if you pick hibernation:
  `powercfg /hibernate on` in an elevated command prompt.

## How to build

### Option A — `build.cmd` (no Visual Studio needed)

The built-in C# 5 compiler from .NET Framework 4.8 is enough:

```bat
build.cmd
```

The script compiles `src\*.cs` with
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe /target:winexe /platform:anycpu /optimize+`
and puts the result in `bin\SleepyFox.exe`. Source paths are relative, so the script can be
double-clicked.

### Option B — MSBuild

```bat
msbuild src\SleepyFox.csproj /p:Configuration=Release /p:Platform=AnyCPU
```

The exe lands in `src\bin\Release\SleepyFox.exe`. This is also what
`.github/workflows/build.yml` does on `windows-latest`.

> The sources are plain C# 5 on purpose: no string interpolation, no `?.`, no `nameof`,
> no expression-bodied members, no auto-properties with initializers, no tuples. This is
> what the in-box `csc.exe` accepts, and it keeps the project buildable on a machine with
> nothing but Windows and .NET Framework installed.

## First run — read this

1. Build the exe and run it. A small crescent-moon icon with a `Z` appears in the tray.
2. Right-click the icon → **Настройки…**
3. **Turn on "Только показывать, ничего не делать" for the first night.** In that mode the
   fox only writes to the log file, so you can see exactly what it would have done without
   risking a surprise hibernation.
4. Read **Журнал…** the next morning. If everything looks sane, uncheck the box and let it
   work for real.

## Configuration

Right-click the tray icon → **Настройки…** (or double-click the icon). All labels are in
Russian, since this is a small home utility. The window has:

| Setting | Meaning | Default |
|---|---|---|
| Ночь с / до | Night window in hours (0–23). May cross midnight: from 23 to 7. | 23 → 7 |
| Считать заснувшей после, мин | Minutes without mouse/keyboard input before we assume you fell asleep. | 60 |
| Уходить в сон/гибернацию через, мин | Minutes after the video is paused and no input before suspending. | 15 |
| Гибернация / Сон | What to do: hibernate or sleep. | Hibernate |
| Ставить видео на паузу | Send the play/pause key when sound is playing. | On |
| Только показывать, ничего не делать | Dry run: log only, never press keys, never suspend. | Off |
| Проверять каждые, мин | How often to look at the clock and the idle timer. | 5 |
| Запускать при входе в систему | Write `SleepyFox` into `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. | Off |

The settings live in `%APPDATA%\SleepyFox\settings.ini` as plain `key=value` text and are
read at startup and written when you press **Сохранить**.

## Tray menu

* **Настройки…** — the settings window (double-clicking the icon does the same).
* **Проверить сейчас** — run one check immediately and show the result in a balloon tip.
* **Пауза до утра** / **Возобновить** — temporarily stop doing anything. The flag lives in
  memory only; it is not written to the settings file. It clears itself once the night
  window ends.
* **Журнал…** — the last 200 log lines, with a link to open the log file in Notepad.
* **О программе** — short help and the version.
* **Выход** — stop and remove the tray icon.

## Files it creates

Everything is under `%APPDATA%\SleepyFox\`:

* `settings.ini` — your settings;
* `state.txt` — the moment the fox decided you were asleep (used to survive a restart);
* `sleepyfox.log` — the log, trimmed to the last 500 lines once it exceeds 1 MB.

Nothing else is written anywhere: no registry keys other than the optional `Run` value,
no files in Program Files, no shortcuts, no scheduled tasks, no services.

## Honest limitations

* **The audio check is a heuristic.** A quiet scene in a video looks like silence, so the
  pause key is not sent (the machine still hibernates later, just with the video playing).
  And *any* sound on the default output device counts — music, a notification, a call —
  so the fox may pause something else.
* **The play/pause key is global.** It goes to whichever app currently owns the media keys
  (usually the last one that played something). With several players open, it can pause the
  wrong one. There is no per-application control and no "is the video fullscreen" check.
* **Playback is not resumed.** When you come back, the video stays paused; the fox does not
  send the key again, because that toggle could just as easily start playback again.
* **Hibernation may be unavailable.** If hibernation is disabled (`powercfg /hibernate off`),
  is blocked by a policy, or another program holds a power request, `SetSuspendState`
  returns `false`. The fox logs that honestly and retries on the next check. It cannot
  force the machine down.
* **Idle time is not the same as being asleep.** If you sit still for an hour at night
  watching something with headphones on, the fox will treat it as falling asleep. Raise the
  threshold or use the night window to narrow when it may act.
* **One instance per session.** A second launch just shows a message. Separate Windows
  users are not coordinated.
* **The tray icon is drawn in code** (a crescent and a `Z`), so it looks simple and is
  scaled by Windows. A real fox icon is still on the wish list.
* **Not code-signed**, so SmartScreen may warn you the first time. That is expected for a
  private build.
* **Not tested on a live machine by the author of this repository snapshot.** The build and
  the actual hibernation behaviour have to be verified on real Windows hardware.

## License

MIT — see [LICENSE](LICENSE).

---

# The Sleepy Fox (сонная лиса) — по-русски

Маленькая программа в трее для знакомой беды: засыпаешь перед ноутбуком под видео,
видео играет, Windows из-за этого не засыпает сама, и ноутбук работает до утра.

Лиса живёт рядом с часами и ночью, если мышь и клавиатуру давно не трогали:

1. ставит видео на паузу (только если слышит звук — клавиша play/pause работает как
   переключатель, и слепое нажатие могло бы, наоборот, включить видео);
2. ещё через некоторое время отправляет ноутбук в **гибернацию** (или в сон — как выбрано
   в настройках);
3. любое движение мыши или нажатие клавиши до этого сбрасывает отсчёт.

## Что нужно

* Windows 7 SP1 или новее.
* .NET Framework 4.8 — на Windows 10 (начиная с 1903) и Windows 11 уже стоит.
* Гибернация, если выбран этот вариант: в командной строке от администратора
  `powercfg /hibernate on`.

## Как собрать

**Способ 1, самый простой — `build.cmd`.** Ни Visual Studio, ни NuGet не нужны:
щёлкните по `build.cmd` дважды, готовый файл появится в `bin\SleepyFox.exe`.
Скрипт берёт встроенный компилятор
`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe`.

**Способ 2 — MSBuild:**
`msbuild src\SleepyFox.csproj /p:Configuration=Release /p:Platform=AnyCPU`,
готовый файл — `src\bin\Release\SleepyFox.exe`.

## Первый запуск — важный совет

На первую ночь **включите галочку «Только показывать, ничего не делать»**
(Настройки → Безопасность). В этом режиме лиса ничего не нажимает и никуда не уходит,
а только записывает в журнал, что сделала бы. Утром посмотрите **Журнал…** — если всё
разумно, галочку снимите.

## Настройки

Правый щелчок по значку в трее → **Настройки…** (или двойной щелчок по значку).
Настройки лежат в `%APPDATA%\SleepyFox\settings.ini` простым текстом.

* «ночь с» и «до» — часы 0–23, окно может переходить через полночь (с 23 до 7);
* «считать заснувшей после» — минут без мыши и клавиатуры (по умолчанию 60);
* «уходить в сон/гибернацию через» — минут после паузы (по умолчанию 15);
* «Гибернация» или «Сон» (по умолчанию гибернация);
* «Ставить видео на паузу» (включено);
* «Только показывать, ничего не делать» (выключено; на первый запуск лучше включить);
* «Проверять каждые» — минут (по умолчанию 5);
* «Запускать при входе в систему» — пишет или удаляет значение `SleepyFox` в
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`.

## Меню в трее

«Настройки…», «Проверить сейчас», «Пауза до утра» / «Возобновить», «Журнал…»,
«О программе», «Выход». «Пауза до утра» живёт только в памяти, в файл настроек не
сохраняется и снимается сама, когда ночное окно заканчивается.

## Честные ограничения

* Проверка звука — это догадка, а не знание: тихая сцена в видео выглядит как тишина,
  и пауза не нажмётся (ноутбук всё равно уснёт позже, просто с играющим видео). И любой
  звук на устройстве вывода считается «видео играет» — музыка, уведомление, звонок.
* Клавиша play/pause общая для всей системы: нажатие уходит в то приложение, которое
  сейчас владеет медиаклавишами. Если открыто несколько плееров, пауза может достаться
  не тому. Управления «по приложению» и проверки полноэкранного режима нет.
* Обратно видео не включается: когда вы вернулись, оно остаётся на паузе — лиса не жмёт
  клавишу второй раз, чтобы случайно не запустить воспроизведение.
* Гибернация может быть недоступна (выключена, запрещена политикой, другое приложение
  держит запрос на питание). Тогда лиса честно пишет в журнал, что уложить ноутбук не
  вышло, и пробует на следующей проверке. Заставить систему уснуть она не может.
* Бездействие — не то же самое, что сон: если вы час сидите неподвижно под ночным видео,
  лиса решит, что вы уснули. Поднимите порог или сузьте ночное окно.
* Один экземпляр на сеанс Windows; разные пользователи между собой не согласуются.
* Значок трея рисуется программно (месяц и буква Z) — настоящую иконку лисы ещё
  предстоит добавить.
* Подписи кода нет, поэтому SmartScreen при первом запуске может предупредить.
* **На живой машине это ещё не проверялось:** сборка и реальное поведение при
  гибернации требуют проверки на настоящей Windows.

## Лицензия

MIT, см. [LICENSE](LICENSE).
