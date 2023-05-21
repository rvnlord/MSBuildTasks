using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using MSBuildTasks.Source.UtilClasses;

namespace MSBuildTasks.Source.Tasks
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class ImportLibraryMSBuildTask : Microsoft.Build.Utilities.Task
    {
        [Required] 
        public virtual string SolutionDir { get; set; } = string.Empty;

        [Required]
        public virtual string ProjectDir { get; set; } = string.Empty;

        [Required] 
        public virtual string LibraryName { get; set; } = string.Empty;

        [Required] 
        public virtual string From { get; set; } = string.Empty;
        
        public virtual string To { get; set; } = string.Empty;

        public virtual bool IsConsoleTest { get; set; }

        public override bool Execute()
        {
            if (IsConsoleTest)
                Console.WriteLine("Starting 'ImportLibraryMSBuildTask' MSBuildTask...");
            else
                Log.LogMessage(MessageImportance.High, "Starting 'ImportLibraryMSBuildTask' MSBuildTask...");

            var slnPath = Directory.GetFiles(SolutionDir, "*.sln", SearchOption.TopDirectoryOnly).Single();
            var sln = File.ReadAllText(slnPath);
            var regex = new Regex($@"Project\(""{{.*?}}""\) = ""{Regex.Escape(LibraryName)}"", ""(.*?)""");
            var relativeLibraryProjectFilePath = regex.Match(sln).Groups[1].Value;
            var libraryDir = new DirectoryInfo(Path.GetFullPath(Path.Combine(SolutionDir, relativeLibraryProjectFilePath))).Parent ?? throw new NullReferenceException("Invalid Library Dir Path");
            var targetDir = new DirectoryInfo(Path.GetFullPath(Path.Combine(ProjectDir, To, LibraryName)));
            var sourcePatternCommonPrefix = GetCommonPrefix(From.Split(';'));

            if (!targetDir.Exists)
                targetDir.Create();

            var sourceFilesMatcher = new CustomMatcher();
            sourceFilesMatcher.AddIncludePatterns(From.Split(';'));
            var sourceFilePaths = sourceFilesMatcher.GetResultsInFullPath(libraryDir.FullName).ToArray();

            foreach (var sourceFilePath in sourceFilePaths)
            {
                var relativeSourceFilePath = sourceFilePath.Split(new[] { $@"{libraryDir.FullName}\{sourcePatternCommonPrefix}\" }, StringSplitOptions.None).Last();
                var relativeTargetFilePath = Path.Combine(LibraryName, relativeSourceFilePath);
                var targetFilePath = Path.GetFullPath(Path.Combine(ProjectDir, To, relativeTargetFilePath));
                if (!File.Exists(targetFilePath) || File.GetLastWriteTimeUtc(sourceFilePath) > File.GetLastWriteTimeUtc(targetFilePath))
                {
                    var message = $"Copying '{relativeSourceFilePath}' --> '{relativeTargetFilePath}'";
                    if (IsConsoleTest)
                        Console.WriteLine(message);
                    else
                        Log.LogMessage(MessageImportance.High, message);

                    var targetDirPath = Path.GetDirectoryName(targetFilePath) ?? throw new NullReferenceException("Invalid directory path");
                    if (!Directory.Exists(targetDirPath))
                        Directory.CreateDirectory(targetDirPath);

                    new FileInfo(sourceFilePath).CopyTo(targetFilePath, true);
                }
            }
            
            var allRecursiveFilesInTargetDir = targetDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var reecursiveTargetFile in allRecursiveFilesInTargetDir)
            {
                var relativeTargetFilePath = reecursiveTargetFile.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new FileInfo($@"{libraryDir.FullName}\{sourcePatternCommonPrefix}\{relativeTargetFilePath}").Exists)
                {
                    var message = $"Removing '{relativeTargetFilePath}' file because it doesn't exist in source";
                    if (IsConsoleTest)
                        Console.WriteLine(message);
                    else
                        Log.LogMessage(MessageImportance.High, message);
                    reecursiveTargetFile.Delete();
                }
            }

            var allRecursiveDirsInTargetDir = targetDir.GetDirectories("*", SearchOption.AllDirectories);
            foreach (var recursiveTargetDir in allRecursiveDirsInTargetDir)
            {
                var relativeTargetDirPath = recursiveTargetDir.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new DirectoryInfo($@"{libraryDir.FullName}\{sourcePatternCommonPrefix}\{relativeTargetDirPath}").Exists)
                {
                    var message = $"Removing '{relativeTargetDirPath}' directory because it doesn't exist in source";
                    if (IsConsoleTest)
                        Console.WriteLine(message);
                    else
                    {
                        Log.LogMessage(MessageImportance.High, message);
                        Log.LogMessage(MessageImportance.High, $"Library Dir: {libraryDir.FullName}");
                        Log.LogMessage(MessageImportance.High, $@"Exact Source Path Checked: {libraryDir}\{relativeTargetDirPath}, Exists: {new DirectoryInfo($@"{libraryDir}\{relativeTargetDirPath}").Exists}");
                    }
                    if (Directory.Exists(recursiveTargetDir.FullName))
                        recursiveTargetDir.Delete(true);
                }
            }

            if (IsConsoleTest)
                Console.WriteLine("Finished 'ImportLibraryMSBuildTask' MSBuildTask.");
            else
                Log.LogMessage(MessageImportance.High, "Finished 'ImportLibraryMSBuildTask' MSBuildTask.");

            return true;
        }

        private static string GetCommonPrefix(string[] paths)
        {
            if (paths.Length == 0) 
                return "";
            var prefix = paths[0];
            foreach (var path in paths)
            {
                int j;
                for (j = 0; j < Math.Min(prefix.Length, path.Length); j++)
                    if (prefix[j] != path[j] || prefix[j] == '*')
                        break;
                prefix = prefix.Substring(0, j);
            }
            while (prefix.Last() != '/' && prefix.Last() != '\\')
                prefix = prefix.Substring(0, prefix.Length - 1);
            if (prefix.EndsWith("/") || prefix.EndsWith("\\"))
                prefix = prefix.Substring(0, prefix.Length - 1);
            return prefix;
        }
    }
}
