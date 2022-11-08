using CommonLib.Source.Common.Utils;
using MSBuildTasks.Source.Tasks;

var projectDir = FileUtils.GetProjectDir("CommonLib.Web", "CrimsonRelays");
var solutionDir = FileUtils.GetSolutionDir("CrimsonRelays");
new CopyToReferencingProjectsMSBuildTask
{
    SolutionDir = solutionDir,
    ProjectDir = projectDir,
    OutDirPart = @"bin\Debug\net6.0",
    SourceFilePatterns = @"Content\**\*.*",
    IncludePublish = true,
    CheckFiles = false,
    IsConsoleTest = true
}.Execute();

System.Console.WriteLine("Done");
System.Console.ReadKey();