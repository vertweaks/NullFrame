using System;
using System.Diagnostics;
using System.Security.Principal;

namespace NullFrame.Services
{
    public static class SystemHelper
    {
        public static bool IsAdmin()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static (bool success, string output) RunPowerShell(string command, bool capture = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command -",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = capture,
                    RedirectStandardError = capture
                };
                using var proc = Process.Start(psi);
                if (proc == null) return (false, "");
                proc.StandardInput.Write(command);
                proc.StandardInput.Close();
                string output = capture ? proc.StandardOutput.ReadToEnd() : "";
                proc.WaitForExit(30000);
                return (proc.ExitCode == 0, output.Trim());
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static (bool success, string output) RunCmd(string command, bool capture = false)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = capture,
                    RedirectStandardError = capture
                };
                using var proc = Process.Start(psi);
                if (proc == null) return (false, "");
                string output = capture ? proc.StandardOutput.ReadToEnd() : "";
                proc.WaitForExit(30000);
                return (proc.ExitCode == 0, output.Trim());
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static bool RunBcdedit(string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "bcdedit.exe",
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return false;
                proc.WaitForExit(15000);
                return proc.ExitCode == 0;
            }
            catch { return false; }
        }

        public static string GetActivePowerScheme()
        {
            var (ok, output) = RunPowerShell("(powercfg /getactivescheme) -replace '.*:\\s*','' -replace '\\s*\\(.*',''", true);
            return ok ? output.Trim() : "";
        }

        public static bool RunNetsh(string args)
        {
            var (ok, _) = RunCmd($"netsh {args}");
            return ok;
        }

        public static bool RunSchtasks(string args)
        {
            var (ok, _) = RunCmd($"schtasks {args}");
            return ok;
        }
    }
}
