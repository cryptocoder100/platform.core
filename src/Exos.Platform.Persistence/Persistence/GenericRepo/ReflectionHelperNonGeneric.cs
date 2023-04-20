#pragma warning disable CA1822 // Mark members as static
namespace Exos.Platform.Persistence.GenericRepo
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Reflection Helper.
    /// </summary>
    public class ReflectionHelperNonGeneric
    {
        /// <summary>
        /// Get properties of the object.
        /// </summary>
        /// <param name="t">Object to get the properties.</param>
        /// <returns>List of properties.</returns>
        public PropertyInfo[] GetProperties(Type t)
        {
            if (t is null)
            {
                throw new ArgumentNullException(nameof(t));
            }

            return t.GetProperties();
        }

        /// <summary>
        /// Get Attribute Info.
        /// </summary>
        /// <param name="propertyInfo"><see cref="PropertyInfo"/>.</param>
        /// <param name="attr"><see cref="Type"/>.</param>
        /// <returns>Attribute Info.</returns>
        public object GetAttributeOnPropertyInfo(PropertyInfo propertyInfo, Type attr)
        {
            if (propertyInfo is null)
            {
                throw new ArgumentNullException(nameof(propertyInfo));
            }

            return propertyInfo.GetCustomAttributes(attr, true).FirstOrDefault();
        }

        /// <summary>
        /// Get Attribute on Type.
        /// </summary>
        /// <param name="type">Type Object.</param>
        /// <param name="attr">Attribute Object.</param>
        /// <returns>Attribute.</returns>
        public object GetAttributeOnType(Type type, Type attr)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return type.GetCustomAttributes(attr, true).FirstOrDefault();
        }
    }
}
#pragma warning restore CA1822 // Mark members as static
