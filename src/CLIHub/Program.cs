namespace CLIHub;

using Velopack;

/// <summary>
///   Application entry point. Velopack must run before anything else in the process.
/// </summary>
public static class Program
{
    /// <summary>
    ///   Runs Velopack's install hooks and starts the WPF application.
    /// </summary>
    /// <param name="args"> Command-line arguments, forwarded to WPF. </param>
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
