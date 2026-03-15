using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Xml.Linq;
using NAudio.CoreAudioApi;
using Windows.Management.Deployment;

namespace Flow.Launcher.Plugin.AppAudioManager
{
    public class AudioSessionFactory
    {
        private const string systemSoundsIdentifier = "@%SystemRoot%\\System32\\AudioSrv.Dll";
        private readonly string windowsAppsPath;
        private readonly string currentUserSid;
        private PackageManager packageManager;

        public AudioSessionFactory()
        {
            packageManager = new PackageManager();

            windowsAppsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "WindowsApps"
            );

            currentUserSid = WindowsIdentity.GetCurrent().User.Value;
        }

        public AudioSession create(
            AudioSessionControl session
        )
        {
            if (
                !string.IsNullOrEmpty(session.DisplayName)
                && session.DisplayName.StartsWith(systemSoundsIdentifier)
            )
            {
                return HandleSystemSounds(session);
            }

            using var rootProcess = GetRootProcess(session: session);

            string? processFilePath = GetProcessFilePath(process: rootProcess);

            string fallbackName = GetFallbackName(session: session, process: rootProcess);            

            string fallbackIconPath = GetFallbackIconPath(session: session, processFilePath: processFilePath);

            // if this is an UWP app
            if (processFilePath is not null && processFilePath.StartsWith(windowsAppsPath))
            {
                return HandleUWPApp(session: session, processFilePath: processFilePath, fallbackName: fallbackName, fallbackIconPath: fallbackIconPath);
            }

            return new AudioSession(
                session: session,
                name: fallbackName,
                iconPath: fallbackIconPath,
                processId: (int)session.GetProcessID,
                processFilePath: processFilePath
            );
        }

        private static string GetFallbackIconPath(AudioSessionControl session, string? processFilePath)
        {
            if (!string.IsNullOrEmpty(session.IconPath))
            {
                return session.IconPath;
            }

            if (processFilePath != null) return processFilePath;

            return string.Empty;
        }

        private string GetFallbackName(AudioSessionControl session, Process process)
        {
            if (!string.IsNullOrEmpty(session.DisplayName))
            {
                return session.DisplayName;
            }

            if (process != null)
            {
                try
                {
                    if (!string.IsNullOrEmpty(process.MainWindowTitle))
                    {
                        return process.MainWindowTitle;
                    }
                }
                catch (Exception ex) when (
                        ex is InvalidOperationException or NotSupportedException)
                { /* Ignore */ }

                try
                {
                    if (!string.IsNullOrEmpty(process.ProcessName))
                    {
                        return process.ProcessName;
                    }
                }
                catch (Exception ex) when (
                        ex is InvalidOperationException or NotSupportedException)
                {  /* Ignore */ }
            }

            return "Unknown";
        }

        private Process GetRootProcess(AudioSessionControl session)
        {
            var processId = (int)session.GetProcessID;

            // Get process associated with the audio session
            Process sessionProcess = null;
            try
            {
                sessionProcess = Process.GetProcessById(processId);
            }
            catch (Exception ex) when (
                ex is ArgumentException or InvalidOperationException)
            { /* Ignore */ }
            ;

            // If the process is a WebView2 process, go up the parent chain to find the actual host process
            var parentProcess = sessionProcess;
            while (parentProcess is not null && parentProcess.ProcessName == "msedgewebview2")
            {
                var parentProcessId = ProcessHelper.GetParentProcessId(parentProcess.Id);
                if (parentProcessId == -1)
                {
                    break;
                }

                if (parentProcess != sessionProcess) parentProcess.Dispose();

                try
                {
                    parentProcess = Process.GetProcessById(parentProcessId);
                }
                catch (Exception ex) when (
                    ex is ArgumentException or InvalidOperationException)
                {
                    parentProcess = null;
                }
            }


            // if we found the parent process use it
            if (parentProcess is not null)
            {
                if (sessionProcess is not null) sessionProcess.Dispose();
                return parentProcess;
            }

            // otherwise just use the sessionProcess
            return sessionProcess;
        }

        private static string? GetProcessFilePath(Process process)
        {
            if (process != null)
            {
                try
                {
                    return process.MainModule.FileName;
                }
                catch (Exception ex) when (
                    ex is InvalidOperationException or NotSupportedException or System.ComponentModel.Win32Exception)
                {  /* Ignore */ }
            }
            return null;
        }

        private AudioSession HandleSystemSounds(AudioSessionControl session)
        {
            // expand identifier and remove @ to get path
            var processFilePath = Environment.ExpandEnvironmentVariables(systemSoundsIdentifier).Substring(1);

            return new AudioSession(
                session: session,
                name: "System Sounds",
                iconPath: "Assets/SystemSoundsIcon.png",
                processId: (int)session.GetProcessID,
                processFilePath: processFilePath
            );
        }

        private AudioSession HandleUWPApp(
            AudioSessionControl session,
            string? processFilePath,
            string fallbackName,
            string fallbackIconPath
        )
        {
            string? uwpName = null;
            string? uwpIconPath = null;


            var startIndex = processFilePath.IndexOf("WindowsApps\\") + "WindowsApps\\".Length;
            var appFolderPath = processFilePath.Substring(0, processFilePath.IndexOf("\\", startIndex) + 1);

            // try to get name from package manager
            string packageFullName = new DirectoryInfo(appFolderPath).Name;
            var package = packageManager.FindPackageForUser(currentUserSid, packageFullName);
            if (package != null)
            {
                var appEntry = package.GetAppListEntries().FirstOrDefault();

                if (appEntry != null)
                {
                    uwpName = appEntry.DisplayInfo.DisplayName;
                }
            }

            // extract info from manifest
            string manifestPath = Path.Combine(appFolderPath, "AppxManifest.xml");
            try
            {
                var xmlParser = new XMLParser(filePath: manifestPath);

                // Try to get name if we don't have it yet
                if (
                    uwpName is null &&
                    xmlParser.TryGetValueByPath(
                        out string propDisplayName,
                        "Properties",
                        "DisplayName"
                    )
                )
                {
                    uwpName = propDisplayName;
                }

                // Try to get icon path
                if (
                    xmlParser.TryGetElementByPath(
                        out XElement visualElements,
                        "Applications",
                        "Application",
                        "uap:VisualElements"
                    )
                    &&
                    xmlParser.TryGetAttributeValue(
                        out string square44LogoRelPath,
                        element: visualElements,
                        attributeName: "Square44x44Logo"
                    )
                )
                {
                    string logoManifestPath = Path.Combine(
                        appFolderPath,
                        square44LogoRelPath
                    );

                    var variants = UWPResourceResolver.FindAllVariants(logoManifestPath);

                    uwpIconPath = variants.ElementAtOrDefault(0);
                }
                else if (xmlParser.TryGetValueByPath(
                    out string propLogoRelPath,
                    "Properties",
                    "Logo"
                ))
                {
                    string logoManifestPath = Path.Combine(
                        appFolderPath,
                        propLogoRelPath
                    );

                    var variants = UWPResourceResolver.FindAllVariants(logoManifestPath);

                    uwpIconPath = variants.ElementAtOrDefault(0);
                }

            }
            catch (Exception)
            {
                // if we failed to extract info from the manifest just ignore as we will use the fallbacks
            }

            return new AudioSession(
                session: session,
                name: string.IsNullOrEmpty(uwpName) ? fallbackName : uwpName,
                iconPath: string.IsNullOrEmpty(uwpIconPath) ? fallbackIconPath : uwpIconPath,
                processId: (int)session.GetProcessID,
                processFilePath: processFilePath
            );

        }
    }
}