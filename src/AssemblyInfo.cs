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
