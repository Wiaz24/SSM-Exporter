using System.Text;

namespace SsmExporter.CLI.Models;

public class TerraformDocument
{
    private readonly bool _addProvider;
    private readonly string _applicationName;
    private readonly List<AppSettingsEnvDocument> _appSettingsEnvDocuments;
    public IEnumerable<AppSettingsEnvDocument> AppSettingsEnvDocuments => _appSettingsEnvDocuments;
    private readonly StringBuilder _terraformStringBuilder = new();

    public TerraformDocument(IEnumerable<AppSettingsEnvDocument> appSettingsEnvDocuments, string applicationName,
        bool addProvider = false)
    {
        _addProvider = addProvider;
        _applicationName = applicationName;
        _appSettingsEnvDocuments = new List<AppSettingsEnvDocument>(appSettingsEnvDocuments);
    }

    public void ExportTerraformFile(string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory cannot be null or empty.", nameof(outputDirectory));
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var filePath = Path.Combine(outputDirectory, $"{_applicationName}.tf");
        var content = GetTerraformFileContent();
        File.WriteAllText(filePath, content);
    }

    public string GetTerraformFileContent()
    {
        _terraformStringBuilder.Clear();
        if (_addProvider)
        {
            AddProviderSection();
            _terraformStringBuilder.AppendLine("");
        }

        AddLocalsSection();
        _terraformStringBuilder.AppendLine("");
        foreach (var document in _appSettingsEnvDocuments)
        {
            AddAwsSsmParameterSectionForDocument(document);
            _terraformStringBuilder.AppendLine("");
        }

        return _terraformStringBuilder.ToString();
    }

    private void AddLocalsSection()
    {
        _terraformStringBuilder.AppendLine("locals {");
        foreach (var document in AppSettingsEnvDocuments)
        {
            AddLocalsSectionForDocument(document);
        }

        _terraformStringBuilder.AppendLine("}");
    }

    private void AddLocalsSectionForDocument(AppSettingsEnvDocument document)
    {
        _terraformStringBuilder.AppendLine($"    {document.Environment.ToLower()}_parameters = {{");
        foreach (var parameter in document.Parameters)
        {
            _terraformStringBuilder.AppendLine($"        \"{parameter}\" = {{ type = \"String\", value = \"\"}}");
        }

        foreach (var secret in document.Secrets)
        {
            _terraformStringBuilder.AppendLine($"        \"{secret}\" = {{ type = \"SecureString\", value = \"\"}}");
        }

        _terraformStringBuilder.AppendLine("    }");
    }

    private void AddAwsSsmParameterSectionForDocument(AppSettingsEnvDocument document)
    {
        _terraformStringBuilder.AppendLine(
            $"resource \"aws_ssm_parameter\" \"{document.Environment.ToLower()}_params\" {{");
        _terraformStringBuilder.AppendLine($"    for_each    = local.{document.Environment.ToLower()}_parameters");
        _terraformStringBuilder.AppendLine(
            $"    name        = \"/{_applicationName}/{document.Environment}${{each.key}}\"");
        _terraformStringBuilder.AppendLine(
            $"    description = \"{document.Environment} parameter for {_applicationName}\"");
        _terraformStringBuilder.AppendLine("    type        = each.value.type");
        _terraformStringBuilder.AppendLine("    value       = each.value.value");
        _terraformStringBuilder.AppendLine("    tier        = \"Standard\"");
        _terraformStringBuilder.AppendLine("");
        _terraformStringBuilder.AppendLine("    tags = {");
        _terraformStringBuilder.AppendLine($"        Application = \"{_applicationName}\"");
        _terraformStringBuilder.AppendLine($"        Environment = \"{document.Environment}\"");
        _terraformStringBuilder.AppendLine("        ManagedBy   = \"Terraform\"");
        _terraformStringBuilder.AppendLine("    }");
        _terraformStringBuilder.AppendLine("}");
    }

    private void AddProviderSection(string region = "eu-central-1", string profile = "default")
    {
        _terraformStringBuilder.AppendLine("provider \"aws\" {");
        _terraformStringBuilder.AppendLine($"    region = \"{region}\"");
        _terraformStringBuilder.AppendLine($"    profile = \"{profile}\"");
        _terraformStringBuilder.AppendLine("}");
    }
}