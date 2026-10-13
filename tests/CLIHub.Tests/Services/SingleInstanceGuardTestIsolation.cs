namespace CLIHub.Tests.Services;

using CLIHub.Core.Infrastructure.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
///   Replaces the production <see cref="SingleInstanceGuard"/> registration with a guard that uses
///   unique mutex and pipe names, so tests that resolve the real guard cannot collide with other
///   test processes sharing the Windows session.
/// </summary>
internal static class SingleInstanceGuardTestIsolation
{
    public static void ReplaceGuardWithUniqueNames(this IServiceCollection services)
    {
        services.RemoveAll<SingleInstanceGuard>();
        services.RemoveAll<ISingleInstanceGuard>();
        services.AddSingleton(_ => new SingleInstanceGuard(
            $@"Local\CLIHub.Tests.{Guid.NewGuid():N}",
            $"CLIHub.Tests.{Guid.NewGuid():N}"));
        services.AddSingleton<ISingleInstanceGuard>(sp => sp.GetRequiredService<SingleInstanceGuard>());
    }
}
