using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BclMetrics
{
    static class Program
    {
        static void Main(string[] args)
        {
            string slnPath = @"C:\prj\mono\mono-master\bcl.sln";

            var metrics = new List<ProjectMetrics>();
            foreach (var csproj in GetProjectsFromSolution(slnPath))
            {
                Out($"Parsing {csproj}...");
                var stat = CalculateMetricsForProject(csproj);

                string[] bclPrefixes = { "System", "Windows", "corlib", "Microsoft" };
                // ignore tools, facades, etc. BCL-assemblies' names start with System%, Microsoft% or corlib
                if (bclPrefixes.Any(i => stat.ProjectName.StartsWith(i, StringComparison.InvariantCultureIgnoreCase)))
                    metrics.Add(stat);
            }

            // Top largest assemblies by lines of code:
            int top = 20;
            var topLargestProjs = metrics
                .OrderByDescending(i => i.Total)
                .Take(top)
                .Select((proj, index) => $"{index + 1}. {proj.ProjectName}  ({proj.Total} LOC)");

            Out($"\n\nTop {top} Largest assemblies:\n   " + string.Join("\n   ", topLargestProjs));

            int totalCore = metrics.Sum(i => i.Core);
            int totalRefSource = metrics.Sum(i => i.ReferenceSourcesLines);
            int totalMono = metrics.Sum(i => i.MonoLines);
            int total = metrics.Sum(i => i.Total);

            string Percentages(int value, int sum)
                => $"{value:N0} (" + Math.Round(value * 100f / sum, 1) + "%)";

            Out($"\nTotal Lines Of Code (LOC): {total:N0}\n" +
                              $"   Mono:  {Percentages(totalMono, total)}\n" +
                              $"  .NET ReferenceSources:  {Percentages(totalRefSource, total)}\n" +
                              $"  .NET Core:  {Percentages(totalCore, total)}\n");

            Out(".NET Core Adoption by assemblies:");
            foreach (var item in metrics
                .OrderByDescending(i => i.Core / (float)i.Total)
                .Where(i => i.Core > 0))
            {
                Out($"   {item.ProjectName}   ({Math.Round(item.Core * 100f / item.Total, 1)}%)");
            }
            Console.ReadKey();
        }

        static void Out(string str)
        {
            Console.WriteLine(str);
            File.AppendAllText("log.txt", str + "\n");
        }

        static ProjectMetrics CalculateMetricsForProject(string csproj)
        {
            var result = new ProjectMetrics { ProjectPath = csproj };
            foreach (var fullPath in GetCsFilesFromProject(csproj))
            {
                var linesOfCode = CalculateLinesOfCode(fullPath);
                /*
				 example: corlib.csproj (or corlib.dll.sources)
				   System/EmptyArray.cs  -->  Mono
				   ../referencesource/mscorlib/system/threading/Tasks/TaskToApm.cs  -->  .NET ReferenceSources
				   ../../../external/corert/src/Common/src/Interop/Unix/Interop.Libraries.cs  -->  CoreRT
				   ../../../external/corefx/src/Common/src/CoreLib/System/Memory.cs  -->  CoreFX
				   corefx/SR.cs  --> CoreFX: in local "corefx/" folders we usually store mono-specific glue for corefx, but lets treat SR.cs as .NET Core sources
				   corefx/GlobalizationMode.cs  -->  Mono (glue)
				 */
                if (fullPath.Contains("/external/corefx/".ToOsPath()) ||
                    fullPath.EndsWith("/corefx/SR.cs".ToOsPath())) //
                    result.CoreFxLines += linesOfCode;
                else if (fullPath.Contains("/external/corert/".ToOsPath()) ||
                         fullPath.EndsWith("/corert/SR.cs".ToOsPath()))
                    result.CoreRtLines += linesOfCode;
                else if (fullPath.Contains("/mcs/class/referencesource/".ToOsPath()) ||
                         fullPath.Contains("/external/aspnetwebstack/".ToOsPath()))
                    result.ReferenceSourcesLines += linesOfCode;
                else
                    result.MonoLines += linesOfCode;
            }

            return result;
        }

        static IEnumerable<string> GetCsFilesFromProject(string csproj)
        {
            var csprojFolder = Path.GetDirectoryName(csproj);

            var doc = XDocument.Load(csproj);
            XNamespace msbuild = "http://schemas.microsoft.com/developer/msbuild/2003";
            foreach (var line in doc.Descendants(msbuild + "Compile"))
            {
                string includePath = line.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(includePath))
                    continue;
                includePath = includePath.ToOsPath();

                if (!includePath.Contains("*.cs"))
                    yield return Path.GetFullPath(Path.Combine(csprojFolder, includePath));
                else // expand wildcards
                {
                    var fullPath = Path.GetFullPath(Path.Combine(csprojFolder, includePath.Substring(0, includePath.Length - "/*.cs".Length)));
                    foreach (var expandedCsFile in Directory.GetFiles(fullPath, "*.cs"))
                        yield return expandedCsFile;
                }
            }
        }

        static int CalculateLinesOfCode(string csFile)
        {
            var lines = File.ReadAllLines(csFile);
            return lines.Length; // UPD: well, let's just count all lines

            // ignore blank lines and comments
            var count = lines.Select(i => i.Trim(' ', '\t', '\n', '\r')).Count(i => !string.IsNullOrEmpty(i) && !i.StartsWith("//"));
            // it doesn't take into account #ifdefs and multiline /**/ comments :(
            return count;
        }

        static string ToOsPath(this string path) => path
                .Replace("/", Path.DirectorySeparatorChar.ToString())
                .Replace("\\", Path.DirectorySeparatorChar.ToString());

        static IEnumerable<string> GetProjectsFromSolution(string sln)
        {
            foreach (var line in File.ReadAllLines(sln))
            {
                //parse csprojs from .sln
                //Project("{id1}") = "projectName", "pathTo.csproj", "{id2}"
                //------------------------------------------^
                var startStr = "\", \"";
                var start = line.IndexOf(startStr);
                if (start < 0)
                    continue;
                var end = line.IndexOf("\"", start + startStr.Length + 1);
                var relativePath = line.Substring(start + startStr.Length, end - start - startStr.Length);
                yield return Path.Combine(Path.GetDirectoryName(sln), relativePath).ToOsPath();
            }
        }
    }

    public class ProjectMetrics
    {
        public string ProjectPath { get; set; }
        public int MonoLines { get; set; }
        public int ReferenceSourcesLines { get; set; }
        public int CoreFxLines { get; set; }
        public int CoreRtLines { get; set; }

        public string ProjectName => Path.GetFileNameWithoutExtension(ProjectPath);
        public int Core => CoreFxLines + CoreRtLines;
        public int Total => Core + MonoLines + ReferenceSourcesLines;
    }
}