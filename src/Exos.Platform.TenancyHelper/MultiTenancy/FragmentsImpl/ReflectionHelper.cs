namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl
{
    using System;

    /// <summary>
    ///  Utility Class used to access objects using reflection.
    /// </summary>
    public static class ReflectionHelper
    {
        /// <summary>
        /// Get the value of a property.
        /// </summary>
        /// <param name="src">Object.</param>
        /// <param name="propName">Property Name.</param>
        /// <returns>Value from the property.</returns>
        public static object GetPropValue(object src, string propName)
        {
            if (src == null)
            {
                return null;
            }

            if (src.GetType().GetProperty(propName) != null)
            {
                return src.GetType().GetProperty(propName).GetValue(src, null);
            }
            else
            {
                throw new ArgumentException($"Property with name={propName} does not exists on the source object of type ={src.GetType().ToString()}");
            }
        }
    }
}
