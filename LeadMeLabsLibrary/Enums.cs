namespace LeadMeLabsLibrary;

public static class Enums
{
    /// <summary>
    /// Describe the different levels of logging, only the most essential messages are printed at None.
    /// The levels are [None - essential only, Normal - basic messages and commands, Debug - anything that can be used for information, Verbose - everything].
    /// </summary>
    public enum LogLevel
    {
        Off,
        Error,
        Info,
        Normal,
        Debug,
        Verbose,
        Update
    }
}