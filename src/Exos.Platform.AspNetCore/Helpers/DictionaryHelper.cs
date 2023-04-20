namespace Exos.Platform.Helpers;

/// <summary>
/// Helper methods for working with <see cref="IDictionary{TKey, TValue}" /> classes.
/// </summary>
public static class DictionaryHelper
{
    /// <summary>
    /// Gets the element at the specified <paramref name="key" /> if found; otherwise, <see langword="default" />.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <param name="dictionary">An <see cref="IDictionary{TKey, TValue}" /> instance.</param>
    /// <param name="key">The key of the element to get.</param>
    /// <returns>The element specified if found; otherwise, <see langword="default" />.</returns>
    public static TValue GetValueOrDefault<TKey, TValue>(IDictionary<TKey, TValue> dictionary, TKey key)
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (dictionary.TryGetValue(key, out TValue value))
        {
            return value;
        }

        return default;
    }
}
