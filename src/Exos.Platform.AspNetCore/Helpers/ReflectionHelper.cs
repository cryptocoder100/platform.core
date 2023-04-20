#pragma warning disable SA1405 // Debug.Assert should provide message text
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Exos.Platform.AspNetCore.Helpers
{
    /// <summary>
    /// Helper methods for using .NET Reflection.
    /// </summary>
    public static class ReflectionHelper
    {
        private static readonly ConcurrentDictionary<string, Lazy<IEnumerable<PropertyMap>>> _propertyMaps = new ConcurrentDictionary<string, Lazy<IEnumerable<PropertyMap>>>();
        private static readonly ConcurrentDictionary<string, Lazy<DynamicMethod>> _delegateMaps = new ConcurrentDictionary<string, Lazy<DynamicMethod>>();

        /// <summary>
        /// Copies the property values from one object to another.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object.</typeparam>
        /// <typeparam name="TTarget">The type of the target object.</typeparam>
        /// <param name="source">The source object to copy from.</param>
        /// <param name="caseSensitive">Whether property names are case-sensitive. The default is <c>false</c>.</param>
        /// <returns>An instance of <typeparamref name="TTarget" /> with the property values copied from <paramref name="source" />.</returns>
        /// <remarks>
        /// This helper method is no substitute for a robust mapping library like Mapster. It exist only so we don't need a
        /// dependency on a third-party in our platform libraries.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TTarget Map<TSource, TTarget>(TSource source, bool caseSensitive = false)
            where TSource : class
            where TTarget : class
        {
            if (source == null)
            {
                return null;
            }

            // Can we emit and JIT IL?
            if (RuntimeFeature.IsDynamicCodeCompiled)
            {
                return MapWithILEmit<TSource, TTarget>(source, caseSensitive);
            }

            return MapWithReflection<TSource, TTarget>(source, caseSensitive);
        }

        // Internal for testing
        internal static TTarget MapWithReflection<TSource, TTarget>(TSource source, bool caseSensitive)
        {
            Debug.Assert(source != null);

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            // Get the cached property map
            var key = GetMapKey(sourceType, targetType, caseSensitive);
            var map = _propertyMaps.GetOrAdd(
                key,
                x => new Lazy<IEnumerable<PropertyMap>>(
                    () =>
                    {
                        var props = GetMatchingProperties(sourceType, targetType, caseSensitive);
                        return props;
                    })).Value;

            // Map the properties by iterating over each one
            var target = Activator.CreateInstance<TTarget>();
            foreach (var pm in map)
            {
                var val = pm.SourceProperty.GetValue(source, null);
                pm.TargetProperty.SetValue(target, val, null);
            }

            return target;
        }

        // Internal for testing
        internal static TTarget MapWithILEmit<TSource, TTarget>(TSource source, bool caseSensitive)
        {
            Debug.Assert(source != null);

            var sourceType = typeof(TSource);
            var targetType = typeof(TTarget);

            // Get the cached delegate
            var key = GetMapKey(sourceType, targetType, caseSensitive);
            var del = _delegateMaps.GetOrAdd(
                key,
                x => new Lazy<DynamicMethod>(
                    () =>
                    {
                        var args = new[] { sourceType, targetType };
                        var methodName = GetMapMethodName(sourceType, targetType, caseSensitive);
                        var dm = new DynamicMethod(methodName, null, args);
                        var il = dm.GetILGenerator();
                        var props = GetMatchingProperties(sourceType, targetType, caseSensitive);

                        foreach (var pm in props)
                        {
                            il.Emit(OpCodes.Ldarg_1);
                            il.Emit(OpCodes.Ldarg_0);
                            il.EmitCall(OpCodes.Callvirt, pm.SourceProperty.GetGetMethod(), null);
                            il.EmitCall(OpCodes.Callvirt, pm.TargetProperty.GetSetMethod(), null);
                        }

                        il.Emit(OpCodes.Ret);

                        return dm;
                    })).Value;

            // Map the properties using the dynamic method
            var target = Activator.CreateInstance<TTarget>();
            var args = new object[] { source, target };
            del.Invoke(null, args);

            return target;
        }

        private static IEnumerable<PropertyMap> GetMatchingProperties(Type sourceType, Type targetType, bool caseSensitive)
        {
            var sourceProperties = sourceType.GetProperties().Where(p => p.CanRead);
            var targetProperties = targetType.GetProperties().Where(p => p.CanWrite);

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var properties = sourceProperties
                .SelectMany(s => targetProperties
                    .Select(t => new PropertyMap { SourceProperty = s, TargetProperty = t }))
                .Where(pm =>
                    pm.SourceProperty.Name.Equals(pm.TargetProperty.Name, comparison) &&
                    pm.SourceProperty.PropertyType == pm.TargetProperty.PropertyType);

            return properties;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetMapKey(Type sourceType, Type targetType, bool caseSensitive)
        {
            return $"{sourceType.FullName} -> {targetType.FullName} ({(caseSensitive ? "CaseSensitive" : "CaseInsensitive")})";
        }

        private static string GetMapMethodName(Type sourceType, Type targetType, bool caseSensitive)
        {
            var safeSource = sourceType.FullName.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("+", string.Empty, StringComparison.OrdinalIgnoreCase);
            var safeTarget = targetType.FullName.Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase).Replace("+", string.Empty, StringComparison.OrdinalIgnoreCase);

            return $"Map{safeSource}To{safeTarget}{(caseSensitive ? "CaseSensitive" : "CaseInsensitive")}";
        }

        private struct PropertyMap
        {
            public PropertyInfo SourceProperty { get; set; }

            public PropertyInfo TargetProperty { get; set; }
        }
    }
}

// References
// https://www.twilio.com/blog/building-blazing-fast-object-mapper-c-sharp-net-core
// https://stackoverflow.com/questions/40564926/select-multiple-columns-without-join-in-linq
// https://github.com/dotnet/runtime/issues/25959

#pragma warning restore SA1405 // Debug.Assert should provide message text