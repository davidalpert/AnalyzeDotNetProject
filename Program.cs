using System;
using System.IO;
using System.Linq;
using NuGet.ProjectModel;

namespace AnalyzeDotNetProject
{
    class Program
    {
        static void Main(string[] args)
        {
            // Replace to point to your project or solution
            if (args.Length < 1) {
              throw new ArgumentException("must provide a solution file to operate on");
            }
            var solutionFile = new FileInfo(args[0]);
            if (solutionFile.Exists == false) {
              throw new ArgumentException($"cannot find {solutionFile.FullName}");
            }

            string projectPath = solutionFile.FullName;

            var dependencyGraphService = new DependencyGraphService();
            var dependencyGraph = dependencyGraphService.GenerateDependencyGraph(projectPath);

            foreach(var project in dependencyGraph.Projects.Where(p => p.RestoreMetadata.ProjectStyle == ProjectStyle.PackageReference))
            {
                // Generate lock file
                var lockFileService = new LockFileService();
                var lockFile = lockFileService.GetLockFile(project.FilePath, project.RestoreMetadata.OutputPath);

                Console.WriteLine(project.Name);
                
                foreach(var targetFramework in project.TargetFrameworks)
                {
                    Console.WriteLine($"  [{targetFramework.FrameworkName}]");

                    var lockFileTargetFramework = lockFile.Targets.FirstOrDefault(t => t.TargetFramework.Equals(targetFramework.FrameworkName));
                    if (lockFileTargetFramework != null)
                    {
                        foreach(var dependency in targetFramework.Dependencies)
                        {
                            var projectLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == dependency.Name);

                            ReportDependency(projectLibrary, lockFileTargetFramework, 1);
                        }
                    }
                }
            }
        }

        private static void ReportDependency(LockFileTargetLibrary projectLibrary, LockFileTarget lockFileTargetFramework, int indentLevel)
        {
            Console.Write(new String(' ', indentLevel * 2));
            if (projectLibrary == null) {
              Console.WriteLine("projectLibrary is <null>");
              return;
            }
            Console.WriteLine($"{projectLibrary.Name}, v{projectLibrary.Version}");

            foreach (var childDependency in projectLibrary.Dependencies)
            {
                var childLibrary = lockFileTargetFramework.Libraries.FirstOrDefault(library => library.Name == childDependency.Id);

                if (childLibrary != null) {
                  ReportDependency(childLibrary, lockFileTargetFramework, indentLevel + 1);
                } else {
                  Console.Write(new String(' ', (indentLevel+1*2)));
                  Console.WriteLine($"Could not find a childLibrary for {childDependency.Id} in: {String.Join(",", lockFileTargetFramework.Libraries.Select(l => l.Name))}");
                }
            }
        }
    }
}
