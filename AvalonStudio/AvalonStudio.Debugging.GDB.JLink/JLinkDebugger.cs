﻿using AvalonStudio.Packages;
using AvalonStudio.Platforms;
using AvalonStudio.Projects;
using AvalonStudio.Toolchains.GCC;
using AvalonStudio.Utils;
using Mono.Debugging.Client;
using System;
using System.Composition;
using System.IO;
using System.Threading.Tasks;

namespace AvalonStudio.Debugging.GDB.JLink
{
    [Shared]
    [ExportDebugger]
    internal class JLinkDebugger : IDebugger2
    {
        public static string BaseDirectory
        {
            get
            {
                if (Platform.PlatformIdentifier != Platforms.PlatformID.Win32NT)
                {
                    return string.Empty;
                }
                else
                {
                    return Path.Combine(PackageManager.GetPackageDirectory("AvalonStudio.Debuggers.JLink"), "content").ToPlatformPath();
                }
            }
        }

        public string BinDirectory => BaseDirectory;
        
        public DebuggerSession CreateSession(IProject project)
        {
            if (project.ToolChain is GCCToolchain)
            {
                return new JLinkGdbSession(project, (project.ToolChain as GCCToolchain).GDBExecutable);
            }

            throw new Exception("No toolchain");
        }

        public DebuggerSessionOptions GetDebuggerSessionOptions(IProject project)
        {
            var evaluationOptions = EvaluationOptions.DefaultOptions.Clone();

            evaluationOptions.EllipsizeStrings = false;
            evaluationOptions.GroupPrivateMembers = false;
            evaluationOptions.EvaluationTimeout = 1000;

            return new DebuggerSessionOptions() { EvaluationOptions = evaluationOptions };
        }

        public DebuggerStartInfo GetDebuggerStartInfo(IProject project)
        {
            var startInfo = new DebuggerStartInfo()
            {
                Command = Path.Combine(project.CurrentDirectory, project.Executable).ToPlatformPath(),
                Arguments = "",
                WorkingDirectory = Path.GetDirectoryName(Path.Combine(project.CurrentDirectory, project.Executable)),
                UseExternalConsole = false,
                CloseExternalConsoleOnExit = true
            };

            var settings = project.GetDebuggerSettings<JLinkSettings>();

            startInfo.RequiresManualStart = !settings.Run;

            return startInfo;
        }

        public object GetSettingsControl(IProject project)
        {
            return new JLinkSettingsFormViewModel(project);
        }

        public async Task<bool> InstallAsync(IConsole console, IProject project)
        {
            if (Platform.PlatformIdentifier == Platforms.PlatformID.Win32NT)
            {
                switch(await PackageManager.EnsurePackage("AvalonStudio.Debuggers.JLink", console))
                {
                    case PackageEnsureStatus.NotFound:
                    case PackageEnsureStatus.Unknown:
                        return false;
                }
            }

            return true;
        }
    }
}