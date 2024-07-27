using System;
using System.DirectoryServices.AccountManagement;
using System.Management.Automation.Runspaces;
using System.Management.Automation;
using System.Net.NetworkInformation;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace RemoteInstallers
{
    public class RemoteInstaller
    {
        public class RemoteInstallResult
        {
            public string ComputerName { get; set; }
            public int ExitCode { get; set; }
            public string StandardOutput { get; set; }
            public string StandardError { get; set; }
            public bool IsAdmin { get; set; }
            public bool IsPSRemotingEnabled { get; set; }
        }

        private static async Task<RemoteInstallResult> InitializeRunspaceAsync(string remoteComputerName, Runspace runspace)
        {
            RemoteInstallResult result = new RemoteInstallResult
            {
                ComputerName = remoteComputerName,
                ExitCode = -1,
                StandardOutput = string.Empty,
                StandardError = string.Empty,
                IsAdmin = false,
                IsPSRemotingEnabled = false
            };

            try
            {
                runspace.Open();
                result.IsPSRemotingEnabled = true;
            }
            catch (Exception ex)
            {
                result.StandardError = $"PSRemoting is not enabled or the remote computer is unreachable: {ex.Message}";
                return result;
            }

            bool isAdmin = await CheckAdminRightsAsync(runspace);
            if (!isAdmin)
            {
                result.StandardError = "The current user is not an administrator on the remote computer.";
                return result;
            }

            result.IsAdmin = true;
            return result;
        }

        private static async Task<RemoteInstallResult> ExecuteRemoteCommandAsync(
            string remoteComputerName,
            string command,
            int timeoutInMinutes,
            CancellationToken cancellationToken)
        {
            return await ExecuteRemoteScriptAsync(remoteComputerName, command, timeoutInMinutes, cancellationToken);
        }

        private static async Task<RemoteInstallResult> ExecuteRemoteScriptAsync(
            string remoteComputerName,
            string script,
            int timeoutInMinutes,
            CancellationToken cancellationToken)
        {
            string connectionUri = $"http://{remoteComputerName}:5985/wsman";
            string shellUri = "http://schemas.microsoft.com/powershell/Microsoft.PowerShell";

            WSManConnectionInfo connectionInfo = new WSManConnectionInfo(new Uri(connectionUri))
            {
                SkipCACheck = true,
                SkipCNCheck = true,
                SkipRevocationCheck = true,
                AuthenticationMechanism = AuthenticationMechanism.Negotiate
            };

            using (Runspace runspace = RunspaceFactory.CreateRunspace(connectionInfo))
            {
                var result = await InitializeRunspaceAsync(remoteComputerName, runspace);
                if (!result.IsPSRemotingEnabled || !result.IsAdmin)
                {
                    return result;
                }

                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.Runspace = runspace;
                    powerShell.AddScript(script);

                    using (cancellationToken.Register(() => powerShell.Stop()))
                    {
                        var outputCollection = new PSDataCollection<PSObject>();
                        var invokeTask = Task.Factory.FromAsync(
                            powerShell.BeginInvoke<PSObject, PSObject>(null, outputCollection),
                            powerShell.EndInvoke
                        );

                        if (timeoutInMinutes > 0)
                        {
                            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(timeoutInMinutes), cancellationToken);
                            var completedTask = await Task.WhenAny(invokeTask, timeoutTask);
                            if (completedTask == timeoutTask)
                            {
                                result.StandardError = "Operation timed out.";
                                return result;
                            }
                        }
                        else
                        {
                            await invokeTask;
                        }

                        foreach (var outputItem in outputCollection)
                        {
                            result.StandardOutput += outputItem.ToString() + Environment.NewLine;
                        }

                        if (powerShell.Streams.Error.Count > 0)
                        {
                            foreach (var error in powerShell.Streams.Error)
                            {
                                result.StandardError += error.ToString() + Environment.NewLine;
                            }
                        }
                    }
                }
                return result;
            }
        }

        public static Task<RemoteInstallResult> RunMsiRemotelyAsync(
            string remoteComputerName,
            string arguments,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string command = $"Start-Process msiexec.exe -ArgumentList '{arguments}' -Wait -PassThru";
            return ExecuteRemoteCommandAsync(remoteComputerName, command, timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> RunExecutableRemotelyAsync(
            string remoteComputerName,
            string exePath,
            string arguments,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string command = $"Start-Process '{exePath}' -ArgumentList '{arguments}' -Wait -PassThru";
            return ExecuteRemoteCommandAsync(remoteComputerName, command, timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> RunMsuRemotelyAsync(
            string remoteComputerName,
            string msuPath,
            string arguments,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            return RunExecutableRemotelyAsync(remoteComputerName, "wusa.exe", $"\"{msuPath}\" {arguments}", timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> RunPowerShellScriptRemotelyAsync(
            string remoteComputerName,
            string scriptPath,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string arguments = $"-File \"{scriptPath}\"";
            return RunExecutableRemotelyAsync(remoteComputerName, "powershell.exe", arguments, timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> RunVBScriptRemotelyAsync(
            string remoteComputerName,
            string scriptPath,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string arguments = $"\"{scriptPath}\"";
            return RunExecutableRemotelyAsync(remoteComputerName, "cscript.exe", arguments, timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> RunBatchFileRemotelyAsync(
            string remoteComputerName,
            string batchFilePath,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string arguments = $"/c \"{batchFilePath}\"";
            return RunExecutableRemotelyAsync(remoteComputerName, "cmd.exe", arguments, timeoutInMinutes, cancellationToken);
        }

        public static Task<RemoteInstallResult> ImportRegFileRemotelyAsync(
            string remoteComputerName,
            string regFilePath,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string arguments = $"/s \"{regFilePath}\"";
            return RunExecutableRemotelyAsync(remoteComputerName, "reg.exe", arguments, timeoutInMinutes, cancellationToken);
        }

        public static async Task<RemoteInstallResult> ImportRegFileForAllUsersRemotelyAsync(
            string remoteComputerName,
            string regFilePath,
            int timeoutInMinutes = 0,
            CancellationToken cancellationToken = default)
        {
            string script = $@"
                $users = Get-WmiObject Win32_UserProfile | Where-Object {{ $_.Special -eq $false }}
                foreach ($user in $users) {{
                    $path = Join-Path $user.LocalPath 'NTUSER.DAT'
                    reg load HKU\TempUser $path
                    reg import '{regFilePath}'
                    reg unload HKU\TempUser
                }}";

            return await ExecuteRemoteScriptAsync(remoteComputerName, script, timeoutInMinutes, cancellationToken);
        }

        private static async Task<bool> CheckAdminRightsAsync(Runspace runspace)
        {
            using (PowerShell powerShell = PowerShell.Create())
            {
                powerShell.Runspace = runspace;
                powerShell.AddScript(@"
                    $user = [System.Security.Principal.WindowsIdentity]::GetCurrent()
                    $principal = New-Object System.Security.Principal.WindowsPrincipal($user)
                    $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)
                ");

                try
                {
                    var results = await Task.Factory.FromAsync(powerShell.BeginInvoke(), powerShell.EndInvoke);
                    return results.Count > 0 && (bool)results[0].BaseObject;
                }
                catch
                {
                    return false;
                }
            }
        }

        public static async Task<bool> IsRemoteComputerOnlineAsync(string computerNameOrIp)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Ping ping = new Ping();
                    PingReply reply = ping.Send(computerNameOrIp);
                    return reply.Status == IPStatus.Success;
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<string> GetComputerNameFromIpAsync(string ipAddress)
        {
            return await Task.Run(() =>
            {
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(ipAddress);
                    return hostEntry.HostName;
                }
                catch
                {
                    return null;
                }
            });
        }

        public static async Task<bool> IsComputerNameValidInActiveDirectoryAsync(string computerName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                    {
                        ComputerPrincipal computer = ComputerPrincipal.FindByIdentity(context, computerName);
                        return computer != null;
                    }
                }
                catch
                {
                    return false;
                }
            });
        }
    }
}
