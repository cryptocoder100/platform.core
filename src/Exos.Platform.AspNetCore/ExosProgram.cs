#nullable enable
#pragma warning disable SA1009 // Closing parenthesis should be spaced correctly

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Exos.Platform.AspNetCore.Helpers;

namespace Exos.Platform;

/// <summary>
/// EXOS program setup.
/// </summary>
public static class ExosProgram
{
    /// <summary>
    /// Will attempt to log application crashes to Application Insights before the process exits.
    /// </summary>
    public static void LogUnhandledExceptions()
    {
        // TODO
    }

    /// <summary>
    /// Configures the console to display information about the service.
    /// </summary>
    public static void HookConsole()
    {
        try
        {
            // Designed to be called in the entry of a .NET service, so we can reliably
            // say the entry assembly and standard build attributes will never be null.

            var entryName = AssemblyHelper.EntryAssemblyName!;
            var entryVersion = AssemblyHelper.EntryAssemblyVersion!.ToString(3);
            var entryConfiguration = AssemblyHelper.EntryAssembly!.GetCustomAttributes<AssemblyConfigurationAttribute>().First().Configuration;

            var platformProduct = AssemblyHelper.PlatformAssembly.GetCustomAttributes<AssemblyProductAttribute>().First().Product;
            var platformCompany = AssemblyHelper.PlatformAssembly.GetCustomAttributes<AssemblyCompanyAttribute>().First().Company;
            var platformVersion = AssemblyHelper.GetVersion(AssemblyHelper.PlatformAssembly)!.ToString(3);
            var platformConfiguration = AssemblyHelper.PlatformAssembly.GetCustomAttributes<AssemblyConfigurationAttribute>().First().Configuration;
            var platformCopyright = AssemblyHelper.PlatformAssembly.GetCustomAttributes<AssemblyCopyrightAttribute>().First().Copyright;

            var entryNameSimplified = entryName.StartsWith("Exos.", StringComparison.OrdinalIgnoreCase)
                ? entryName.Substring(5)
                : entryName;

            Console.Title = $"{entryNameSimplified} v{entryVersion} ({entryConfiguration})";

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"{platformProduct} by {platformCompany}");
            Console.WriteLine(platformCopyright);

            Console.WriteLine($"{entryName} v{entryVersion} ({entryConfiguration})");
            WriteMetadata(AssemblyHelper.EntryAssembly!);

            Console.WriteLine($"Exos.Platform v{platformVersion} ({platformConfiguration})");
            WriteMetadata(AssemblyHelper.PlatformAssembly);

            Console.WriteLine();
        }
        finally
        {
            Console.ResetColor();
        }
    }

    private static void WriteMetadata(Assembly assembly)
    {
        Debug.Assert(assembly != null);

        const string unknown = "TBD";

        // Metadata can be null if the repo is misconfigured
        string? GetMetadata(string key)
        {
            return assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
                ?.Where(ama => key.Equals(ama.Key, StringComparison.OrdinalIgnoreCase))
                ?.FirstOrDefault()
                ?.Value;
        }

        Console.WriteLine($"  Build timestamp  : {GetMetadata("BuildTimestamp") ?? unknown}");
        Console.WriteLine($"  Build number     : {GetMetadata("BuildNumber") ?? unknown}");
        Console.WriteLine($"  Source branch    : {GetMetadata("SourceBranch") ?? unknown}");
        Console.WriteLine($"  Source version   : {GetMetadata("SourceVersion") ?? unknown}");
        Console.WriteLine($"  Source path      : {GetMetadata("SourcePath") ?? unknown}");
    }
}
