using System.Reflection;

internal static class AppVersion
{
    public static string Current { get; } = Read();

    private static string Read()
    {
        Assembly assembly = typeof(AppVersion).Assembly;
        string informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
        return informational.Split('+')[0];
    }
}
