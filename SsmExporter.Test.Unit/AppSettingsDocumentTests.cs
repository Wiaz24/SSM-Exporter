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
        
        Assert.Equal(2, parameters.Count);
        Assert.Contains("/Database/ConnectionString", parameters);
        Assert.Contains("/Services/ApiKey", parameters);
        
        Assert.Equal(2, secrets.Count);
        Assert.Contains("/Database/Password", secrets);
        Assert.Contains("/Services/ClientSecret", secrets);

        if (File.Exists(tempFileName))
        {
            File.Delete(tempFileName);
        }
    }
}