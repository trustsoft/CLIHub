using System;
using Velopack;

namespace CLIHub;

/// <summary>
/// Application entry point. Velopack must run before anything else in the process.
/// </summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
