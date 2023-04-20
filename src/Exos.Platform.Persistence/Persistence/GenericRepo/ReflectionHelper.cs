#pragma warning disable CA1000 // Do not declare static members on generic types
namespace Exos.Platform.Persistence.GenericRepo
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// ReflectionHelper, this caches the expensive lambda's on properties.
    /// </summary>
    /// <typeparam name="T">Object to access.</typeparam>
    public static class ReflectionHelper<T>
    {
        private static ConcurrentDictionary<string, PropertyInfo[]> _propertiesCache = new ConcurrentDictionary<string, PropertyInfo[]>();
        private static ConcurrentDictionary<string, Func<T, object>> _propertyInfoCache = new ConcurrentDictionary<string, Func<T, object>>();
        private static ConcurrentDictionary<string, object> _attrInfoCache = new ConcurrentDictionary<string, object>();
        private static ConcurrentDictionary<string, object> _classAttrInfoCache = new ConcurrentDictionary<string, object>();

        /// <summary>
        /// Get properties of the object.
        /// </summary>
        /// <returns>List of properties.</returns>
        public static PropertyInfo[] GetProperties()
        {
            return _propertiesCache.GetOrAdd(typeof(T).ToString(), t =>
            {
                return typeof(T).GetProperties();
            });
        }

        /// <summary>
        /// Get Property Info.
        /// </summary>
        /// <param name="propertyInfo"><see cref="PropertyInfo"/>.</param>
        /// <returns>Property Info.</returns>
        public static Func<T, object> GetPropertyInfo(PropertyInfo propertyInfo)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            return _propertyInfoCache.GetOrAdd(typeof(T).ToString() + propertyInfo.Name, t =>
            {
                var instance = Expression.Parameter(typeof(T), propertyInfo.Name);

                // var propertyTest = Expression.Property(instance, propertyInfo);
                var convert = Expression.TypeAs(Expression.Property(instance, propertyInfo), typeof(object));
                var gettter = Expression.Lambda<Func<T, object>>(convert, instance).Compile();
                return gettter;
            });
        }

        /// <summary>
        /// Get Attribute Info.
        /// </summary>
        /// <param name="propertyInfo"><see cref="PropertyInfo"/>.</param>
        /// <param name="attr"><see cref="Type"/>.</param>
        /// <returns>Attribute Info.</returns>
        public static object GetAttributeOnPropertyInfo(PropertyInfo propertyInfo, Type attr)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            if (attr is null)
            {
                throw new ArgumentNullException(nameof(attr));
            }

            return _attrInfoCache.GetOrAdd(typeof(T).ToString() + propertyInfo.Name + attr.ToString(), t =>
            {
                return propertyInfo.GetCustomAttributes(
                attr, true).FirstOrDefault();
            });
        }

        /// <summary>
        /// Get Attribute on Type.
        /// </summary>
        /// <param name="attr"><see cref="Type"/>.</param>
        /// <returns>Attribute.</returns>
        public static object GetAttributeOnType(Type attr)
        {
            return _classAttrInfoCache.GetOrAdd(typeof(T).ToString(), t =>
            {
                return typeof(T).GetCustomAttributes(attr, true).FirstOrDefault();
            });
        }
    }
}
#pragma warning restore CA1000 // Do not declare static members on generic types
