using Spectre.Console;

namespace SsmExporter.CLI.Models;

public class CliInterface
{
    private const string AppSettingsPattern = "appsettings*.json";
    private const string BaseAppSettingsFile = "appsettings.json";

    public async Task RunAsync()
    {
        try
        {
            DisplayWelcomeMessage();
            
            var configurationPath = GetConfigurationPath();
            var configFiles = GetConfigurationFiles(configurationPath);
            
            DisplayFoundFiles(configFiles);
            
            var applicationName = GetApplicationName(configurationPath);
            var includeProvider = ShouldIncludeProvider();
            var outputPath = GetOutputPath(configurationPath);
            
            await ProcessConfigurationFilesAsync(configFiles, applicationName, includeProvider, outputPath);
            
            DisplaySuccessMessage(outputPath, applicationName);
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            Environment.Exit(1);
        }
    }

    private static void DisplayWelcomeMessage()
    {
        var rule = new Rule("[bold blue]SSM Exporter CLI[/]")
        {
            Justification = Justify.Center
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }

    private static string GetConfigurationPath()
    {
        var path = AnsiConsole.Ask<string>(
            "[yellow]Enter the path to configuration files directory:[/]",
            "."
        );

        if (!Directory.Exists(path))
        {
            AnsiConsole.MarkupLine($"[red]Directory '{path}' does not exist![/]");
            throw new DirectoryNotFoundException($"Directory '{path}' does not exist!");
        }

        return Path.GetFullPath(path);
    }

    private static List<string> GetConfigurationFiles(string configurationPath)
    {
        var files = Directory.GetFiles(configurationPath, AppSettingsPattern)
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .Cast<string>()
            .OrderBy(f => f == BaseAppSettingsFile ? 0 : 1)
            .ThenBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]No configuration files found in directory '{configurationPath}'![/]");
            throw new FileNotFoundException("No configuration files found!");
        }

        if (!files.Contains(BaseAppSettingsFile))
        {
            AnsiConsole.MarkupLine($"[red]Base file '{BaseAppSettingsFile}' not found![/]");
            throw new FileNotFoundException($"Base file '{BaseAppSettingsFile}' not found!");
        }

        return files.Select(f => Path.Combine(configurationPath, f)).ToList();
    }

    private static void DisplayFoundFiles(List<string> configFiles)
    {
        var table = new Table()
            .RoundedBorder()
            .AddColumn("[bold]Found configuration files[/]")
            .AddColumn("[bold]Type[/]");

        foreach (var file in configFiles)
        {
            var fileName = Path.GetFileName(file);
            var fileType = fileName == BaseAppSettingsFile ? "[green]Base[/]" : "[blue]Environment[/]";
            table.AddRow(fileName, fileType);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static string GetApplicationName(string configurationPath)
    {
        var defaultAppName = GetDefaultApplicationName(configurationPath);
        
        return AnsiConsole.Ask<string>(
            "[yellow]Enter application name:[/]",
            defaultAppName
        );
    }

    private static string GetDefaultApplicationName(string configurationPath)
    {
        var directoryName = Path.GetFileName(configurationPath);
        
        if (string.IsNullOrEmpty(directoryName))
        {
            directoryName = Path.GetFileName(Directory.GetCurrentDirectory());
        }
        
        // Extract first part before first dot
        var parts = directoryName.Split('.');
        return parts[0];
    }

    private static bool ShouldIncludeProvider()
    {
        return AnsiConsole.Confirm(
            "[yellow]Include provider section in Terraform file?[/]",
            true
        );
    }

    private static string GetOutputPath(string configurationPath)
    {
        var defaultOutputPath = GetDefaultOutputPath(configurationPath);
        
        var outputPath = AnsiConsole.Ask<string>(
            "[yellow]Enter output path for Terraform file:[/]",
            defaultOutputPath
        );

        return Path.GetFullPath(outputPath);
    }

    private static string GetDefaultOutputPath(string configurationPath)
    {
        var parentDirectory = Directory.GetParent(configurationPath)?.FullName;
        
        if (string.IsNullOrEmpty(parentDirectory))
        {
            return Path.Combine(configurationPath, "terraform");
        }
        
        return Path.Combine(parentDirectory, "terraform");
    }

    private static async Task ProcessConfigurationFilesAsync(
        List<string> configFiles, 
        string applicationName, 
        bool includeProvider, 
        string outputPath)
    {
        await AnsiConsole.Progress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("[green]Processing configuration files...[/]");
                task.MaxValue = configFiles.Count + 1;

                // Load base appsettings.json file
                var baseConfigFile = configFiles.First(f => Path.GetFileName(f) == BaseAppSettingsFile);
                task.Description = $"[green]Loading {Path.GetFileName(baseConfigFile)}...[/]";
                
                var baseDocument = new AppSettingsDocument(baseConfigFile);
                task.Increment(1);
                await Task.Delay(50); // Simulate processing time

                // Load environment files
                var envDocuments = new List<AppSettingsEnvDocument>();
                var envConfigFiles = configFiles.Where(f => Path.GetFileName(f) != BaseAppSettingsFile).ToList();

                foreach (var envConfigFile in envConfigFiles)
                {
                    task.Description = $"[green]Loading {Path.GetFileName(envConfigFile)}...[/]";
                    
                    var envDocument = new AppSettingsEnvDocument(baseDocument, envConfigFile);
                    envDocuments.Add(envDocument);
                    
                    task.Increment(1);
                    await Task.Delay(50); // Simulate processing time
                }

                // Generate Terraform file
                task.Description = "[green]Generating Terraform file...[/]";
                
                var terraformDocument = new TerraformDocument(envDocuments, applicationName, includeProvider);
                terraformDocument.ExportTerraformFile(outputPath);
                
                task.Increment(1);
                await Task.Delay(100); // Simulate write time
            });
    }

    private static void DisplaySuccessMessage(string outputPath, string applicationName)
    {
        var panel = new Panel($"[green]✓[/] Terraform file generated successfully!\n\n" +
                             $"[bold]Location:[/] {Path.Combine(outputPath, $"{applicationName}.tf")}")
            .Header("[bold green]Success![/]")
            .RoundedBorder()
            .BorderColor(Color.Green);

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
        
        AnsiConsole.MarkupLine("[dim]Press any key to exit...[/]");
        Console.ReadKey();
    }
}