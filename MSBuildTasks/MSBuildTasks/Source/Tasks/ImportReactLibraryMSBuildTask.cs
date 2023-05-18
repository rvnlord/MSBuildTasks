using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;

namespace MSBuildTasks.Source.Tasks
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ImportReactLibraryMSBuildTask : Microsoft.Build.Utilities.Task
    {
        [Required] 
        public virtual string SolutionDir { get; set; } = string.Empty;

        [Required]
        public virtual string ProjectDir { get; set; } = string.Empty;

        [Required] 
        public virtual string ReactLibraryName { get; set; } = string.Empty;

        public virtual bool IsConsoleTest { get; set; }

        public override bool Execute()
        {
            if (IsConsoleTest)
                Console.WriteLine("Starting 'ImportReactLibraryMSBuildTask' MSBuildTask...");
            else
                Log.LogMessage(MessageImportance.High, "Starting 'ImportReactLibraryMSBuildTask' MSBuildTask...");

            var slnPath = Directory.GetFiles(SolutionDir, "*.sln", SearchOption.TopDirectoryOnly).Single();
            var sln = File.ReadAllText(slnPath);
            var regex = new Regex($@"Project\(""{{.*?}}""\) = ""{Regex.Escape(ReactLibraryName)}"", ""(.*?)""");
            var relativeLibraryPath = regex.Match(sln).Groups[1].Value;
            var libraryDir = Directory.GetParent(Path.GetFullPath(Path.Combine(SolutionDir, relativeLibraryPath)))?.GetDirectories("src").Single() ?? throw new NullReferenceException();
            var targetDir = new DirectoryInfo($@"{new DirectoryInfo(ProjectDir).GetDirectories("src").Single().FullName}\content\{ReactLibraryName}");

            if (!targetDir.Exists)
                targetDir.Create();

            var dirsToCopy = new Stack<DirectoryInfo>();
            dirsToCopy.Push(libraryDir);

            while (dirsToCopy.Count > 0)
            {
                var currentDir = dirsToCopy.Pop();

                foreach (var sourceFile in currentDir.GetFiles())
                {
                    var relativeSourceFilePath = sourceFile.FullName.Split(new[] { $@"{libraryDir.Parent?.FullName}\" }, StringSplitOptions.None).Last();
                    var targetFilePath = $@"{targetDir.FullName}\{relativeSourceFilePath.Split(new[] { @"src\" }, StringSplitOptions.None).Last()}";
                    if (!File.Exists(targetFilePath) || File.GetLastWriteTimeUtc(sourceFile.FullName) > File.GetLastWriteTimeUtc(targetFilePath))
                    {
                        var relativeTargetFilePath = targetFilePath.Split(new[] { $@"{targetDir.Parent?.Parent}\" }, StringSplitOptions.None).Last();
                        var message = $"Copying '{relativeSourceFilePath}' --> '{relativeTargetFilePath}'";
                        if (IsConsoleTest)
                            Console.WriteLine(message);
                        else
                            Log.LogMessage(MessageImportance.High, message);
                        sourceFile.CopyTo(targetFilePath, true);
                    }
                }

                foreach (var sourceDir in currentDir.GetDirectories())
                {
                    var relativeSourceDirPath = sourceDir.FullName.Split(new[] { $@"{libraryDir.FullName}\" }, StringSplitOptions.None).Last();
                    var targetDirPath = $@"{targetDir.FullName}\{relativeSourceDirPath}";
                    if (!Directory.Exists(targetDirPath))
                        Directory.CreateDirectory(targetDirPath);
                    dirsToCopy.Push(sourceDir);
                }
            }
            
            var allRecursiveDirsInTargetDir = targetDir.GetDirectories("*", SearchOption.AllDirectories);
            foreach (var reecursiveTargetDir in allRecursiveDirsInTargetDir)
            {
                var relativeTargetDirPath = reecursiveTargetDir.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new DirectoryInfo($@"{libraryDir.FullName}\{relativeTargetDirPath}").Exists)
                {
                    var relativeToContentTargetDirPath = $@"{targetDir.FullName.Split(new[] { $@"{targetDir.Parent?.Parent?.FullName}\" }, StringSplitOptions.None).Last()}\{relativeTargetDirPath}";
                    var message = $"Removing '{relativeToContentTargetDirPath}' directory because it doesn't exist in source";
                    if (IsConsoleTest)
                        Console.WriteLine(message);
                    else
                    {
                        Log.LogMessage(MessageImportance.High, message);
                        Log.LogMessage(MessageImportance.High, $"Library Dir: {libraryDir.FullName}");
                        Log.LogMessage(MessageImportance.High, $@"Exact Source Path Checked: {libraryDir}\{relativeTargetDirPath}, Exists: {new DirectoryInfo($@"{libraryDir}\{relativeTargetDirPath}").Exists}");
                    }
                    reecursiveTargetDir.Delete(true);
                }
            }

            var allRecursiveFilesInTargetDir = targetDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var reecursiveTargetFile in allRecursiveFilesInTargetDir)
            {
                var relativeTargetFilePath = reecursiveTargetFile.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new FileInfo($@"{libraryDir.FullName}\{relativeTargetFilePath}").Exists)
                {
                    var relativeToContentTargetFilePath = $@"{targetDir.FullName.Split(new[] { $@"{targetDir.Parent?.Parent?.FullName}\" }, StringSplitOptions.None).Last()}\{relativeTargetFilePath}";
                    var message = $"Removing '{relativeToContentTargetFilePath}' file because it doesn't exist in source";
                    if (IsConsoleTest)
                        Console.WriteLine(message);
                    else
                        Log.LogMessage(MessageImportance.High, message);
                    reecursiveTargetFile.Delete();
                }
            }

            if (IsConsoleTest)
                Console.WriteLine("Finished 'ImportReactLibraryMSBuildTask' MSBuildTask.");
            else
                Log.LogMessage(MessageImportance.High, "Finished 'ImportReactLibraryMSBuildTask' MSBuildTask.");

            return true;
        }
    }
}
