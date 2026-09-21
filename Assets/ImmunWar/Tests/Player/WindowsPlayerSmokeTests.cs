using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace ImmunWar.Tests.Player
{
    public sealed class WindowsPlayerSmokeTests
    {
        [Test, Timeout(60000)]
        public void PackagedPlayerLaunchesMenuBattlePauseRestartAndExits()
        {
            var executable = Path.GetFullPath("Builds/Windows/ImmuneWar.exe"); if (!File.Exists(executable)) Assert.Ignore("Release player has not been built yet."); var log = Path.GetFullPath("Logs/windows-player-smoke.log"); Directory.CreateDirectory(Path.GetDirectoryName(log));
            using var process = Process.Start(new ProcessStartInfo(executable, $"--immunwar-smoke -logFile \"{log}\"") { UseShellExecute = false, CreateNoWindow = true }); Assert.That(process, Is.Not.Null); Assert.That(process.WaitForExit(45000), Is.True); Assert.That(process.ExitCode, Is.Zero); var text = File.ReadAllText(log); Assert.That(text, Does.Contain("IMMUNEWAR_SMOKE_MENU")); Assert.That(text, Does.Contain("IMMUNEWAR_SMOKE_BATTLE_PAUSE_RESTART_OK")); Assert.That(text, Does.Contain("IMMUNEWAR_SMOKE_EXIT_OK"));
        }
    }
}
