#pragma warning disable SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning disable CA1724 // Type names should not match namespaces
#pragma warning disable CA1710 // Identifiers should have correct suffix
#pragma warning disable CA1813 // Avoid unsealed attributes
#pragma warning disable CA1019 // Define accessors for attribute arguments
namespace Exos.Platform.Persistence.EventTracking
{
    using System;

    /// <summary>
    /// Class Attribute to configure an Entity for Event Tracking.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class EventTracking : Attribute
    {
        private bool _add;
        private bool _modify;
        private bool _delete;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTracking"/> class.
        /// </summary>
        public EventTracking()
        {
            _add = false;
            _modify = false;
            _delete = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTracking"/> class.
        /// </summary>
        /// <param name="add">Add an event when an entity is created.</param>
        /// <param name="modify">Add an event when an entity is modified.</param>
        /// <param name="delete">Add an event when an entity is deleted. </param>
        public EventTracking(bool add, bool modify, bool delete)
        {
            _add = add;
            _modify = modify;
            _delete = delete;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is added.
        /// </summary>
        public virtual bool Add
        {
            get { return _add; }

            set { _add = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is modified.
        /// </summary>
        public virtual bool Modify
        {
            get { return _modify; }

            set { _modify = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is deleted.
        /// </summary>
        public virtual bool Delete
        {
            get { return _delete; }

            set { _delete = value; }
        }
    }
}
#pragma warning restore SA1649 // SA1649FileNameMustMatchTypeName
#pragma warning restore CA1724 // Type names should not match namespaces
#pragma warning restore CA1710 // Identifiers should have correct suffix
#pragma warning restore CA1813 // Avoid unsealed attributes
#pragma warning restore CA1019 // Define accessors for attribute arguments
