namespace CLIHub.Core.Models;

public sealed record ActionAvailability(bool Run, bool Resume, bool Init, bool Update)
{
    public static ActionAvailability None { get; } = new(false, false, false, false);
}
