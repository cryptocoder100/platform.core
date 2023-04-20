#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1720 // Identifier contains type name
#pragma warning disable CA1308 // Normalize strings to uppercase
namespace Exos.Platform.Persistence.GenericRepo
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Property Container.
    /// </summary>
    public class PropertyContainer
    {
        private readonly Dictionary<string, object> _ids;
        private readonly Dictionary<string, object> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyContainer"/> class.
        /// </summary>
        public PropertyContainer()
        {
            _ids = new Dictionary<string, object>();
            _values = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets IdNames.
        /// </summary>
        public IEnumerable<string> IdNames
        {
            get { return _ids.Keys; }
        }

        /// <summary>
        /// Gets ValueNames.
        /// </summary>
        public IEnumerable<string> ValueNames
        {
            get { return _values.Keys; }
        }

        /// <summary>
        /// Gets AllNames.
        /// </summary>
        public IEnumerable<string> AllNames
        {
            get { return _ids.Keys.Union(_values.Keys); }
        }

        /// <summary>
        /// Gets IdPairs.
        /// </summary>
        public IDictionary<string, object> IdPairs
        {
            get { return _ids; }
        }

        /// <summary>
        /// Gets ValuePairs.
        /// </summary>
        public IDictionary<string, object> ValuePairs
        {
            get { return _values; }
        }

        /// <summary>
        /// Gets AllPairs.
        /// </summary>
        public IEnumerable<KeyValuePair<string, object>> AllPairs
        {
            get { return _ids.Concat(_values); }
        }

        /// <summary>
        /// Gets or sets TableName.
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// This uses the helper to parse properties into PropertyContainer.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="obj">Object.</param>
        /// <returns>PropertyContainer.</returns>
        public PropertyContainer ParseProperties<T>(T obj)
        {
            PropertyInfo[] properties;
            var propertyContainer = new PropertyContainer();
            properties = ReflectionHelper<T>.GetProperties();

            foreach (var property in properties)
            {
                // Skip reference types (but still include string!) OR skip List/arrays etc.
                if ((property.PropertyType.IsClass && property.PropertyType != typeof(string)) ||
                    (!typeof(string).Equals(property.PropertyType) &&
                    typeof(IEnumerable).IsAssignableFrom(property.PropertyType)))
                {
                    continue;
                }

                // Skip methods without a public setter
                if (property.GetSetMethod() == null)
                {
                    continue;
                }

                if (ReflectionHelper<T>.GetAttributeOnPropertyInfo(property, typeof(System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute)) != null)
                {
                    continue;
                }

                // Skip methods specifically ignored
                var name = property.Name;

                // var value = typeof(T).GetProperty(property.Name).GetValue(obj, null);
                var gettter = ReflectionHelper<T>.GetPropertyInfo(property);
                var value = gettter(obj);

                // Expecting one Key attribute.
                if (!propertyContainer.IdNames.Any() &&
                    (ReflectionHelper<T>.GetAttributeOnPropertyInfo(property, typeof(System.ComponentModel.DataAnnotations.KeyAttribute)) != null ||
                    ReflectionHelper<T>.GetAttributeOnPropertyInfo(property, typeof(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute)) != null))
                {
                    propertyContainer.AddId(name, value);
                }
                else
                {
                    propertyContainer.AddValue(name, value);
                }
            }

            var tableName = ReflectionHelper<T>.GetAttributeOnType(typeof(System.ComponentModel.DataAnnotations.Schema.TableAttribute)) as System.ComponentModel.DataAnnotations.Schema.TableAttribute;
            propertyContainer.TableName = tableName != null ? "[" + tableName.Schema + "]" + "." + "[" + tableName.Name + "]" : throw new ExosPersistenceException("Table Name could not be found.");

            // if key is not identified yet, last possible effort
            if (!propertyContainer.IdNames.Any())
            {
                var possibleKey = tableName.Name.ToLowerInvariant() + "id";
                var idpropertyInfo = properties.FirstOrDefault(i => i.Name.ToLowerInvariant() == possibleKey);

                if (idpropertyInfo != null)
                {
                    var gettter = ReflectionHelper<T>.GetPropertyInfo(idpropertyInfo);
                    var value = gettter(obj);
                    propertyContainer.AddId(idpropertyInfo.Name, value);
                }
            }

            return propertyContainer;
        }

        /// <summary>
        /// Add Id.
        /// </summary>
        /// <param name="name">Property Name.</param>
        /// <param name="value">Property Value.</param>
        internal void AddId(string name, object value)
        {
            _ids.Add(name, value);
        }

        /// <summary>
        /// Add Value.
        /// </summary>
        /// <param name="name">Property Name.</param>
        /// <param name="value">Property Value.</param>
        internal void AddValue(string name, object value)
        {
            _values.Add(name, value);
        }
    }
}
#pragma warning restore CA1720 // Identifier contains type name
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1308 // Normalize strings to uppercase