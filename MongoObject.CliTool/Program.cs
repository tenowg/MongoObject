using System.Diagnostics;
using CliTool;
using Spectre.Console.Cli;

var app = new CommandApp<GreetCommand>();
return app.Run(args);

internal class GreetCommand : Command<Settings>
{
    protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        switch(settings.Action)
        {
            case "migrate":
                Console.WriteLine($"Hello, {settings.Action}!");
                if (settings.Build)
                {
                    Console.WriteLine("we will build the migrations now if there are any");
                    GatherResources(settings);
                }
                else
                {
                    Console.WriteLine("time to migrate the next migrations");
                }
                break;
            default:
                Console.WriteLine($"The Options {settings.Action} is not a supported action");
                break;
        }
        return 0;
    }

    private void GatherResources(Settings settings)
    {
        string projectFullPath = Path.GetFullPath(settings.Project);

        var build = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build {settings.Project} -c Debug /p:GeneratePackageOnBuild=false /p:IsPackable=false"
            }
        };

        build.Start();
        build.WaitForExit();
        var code = build.ExitCode;

        if (code != 0)
        {
            return;
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project {settings.Project} --no-build -- --mongoobject-dump-schema",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = projectFullPath
            }
        };

        process.Start();
        while (!process.StandardOutput.EndOfStream)
        {
            string? line = process.StandardOutput.ReadLine();
            if (line != null /*&& line.StartsWith("cli-data:")*/)
            {
                Console.WriteLine(line);
            }
        }
        process.WaitForExit();
    }
}