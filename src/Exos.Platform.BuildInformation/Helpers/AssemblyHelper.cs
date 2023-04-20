namespace Exos.Platform.BuildInformation.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;

    /// <summary>
    /// Helper methods for working with <see cref="Assembly" /> classes.
    /// </summary>
    public static class AssemblyHelper
    {
        /// <summary>
        /// Gets the assembly name as stored in the <see cref="AssemblyTitleAttribute" />.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public static string GetName(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return assembly.GetName().Name;
        }

        /// <summary>
        /// Gets the assembly version as stored in the <see cref="AssemblyVersionAttribute" />.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public static Version GetVersion(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return assembly.GetName().Version;
        }

        /*
        /// <summary>
        /// Gets the assembly copyright as stored in the <see cref="AssemblyCopyrightAttribute" />.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        public static string GetCopyright(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            var copyrightAttribute = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            return copyrightAttribute.Copyright;
        }
        */
    }
}
