using System;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.Execution;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Version to build - Default is '1.0.0' for local builds")]
    readonly string Version = "1.0.0";

    [Solution] readonly Solution Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => SourceDirectory / "AlloySink.Tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Project AlloySinkProject => Solution.GetProject("AlloySink");
    Project TestProject => Solution.GetProject("AlloySink.Tests");

    string _cachedBuildVersion;
    string BuildVersion => _cachedBuildVersion ??= GetBuildVersion();

    Target InstallAbcVersion => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            try
            {
                ProcessTasks.StartProcess("dotnet", "tool install --global Deneblab.AbcVersionCmd")
                    .AssertWaitForExit();
                Console.WriteLine("AbcVersion tool installed successfully");
            }
            catch
            {
                Console.WriteLine("AbcVersion tool installation failed or already installed");
            }
        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(BuildVersion)
                .SetFileVersion(BuildVersion)
                .SetInformationalVersion(BuildVersion)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(TestProject)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetLoggers("trx")
                .SetResultsDirectory(OutputDirectory / "test-results"));
        });

    Target Pack => _ => _
        .DependsOn(Compile)
        .Produces(ArtifactsDirectory / "*.nupkg")
        .Executes(() =>
        {
            DotNetPack(s => s
                .SetProject(AlloySinkProject)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetVersion(BuildVersion)
                .SetPackageReleaseNotes($"Version {BuildVersion}")
                .EnableNoBuild()
                .EnableNoRestore());
        });

    Target CI => _ => _
        .DependsOn(InstallAbcVersion, Clean, Test, Pack)
        .Executes(() =>
        {
            Console.WriteLine($"Build Version: {BuildVersion}");
            Console.WriteLine($"Configuration: {Configuration}");
        });

    string GetBuildVersion()
    {
        // Check for environment variable first (for CI/CD)
        var envVersion = Environment.GetEnvironmentVariable("PACKAGE_VERSION");
        if (!string.IsNullOrEmpty(envVersion))
        {
            return envVersion;
        }

        // Try to use AbcVersion for semantic versioning
        try
        {
            var output = ProcessTasks.StartProcess("abcversion", "-p semversion")
                .AssertWaitForExit()
                .Output.Select(x => x.Text).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(output))
            {
                return output.Trim();
            }
        }
        catch
        {
            // AbcVersion not available, fallback to git-based versioning
        }

        // Fallback to base version
        return Version ?? "1.0.0";
    }

}
