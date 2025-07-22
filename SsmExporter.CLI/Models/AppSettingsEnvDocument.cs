using SsmExporter.CLI.Models.Abstractions;

namespace SsmExporter.CLI.Models;

public class AppSettingsEnvDocument :  AppSettingsDocumentBase
{
    public string Environment { get; init; }

    public AppSettingsEnvDocument(AppSettingsDocument baseDocument, string fileName)
    {
        
    }

    public static string GetEnvironmentFromFileName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName).Split('.').Last();
    }
}