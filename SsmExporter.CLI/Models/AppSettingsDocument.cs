using SsmExporter.CLI.Models.Abstractions;

namespace SsmExporter.CLI.Models;

public class AppSettingsDocument : AppSettingsDocumentBase
{
    public AppSettingsDocument(string fileName)
    {
        var document = LoadDocument(fileName);
        LoadParametersAndSecrets(document);
    }
}