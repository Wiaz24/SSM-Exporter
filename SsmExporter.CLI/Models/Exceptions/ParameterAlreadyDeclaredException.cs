namespace SsmExporter.CLI.Models.Exceptions;

public class ParameterAlreadyDeclaredException : Exception
{
    public string ParameterPath { get; }

    public ParameterAlreadyDeclaredException(string parameterPath, string parameterType)
        : base($"'{parameterPath}' has already been declared as {parameterType}.")
    {
        ParameterPath = parameterPath;
    }
}