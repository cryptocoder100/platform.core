#nullable enable
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

using System;
using System.Linq;
using System.Reflection;

namespace Exos.Platform.AspNetCore.Helpers;

/// <summary>
/// Helper methods for working with <see cref="Assembly" /> classes.
/// </summary>
public static class AssemblyHelper
{
    private static readonly Assembly _platformAssembly = typeof(AssemblyHelper).Assembly;
    private static readonly Assembly? _entryAssembly = Assembly.GetEntryAssembly();

    // Cached for quick access
    private static readonly string? _entryAssemblyName = _entryAssembly?.GetName()?.Name;
    private static readonly Version? _entryAssemblyVersion = _entryAssembly?.GetName()?.Version;
    private static readonly string? _entryAssemblyFullName = _entryAssembly?.FullName;

    /// <summary>
    /// Gets the Exos.Platform assembly.
    /// </summary>
    public static Assembly PlatformAssembly
        => _platformAssembly;

    /// <summary>
    /// Gets the entry (application) assembly.
    /// </summary>
    public static Assembly? EntryAssembly
        => _entryAssembly;

    /// <summary>
    /// Gets the name of the entry assembly as stored in the <see cref="AssemblyTitleAttribute" />.
    /// </summary>
    public static string? EntryAssemblyName
        => _entryAssemblyName;

    /// <summary>
    /// Gets the version of the entry assembly as stored in the <see cref="AssemblyVersionAttribute" />.
    /// </summary>
    public static Version? EntryAssemblyVersion
        => _entryAssemblyVersion;

    /// <summary>
    /// Gets the full display name of the entry assembly.
    /// </summary>
    public static string? EntryAssemblyFullName
        => _entryAssemblyFullName;

    /// <summary>
    /// Gets the assembly name as stored in the <see cref="AssemblyTitleAttribute" />.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static string? GetName(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetName().Name;
    }

    /// <summary>
    /// Gets the full display name of an assembly.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static string? GetFullName(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.FullName;
    }

    /// <summary>
    /// Gets the assembly version as stored in the <see cref="AssemblyVersionAttribute" />.
    /// </summary>
    /// <param name="assembly">The assembly.</param>
    public static Version? GetVersion(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        return assembly.GetName().Version;
    }
}
