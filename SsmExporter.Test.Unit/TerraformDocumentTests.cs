using Shouldly;
using SsmExporter.CLI.Models;

namespace SsmExporter.Test.Unit;

public class TerraformDocumentTests
{
    private const string AppsettingsJsonContent = """
                                                  {
                                                      "Services": {
                                                          "ApiKey": "ssm-parameter",
                                                          "ClientSecret": "ssm-secret"
                                                      },
                                                      "RegularConfig": {
                                                          "Timeout": 30,
                                                          "RetryCount": 3
                                                      }
                                                  }
                                                  """;

    private const string AppsettingsDevelopmentJson = """
                                                      {
                                                          "Database": {
                                                              "ConnectionString": "ssm-secret"
                                                          },
                                                          "SomeOtherConfig": {
                                                              "Key1": "value1"
                                                          }
                                                      }
                                                      """;

    private const string AppsettingsProductionJson = """
                                                     {
                                                         "Database": {
                                                             "ConnectionString": "ssm-secret"
                                                         },
                                                         "SomeOtherConfig": {
                                                             "Key1": "ssm-parameter"
                                                         }
                                                     }
                                                     """;

    private AppSettingsEnvDocument _getAppSettingsDevelopmentDocument()
    {
        var appsettingsFilePath = Path.Combine(Path.GetTempPath(), "tmp", "appsettings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(appsettingsFilePath) ?? string.Empty);
        File.WriteAllText(appsettingsFilePath, AppsettingsJsonContent);

        var appsettingsDevelopmentFilePath = Path.Combine(Path.GetTempPath(), "tmp", "appsettings.Development.json");
        Directory.CreateDirectory(Path.GetDirectoryName(appsettingsDevelopmentFilePath) ?? string.Empty);
        File.WriteAllText(appsettingsDevelopmentFilePath, AppsettingsDevelopmentJson);

        var document = new AppSettingsDocument(appsettingsFilePath);
        var envDocument = new AppSettingsEnvDocument(document, appsettingsDevelopmentFilePath);

        File.Delete(appsettingsFilePath);
        File.Delete(appsettingsDevelopmentFilePath);
        return envDocument;
    }
    
    private AppSettingsEnvDocument _getAppSettingsProductionDocument()
    {
        var appsettingsFilePath = Path.Combine(Path.GetTempPath(), "tmp", "appsettings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(appsettingsFilePath) ?? string.Empty);
        File.WriteAllText(appsettingsFilePath, AppsettingsJsonContent);

        var appsettingsProductionFilePath = Path.Combine(Path.GetTempPath(), "tmp", "appsettings.Production.json");
        Directory.CreateDirectory(Path.GetDirectoryName(appsettingsProductionFilePath) ?? string.Empty);
        File.WriteAllText(appsettingsProductionFilePath, AppsettingsProductionJson);

        var document = new AppSettingsDocument(appsettingsFilePath);
        var envDocument = new AppSettingsEnvDocument(document, appsettingsProductionFilePath);

        File.Delete(appsettingsFilePath);
        File.Delete(appsettingsProductionFilePath);
        return envDocument;
    }

    [Fact]
    public void GetTerraformFileContent_WithSingleEnv_ShouldReturnExpectedContent()
    {
        // Arrange
        const string expectedFileContent = """
                                           provider "aws" {
                                               region = "eu-central-1"
                                               profile = "default"
                                           }

                                           locals = {
                                               development_parameters = {
                                                   "/Services/ApiKey" = { type = "String", value = ""}
                                                   "/Services/ClientSecret" = { type = "SecureString", value = ""}
                                                   "/Database/ConnectionString" = { type = "SecureString", value = ""}
                                               }
                                           }

                                           resource "aws_ssm_parameter" "development_params" {
                                               for_each    = local.development_parameters
                                               name        = "/TestApp/Development${each.key}"
                                               description = "Development parameter for TestApp"
                                               type        = each.value.type
                                               value       = each.value.value
                                               tier        = "Standard"

                                               tags = {
                                                   Application = "TestApp"
                                                   Environment = "Development"
                                                   ManagedBy   = "Terraform"
                                               }
                                           }


                                           """;

        List<AppSettingsEnvDocument> envDocuments = [_getAppSettingsDevelopmentDocument()];
        var terraformDocument = new TerraformDocument(envDocuments, "TestApp", true);

        // Act
        var actualContent = terraformDocument.GetTerraformFileContent();

        // Assert
        actualContent.ShouldBe(expectedFileContent);
    }
    
    [Fact]
    public void GetTerraformFileContent_WithMultipleEnvs_ShouldReturnExpectedContent()
    {
        // Arrange
        const string expectedFileContent = """
                                           locals = {
                                               development_parameters = {
                                                   "/Services/ApiKey" = { type = "String", value = ""}
                                                   "/Services/ClientSecret" = { type = "SecureString", value = ""}
                                                   "/Database/ConnectionString" = { type = "SecureString", value = ""}
                                               }
                                               production_parameters = {
                                                   "/Services/ApiKey" = { type = "String", value = ""}
                                                   "/SomeOtherConfig/Key1" = { type = "String", value = ""}
                                                   "/Services/ClientSecret" = { type = "SecureString", value = ""}
                                                   "/Database/ConnectionString" = { type = "SecureString", value = ""}
                                               }
                                           }

                                           resource "aws_ssm_parameter" "development_params" {
                                               for_each    = local.development_parameters
                                               name        = "/TestApp/Development${each.key}"
                                               description = "Development parameter for TestApp"
                                               type        = each.value.type
                                               value       = each.value.value
                                               tier        = "Standard"

                                               tags = {
                                                   Application = "TestApp"
                                                   Environment = "Development"
                                                   ManagedBy   = "Terraform"
                                               }
                                           }
                                           
                                           resource "aws_ssm_parameter" "production_params" {
                                               for_each    = local.production_parameters
                                               name        = "/TestApp/Production${each.key}"
                                               description = "Production parameter for TestApp"
                                               type        = each.value.type
                                               value       = each.value.value
                                               tier        = "Standard"
                                           
                                               tags = {
                                                   Application = "TestApp"
                                                   Environment = "Production"
                                                   ManagedBy   = "Terraform"
                                               }
                                           }


                                           """;

        List<AppSettingsEnvDocument> envDocuments = [
            _getAppSettingsDevelopmentDocument(),
            _getAppSettingsProductionDocument()
        ];
        var terraformDocument = new TerraformDocument(envDocuments, "TestApp", false);

        // Act
        var actualContent = terraformDocument.GetTerraformFileContent();

        // Assert
        actualContent.ShouldBe(expectedFileContent);
    }
}