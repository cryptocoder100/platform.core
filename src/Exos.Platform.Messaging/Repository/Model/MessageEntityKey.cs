namespace Exos.Platform.Messaging.Repository.Model
{
    using System;

    /// <inheritdoc/>
    public class MessageEntityKey : IEquatable<MessageEntityKey>
    {
        /// <summary>
        /// Gets or sets EntityName.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or sets Owner.
        /// </summary>
        public string Owner { get; set; }

        /// <inheritdoc/>
        public bool Equals(MessageEntityKey other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(EntityName, other.EntityName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(Owner, other.Owner, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((MessageEntityKey)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((EntityName != null ? EntityName.GetHashCode(StringComparison.Ordinal) : 0) * 397) ^ (Owner != null ? Owner.GetHashCode(StringComparison.Ordinal) : 0);
            }
        }
    }
}
