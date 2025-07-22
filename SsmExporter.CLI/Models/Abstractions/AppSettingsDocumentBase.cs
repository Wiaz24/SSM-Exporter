using System.Text.Json;

namespace SsmExporter.CLI.Models.Abstractions;

public abstract class AppSettingsDocumentBase
{
    protected HashSet<string> _parameters = [];
    public IEnumerable<string> Parameters => _parameters;

    protected HashSet<string> _secrets = [];
    public IEnumerable<string> Secrets => _secrets;

    protected static JsonDocument LoadDocument(string fileName)
    {
        if (!File.Exists(fileName))
        {
            throw new FileNotFoundException($"Configuration file not found: {fileName}");
        }
    
        try
        {
            var jsonContent = File.ReadAllText(fileName);
            return JsonDocument.Parse(jsonContent);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Invalid JSON format in file: {fileName}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error reading configuration file: {fileName}", ex);
        }
    }
    protected void LoadParametersAndSecrets(JsonDocument jsonDocument)
    {
        ProcessJsonElement(jsonDocument.RootElement, "");
    }
    private void ProcessJsonElement(JsonElement element, string currentPath)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var newPath = string.IsNullOrEmpty(currentPath) 
                        ? $"/{property.Name}" 
                        : $"{currentPath}/{property.Name}";
                    ProcessJsonElement(property.Value, newPath);
                }
                break;
            
            case JsonValueKind.Array:
                for (var i = 0; i < element.GetArrayLength(); i++)
                {
                    var newPath = $"{currentPath}[{i}]";
                    ProcessJsonElement(element[i], newPath);
                }
                break;
            
            case JsonValueKind.String:
                var value = element.GetString() ?? "";
                switch (value)
                {
                    case "ssm-parameter":
                        _parameters.Add(currentPath);
                        break;
                    case "ssm-secret":
                        _secrets.Add(currentPath);
                        break;
                }
                break;
        }
    }
}