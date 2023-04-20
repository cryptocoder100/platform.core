namespace Exos.Platform.AspNetCore.Extensions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// ActivityExtensions is used for managing custom properties when http context is not present.
    /// </summary>
    public static class ActivityExtensions
    {
        /// <summary>
        /// Sets the Key Value in Activity.
        /// </summary>
        /// <param name="activity">current activity.</param>
        /// <param name="keyValuesToSet">keyValuesToSet.</param>
        public static void SetKeyValues(this Activity activity, Dictionary<string, string> keyValuesToSet)
        {
            if (activity != null && keyValuesToSet != null && keyValuesToSet.Count > 0)
            {
                foreach (var kv in keyValuesToSet)
                {
                    var existingKv = activity?.Tags?.Where(i => i.Key == kv.Key)?.FirstOrDefault();

                    if (existingKv == null || default(KeyValuePair<string, string>).Equals(existingKv))
                    {
                        activity.SetTag(kv.Key, kv.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the Value for given Key.
        /// </summary>
        /// <param name="activity">current activity.</param>
        /// <param name="key">keyValueToGet.</param>
        /// <returns>Returns the Value for given Key.</returns>
        public static string GetValue(this Activity activity, string key)
        {
            if (activity != null && activity?.Tags != null)
            {
                // Not all log statements are in the context of a request. This one is.
                var trackingIdKv = activity.Tags?.Where(i => i.Key == key)?.FirstOrDefault();
                if (!trackingIdKv.Equals(default(KeyValuePair<string, string>)))
                {
                    return trackingIdKv?.Value;
                }
            }

            return default(string);
        }

        /// <summary>
        /// Sets the Tracking Id.
        /// </summary>
        /// <param name="activity">Current Activity.</param>
        /// <param name="value">value to set.</param>
        public static void SetTrackingId(this Activity activity, string value)
        {
            if (!string.IsNullOrEmpty(value) && activity != null && activity?.Tags != null)
            {
                activity?.SetKeyValues(new Dictionary<string, string> { { "Tracking-Id", value } });
            }
        }

        /// <summary>
        /// Gets the Tracking Id.
        /// </summary>
        /// <param name="activity">current activity.</param>
        /// <returns>TrackingId Value.</returns>
        public static string GetTrackingId(this Activity activity)
        {
            if (activity != null && activity?.Tags != null)
            {
                // Not all log statements are in the context of a request. This one is.
                var trackingIdKv = activity?.Tags?.Where(i => i.Key == "Tracking-Id")?.FirstOrDefault();
                if (!trackingIdKv.Equals(default(KeyValuePair<string, string>)))
                {
                    return trackingIdKv?.Value;
                }
            }

            return default(string);
        }
    }
}
