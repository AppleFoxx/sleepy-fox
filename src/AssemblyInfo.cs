using System.Reflection;
using System.Runtime.InteropServices;
using SleepyFox;

// Версия берётся из одной константы SleepyFoxContext.AppVersion,
// чтобы «О программе» и свойства файла не расходились.
[assembly: AssemblyTitle("The Sleepy Fox")]
[assembly: AssemblyDescription("Сонная лиса: ставит видео на паузу и укладывает компьютер спать, когда вы уснули.")]
[assembly: AssemblyProduct("The Sleepy Fox")]
[assembly: AssemblyCopyright("Copyright (c) 2026 The Sleepy Fox contributors")]
[assembly: AssemblyVersion(SleepyFoxContext.AppVersion)]
[assembly: AssemblyFileVersion(SleepyFoxContext.AppVersion)]
[assembly: ComVisible(false)]

// Пометка, под какую версию среды собран файл. Без неё среда считает программу
// собранной под старую версию и выбирает устаревшие умолчания (именно так 07.10.2026
// сорвался запрос к GitHub: среда предлагала протокол TLS 1.0, который GitHub отвергает).
[assembly: System.Runtime.Versioning.TargetFramework(".NETFramework,Version=v4.8")]
