using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Extensions.FileSystemGlobbing;

namespace MSBuildTasks.Source.Tasks
{
    public class CopyToReferencingProjectsMSBuildTask : Microsoft.Build.Utilities.Task
    {
        [Required] 
        public virtual string SolutionDir { get; set; } = string.Empty;

        [Required]
        public virtual string ProjectDir { get; set; } = string.Empty;
    
        [Required]
        public virtual string OutDirPart { get; set; }

        [Required]
        public virtual string SourceFilePatterns { get; set; } = string.Empty;

        [Required]
        public virtual bool IncludePublish { get; set; }
        
        [Required]
        public virtual bool CheckFiles { get; set; }

        public virtual bool IsConsoleTest { get; set; }

        public override bool Execute()
        {
            if (IsConsoleTest)
            {
                Console.WriteLine("Starting 'CopyToReferencingProjects' MSBuildTask...");
                Console.WriteLine($"IsPublish? {(IncludePublish ? "true" : "false")}");
            }
            else
            {
                Log.LogMessage(MessageImportance.High, "Starting 'CopyToReferencingProjects' MSBuildTask...");
                Log.LogMessage(MessageImportance.High, $"IsPublish? {(IncludePublish ? "true" : "false")}");
            }
            
            var projDirs = Directory.GetFiles(SolutionDir, "*.csproj", SearchOption.AllDirectories).Select(Directory.GetParent).Select(di => di.FullName.TrimEnd('\\')).ToArray();
            var outDirs = projDirs.Select(projDir => projDir + '\\' + OutDirPart.Trim('\\') + '\\' + "_myContent" + '\\' + Directory.CreateDirectory(ProjectDir).Name).Where(p => !p.StartsWith(ProjectDir)).ToArray();
            if (IncludePublish)
                outDirs = outDirs.Concat(projDirs.Select(projDir => projDir + '\\' + @"bin\Publish" + '\\' + "_myContent" + '\\' + Directory.CreateDirectory(ProjectDir).Name).Where(p => !p.StartsWith(ProjectDir))).ToArray();
            //foreach (var outDir in outDirs)
            //{
            //    var dirToRemove = Directory.GetParent(Directory.GetParent(outDir).FullName).FullName.TrimEnd('\\') + @"\Content";
            //    if (Directory.Exists(dirToRemove))
            //    {
            //        var message = $"Deleting '{dirToRemove}'";
            //        if (IsConsoleTest)
            //            Console.WriteLine(message);
            //        else
            //            Log.LogMessage(MessageImportance.High, message);
            //        Directory.Delete(dirToRemove, true);
            //    }
            //}
            
            var sourceFilesMatcher = new Matcher();
            sourceFilesMatcher.AddIncludePatterns(SourceFilePatterns.Split(';'));
            var sourceFiles = sourceFilesMatcher.GetResultsInFullPath(ProjectDir).ToArray();
            foreach (var outDir in outDirs)
            {
                var message = $"Copying '{SourceFilePatterns}' --> '{outDir.Split(new[] { SolutionDir + @"\" }, StringSplitOptions.None).Last()}'";
                if (IsConsoleTest)
                    Console.WriteLine(message);
                else
                    Log.LogMessage(MessageImportance.High, message);

                var countFile = Directory.GetFiles(outDir, $"_myContent-Count-{sourceFiles.Length}", SearchOption.TopDirectoryOnly).SingleOrDefault();
                if (CheckFiles || countFile is null) // sourceFilesMatcher.GetResultsInFullPath(outDir).Count()
                {
                    foreach (var sourceFile in sourceFiles)
                    {
                        var destFile = outDir.TrimEnd('\\') + '\\' + sourceFile.Split(new[] { ProjectDir }, StringSplitOptions.None).Last().TrimStart('\\');
                        var fiSource = new FileInfo(sourceFile);
                        var fiDest = new FileInfo(destFile);
                        if (!File.Exists(destFile) || fiSource.LastWriteTimeUtc < fiDest.LastWriteTimeUtc || fiSource.Length != fiDest.Length)
                        {
                            new FileInfo(destFile).Directory?.Create();
                            File.Copy(sourceFile, destFile, true);
                        }
                    }
                }

                var oldCountFiles = Directory.GetFiles(outDir, "_myContent-Count-*", SearchOption.TopDirectoryOnly);
                foreach (var oldCountFile in oldCountFiles)
                    File.Delete(oldCountFile);
                File.WriteAllBytes(outDir.TrimEnd('\\') + $@"\_myContent-Count-{sourceFiles.Length}", Array.Empty<byte>());
            }

            return true;
        }
    }
}