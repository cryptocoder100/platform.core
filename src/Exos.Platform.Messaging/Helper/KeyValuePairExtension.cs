namespace System.Collections.Generic
{
    /// <summary>
    /// KeyValuePairExtension.
    /// </summary>
    public static class KeyValuePairExtension
    {
        /// <summary>
        /// GetValue.
        /// </summary>
        /// <param name="keyValuePair">keyValuePair.</param>
        /// <returns>string.</returns>
        public static string GetValue(this KeyValuePair<string, object> keyValuePair)
        {
            return keyValuePair.Equals(default(KeyValuePair<string, object>)) ? null : (string)keyValuePair.Value;
        }

        /// <summary>
        /// GetKey.
        /// </summary>
        /// <param name="keyValuePair">keyValuePair.</param>
        /// <returns>string.</returns>
        public static string GetKey(this KeyValuePair<string, object> keyValuePair)
        {
            return keyValuePair.Equals(default(KeyValuePair<string, object>)) ? null : keyValuePair.Key;
        }
    }
}
