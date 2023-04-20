namespace Exos.Platform.Persistence.Encryption
{
    using System;

    /// <summary>
    /// Attribute to decorate fields for encryption / decryption.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class EncryptedAttribute : Attribute
    {
    }
}
