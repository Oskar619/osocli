namespace osocli
{
    public interface ILoggingOptions
    {
        /// <summary>
        /// Verbosity logging level for console output.
        /// q=> quiet
        /// m=> minimal
        /// n=> normal
        /// d=> detailed
        /// diag=> diagnostic
        /// </summary>
        string Verbosity { get; set; }

        /// <summary>
        /// The log file to save the full logs into.
        /// </summary>
        string LogFile { get; set; }
    }
}
