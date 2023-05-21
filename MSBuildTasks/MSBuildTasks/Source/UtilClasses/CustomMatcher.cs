using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MSBuildTasks.Source.UtilClasses
{
    public class CustomMatcher
    {
        private readonly List<string> _includePatterns = new List<string>();

        public void AddIncludePatterns(IEnumerable<string> patterns)
        {
            _includePatterns.AddRange(patterns);
        }

        public IEnumerable<string> GetResultsInFullPath(string directoryPath)
        {
            var filePaths = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            var fullPathPatterns = _includePatterns.Select(p => Path.Combine(directoryPath, p).Replace("/", @"\"));
            var matches = filePaths.Where(f => fullPathPatterns.Any(p => IsMatch(p, f))).ToArray();
            return matches;
        }

        private bool IsMatch(string pattern, string filePath)
        {
            //var regexPattern = Regex.Escape(pattern)
            //    .Replace(@"\*\*", "(([^/\\\\]*[/\\\\])*|/?|\\\\?)")
            //    .Replace(@"\*", "[^/\\\\]*")
            //    .Replace(@"\?", "[^/\\\\]")
            //    .Replace(@"\{", "(")
            //    .Replace(@"\}", ")")
            //    .Replace(@",", "|");

            //regexPattern = $"^{regexPattern}$";
            //var regexOptions = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;
            //return Regex.IsMatch(filePath, regexPattern, regexOptions);

            return Regex.Match(filePath, GlobbedPathToRegex(pattern)).Success;
        }

        private static string GlobbedPathToRegex(string pattern, string dirSeparatorChars = @"\\/")
        {
            var builder = new StringBuilder();
            builder.Append('^');
            var remainder = new ReadOnlySpan<char>(pattern.ToCharArray());

            while (remainder.Length > 0)
            {
                var specialCharIndex = remainder.IndexOfAny('*', '?');
                if (specialCharIndex >= 0)
                {
                    var segment = remainder.Slice(0, specialCharIndex);
                    if (segment.Length > 0)
                    {
                        var escapedSegment = Regex.Escape(segment.ToString());
                        builder.Append(escapedSegment);
                    }

                    var currentCharacter = remainder[specialCharIndex];
                    var nextCharacter = specialCharIndex < remainder.Length - 1 ? remainder[specialCharIndex + 1] : '\0';

                    switch (currentCharacter)
                    {
                        case '*':
                            if (nextCharacter == '*')
                            {
                                builder.Append("(.*)");
                                remainder = remainder.Slice(specialCharIndex + 2);
                            }
                            else
                            {
                                builder.Append(dirSeparatorChars.Length > 0 ? $"([^{Regex.Escape(dirSeparatorChars)}]*)" : "(.*)");
                                remainder = remainder.Slice(specialCharIndex + 1);
                            }
                            break;
                        case '?':
                            builder.Append("(.)"); // Regex equivalent of ?
                            remainder = remainder.Slice(specialCharIndex + 1);
                        break;
                    }
                }
                else
                {
                    var escapedSegment = Regex.Escape(remainder.ToString());
                    builder.Append(escapedSegment);
                    remainder = ReadOnlySpan<char>.Empty;
                }
            }

            builder.Append('$');
            return builder.ToString();
        }
    }
}
