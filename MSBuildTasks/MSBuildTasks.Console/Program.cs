using CommonLib.Source.Common.Utils;
using MSBuildTasks.Source.Tasks;

//var projectDir = FileUtils.GetProjectDir("CommonLib.Web", "CrimsonRelays");
//var solutionDir = FileUtils.GetSolutionDir("CrimsonRelays");
//new CopyToReferencingProjectsMSBuildTask
//{
//    SolutionDir = solutionDir,
//    ProjectDir = projectDir,
//    OutDirPart = @"bin\Debug\net6.0",
//    SourceFilePatterns = @"Content\**\*.*",
//    IncludePublish = true,
//    CheckFiles = false,
//    IsConsoleTest = true
//}.Execute();

var projectDir = FileUtils.GetProjectDir("CrimsonRelays.Frontend.React", "CrimsonRelays", true, new[] { ".esproj" });
var solutionDir = FileUtils.GetSolutionDir("CrimsonRelays");
new ImportLibraryMSBuildTask
{
    SolutionDir = solutionDir,
    ProjectDir = projectDir,
    LibraryName = "CommonLib.Web.TypeScript",
    From = "src/**",
    To = "src/content",
    SyncMode = "TwoWay",
    IsConsoleTest = true
}.Execute();

new ImportLibraryMSBuildTask
{
    SolutionDir = solutionDir,
    ProjectDir = projectDir,
    LibraryName = "CommonLib.Web",
    From = "wwwroot/**",
    To = "src/content",
    SyncMode = "TwoWay",
    IsConsoleTest = true
}.Execute();

System.Console.WriteLine("Done");
System.Console.ReadKey();