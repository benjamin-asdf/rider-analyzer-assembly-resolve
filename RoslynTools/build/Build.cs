using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild {
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    const string DOT_NET_SDK_VERSION = "3.1.300";

    [Parameter("Application arguments")]
    readonly string ApplicationArguments;

    [Parameter("Configuration to build - Default is 'Release'")]
    readonly Configuration Configuration = Configuration.Release;
    [Parameter("Project Executable Path")]
    readonly string ProjectExecutable;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath DotNetDirectory => (AbsolutePath)System.IO.Path.GetDirectoryName(DotNetPath);
    AbsolutePath MSBuildDirectory => DotNetDirectory / "sdk" / DOT_NET_SDK_VERSION;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() => {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
        .Executes(() => {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() => {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Publish => _ => _
        .DependsOn(Compile)
        .Executes(() => {
            DotNetPublish(s => s
                .SetProject(Solution)
                .SetOutput(OutputDirectory)
                .SetConfiguration(Configuration)
                .EnableNoBuild());
        });

    Target RunProject(string projectName) => _ => _
        .Executes(() => runProject(projectName));

    Target BuildAndRunProject(string projectName) => _ => _
        .DependsOn(Publish)
        .Executes(() => runProject(projectName));

    private void runProject(string projectName) {
        var appArgs = ApplicationArguments?.Trim('`');
        if (appArgs != null) {
            appArgs += $" ";
        }
        appArgs += $"--msbuild \"{MSBuildDirectory}\"";
        DotNet($"\"{ProjectExecutable?.Trim('`') ?? (OutputDirectory / $"{projectName}.dll")}\" {appArgs}");
    }

    Target RunAnalyzers => RunProject("AnalyzerCLI");
    Target RunGenerators => RunProject("GeneratorCLI");
    Target RunMessagePack => RunProject("MessagePackCLI");
    Target RunEntityIndex => RunProject("EntityIndexCLI");

    Target BuildAndRunAnalyzers => BuildAndRunProject("AnalyzerCLI");
    Target BuildAndRunGenerators => BuildAndRunProject("GeneratorCLI");
    Target BuildAndRunMessagePack => BuildAndRunProject("MessagePackCLI");
    Target BuildAndRunEntityIndex => BuildAndRunProject("EntityIndexCLI");
}
