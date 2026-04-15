static class Log
{
    private static readonly bool IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
    
    // ANSI color codes for Unix-like systems
    private static readonly string Reset = IsWindows ? "" : "\u001b[0m";
    private static readonly string Blue = IsWindows ? "" : "\u001b[34m";
    private static readonly string Yellow = IsWindows ? "" : "\u001b[33m";
    private static readonly string Red = IsWindows ? "" : "\u001b[31m";
    
    // Log level prefixes with colors for Unix-like systems
    private static readonly string DebugPrefix = IsWindows ? "[LOG]" : $"{Blue}[LOG]{Reset}";
    private static readonly string WarnPrefix = IsWindows ? "[LOG][WARN]" : $"{Yellow}[WARN]{Reset}";
    private static readonly string ErrorPrefix = IsWindows ? "[LOG][ERROR]" : $"{Red}[ERROR]{Reset}";
    
    // Timestamp format
    private static string GetTimestamp() => DateTime.Now.ToString("HH:mm:ss.fff");
    
    // Default log methods without timestamp
    public static void Debug(string message) => WriteLog(DebugPrefix, message);
    public static void Warn(string message) => WriteLog(WarnPrefix, message);
    public static void Error(string message) => WriteLog(ErrorPrefix, message);
    
    // Overloaded log methods with timestamp
    public static void DebugWithTimestamp(string message) => WriteLog(DebugPrefix, message, true);
    public static void WarnWithTimestamp(string message) => WriteLog(WarnPrefix, message, true);
    public static void ErrorWithTimestamp(string message) => WriteLog(ErrorPrefix, message, true);
    
    private static void WriteLog(string prefix, string message, bool includeTimestamp = false)
    {
        if (includeTimestamp)
        {
            Console.WriteLine($"{GetTimestamp()} {prefix} {message}");
        }
        else
        {
            Console.WriteLine($"{prefix} {message}");
        }
    }
}