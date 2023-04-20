namespace Exos.Platform.Messaging.Core
{
    /// <summary>
    /// Message Configuration.
    /// </summary>
    public class MessageConfig
    {
        /// <summary>
        /// Gets or Sets the Azure entity name , Topic or Queue.
        /// </summary>
        public string EntityName { get; set; }

        /// <summary>
        /// Gets or Sets the Azure the Owner of the Entity.
        /// Each micro service or module's name who has access to the entity.
        /// </summary>
        public string EntityOwner { get; set; }
    }
}
