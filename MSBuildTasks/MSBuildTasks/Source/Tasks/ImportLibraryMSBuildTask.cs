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
        private SyncModeType _syncMode;

        [Required] 
        public virtual string SolutionDir { get; set; } = string.Empty;

        [Required]
        public virtual string ProjectDir { get; set; } = string.Empty;

        [Required] 
        public virtual string LibraryName { get; set; } = string.Empty;

        [Required] 
        public virtual string From { get; set; } = string.Empty;
        
        [Required]
        public virtual string To { get; set; } = string.Empty;

        [Required]
        public virtual string SyncMode
        {
            get => Enum.GetName(_syncMode.GetType(), _syncMode);
            set => _syncMode = Enum.GetValues(typeof(SyncModeType)).Cast<SyncModeType>().Single(v => Enum.GetName(v.GetType(), v)?.Equals(value, StringComparison.InvariantCultureIgnoreCase) == true);
        }

        public virtual bool IsConsoleTest { get; set; }

        public override bool Execute()
        {
            Print("Starting 'ImportLibraryMSBuildTask' MSBuildTask...");

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
                    Print($"Copying '{relativeSourceFilePath}' --> '{relativeTargetFilePath}'");
                    var targetDirPath = Path.GetDirectoryName(targetFilePath) ?? throw new NullReferenceException("Invalid directory path");
                    if (!Directory.Exists(targetDirPath))
                        Directory.CreateDirectory(targetDirPath);

                    new FileInfo(sourceFilePath).CopyTo(targetFilePath, true);
                } 
                else if (_syncMode == SyncModeType.TwoWay && File.Exists(targetFilePath) && File.GetLastWriteTimeUtc(sourceFilePath) < File.GetLastWriteTimeUtc(targetFilePath))
                {
                    Print($"Copying '{relativeSourceFilePath}' <-- '{relativeTargetFilePath}'");
                    new FileInfo(targetFilePath).CopyTo(sourceFilePath, true);
                }
            }
            
            var allRecursiveFilesInTargetDir = targetDir.GetFiles("*", SearchOption.AllDirectories);
            foreach (var recursiveTargetFile in allRecursiveFilesInTargetDir)
            {
                var relativeTargetFilePath = recursiveTargetFile.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new FileInfo($@"{libraryDir.FullName}\{sourcePatternCommonPrefix}\{relativeTargetFilePath}").Exists)
                {
                    Print($"Removing '{relativeTargetFilePath}' file because it doesn't exist in source");
                    recursiveTargetFile.Delete();
                }
            }

            var allRecursiveDirsInTargetDir = targetDir.GetDirectories("*", SearchOption.AllDirectories);
            foreach (var recursiveTargetDir in allRecursiveDirsInTargetDir)
            {
                var relativeTargetDirPath = recursiveTargetDir.FullName.Split(new[] { $@"{targetDir.FullName}\" }, StringSplitOptions.None).Last();
                if (!new DirectoryInfo($@"{libraryDir.FullName}\{sourcePatternCommonPrefix}\{relativeTargetDirPath}").Exists)
                {
                    Print($"Removing '{relativeTargetDirPath}' directory because it doesn't exist in source");
                    if (Directory.Exists(recursiveTargetDir.FullName))
                        recursiveTargetDir.Delete(true);
                }
            }

            Print("Finished 'ImportLibraryMSBuildTask' MSBuildTask.");

            return true;
        }

        private void Print(string message)
        {
            if (IsConsoleTest)
                Console.WriteLine(message);
            else
                Log.LogMessage(MessageImportance.High, message);
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

        private enum SyncModeType
        {
            OneWay,
            TwoWay
        }
    }
}
