using Shouldly;
using SsmExporter.CLI.Models;

namespace SsmExporter.Test.Unit;

public class AppSettingsDocumentTests
{
    [Fact]
    public void GetEnvironmentName_ShouldExtractEnvNameFromFilename()
    {
        const string fileName1 = "appsettings.Development.json";
        const string fileName2 = "appsettings.Production.json";

        var env1 = AppSettingsEnvDocument.GetEnvironmentFromFileName(fileName1);
        var env2 = AppSettingsEnvDocument.GetEnvironmentFromFileName(fileName2);

        env1.ShouldBe("Development");
        env2.ShouldBe("Production");
    }

    [Fact]
    public void AppSettingsDocumentCtor_WithValidConfigFile_ShouldLoadParametersAndSecrets()
    {
        const string testJson = """
                                {
                                    "Database": {
                                        "ConnectionString": "ssm-parameter",
                                        "Password": "ssm-secret"
                                    },
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

        var tempFileName = Path.GetTempFileName();
        File.WriteAllText(tempFileName, testJson);

        // Act
        var appSettings = new AppSettingsDocument(tempFileName);

        // Assert
        var parameters = appSettings.Parameters.ToList();
        var secrets = appSettings.Secrets.ToList();

        parameters.Count.ShouldBe(2);
        parameters.ShouldContain("/Database/ConnectionString");
        parameters.ShouldContain("/Services/ApiKey");

        secrets.Count.ShouldBe(2);
        secrets.ShouldContain("/Services/ClientSecret");
        secrets.ShouldContain("/Database/Password");

        if (File.Exists(tempFileName))
        {
            File.Delete(tempFileName);
        }
    }

    [Fact]
    public void AppSettingsEnvDocumentCtor_WithValidConfigFiles_ShouldLoadParametersAndSecretsFromBoth()
    {
        const string appsettingsJson = """
                                       {
                                           "Database": {
                                               "ConnectionString": "ssm-parameter",
                                               "Password": "ssm-secret"
                                           },
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

        const string appsettingsDevelopmentJson = """
                                                  {
                                                      "SomeKey": "ssm-secret"
                                                  }
                                                  """;

        var appsettingsFileName = Path.GetTempFileName();
        File.WriteAllText(appsettingsFileName, appsettingsJson);

        var appsettingsDevelopmentFileName = Path.GetTempFileName();
        File.WriteAllText(appsettingsDevelopmentFileName, appsettingsDevelopmentJson);

        // Act
        var appSettings = new AppSettingsDocument(appsettingsFileName);
        var appSettingsDevelopment = new AppSettingsEnvDocument(appSettings, appsettingsDevelopmentFileName);

        // Assert
        var parameters = appSettingsDevelopment.Parameters.ToList();
        var secrets = appSettingsDevelopment.Secrets.ToList();

        parameters.Count.ShouldBe(2);
        parameters.ShouldContain("/Database/ConnectionString");
        parameters.ShouldContain("/Services/ApiKey");

        secrets.Count.ShouldBe(3);
        secrets.ShouldContain("/Services/ClientSecret");
        secrets.ShouldContain("/Database/Password");
        secrets.ShouldContain("/SomeKey");

        if (File.Exists(appsettingsDevelopmentFileName))
        {
            File.Delete(appsettingsDevelopmentFileName);
        }

        if (File.Exists(appsettingsFileName))
        {
            File.Delete(appsettingsFileName);
        }
    }
}