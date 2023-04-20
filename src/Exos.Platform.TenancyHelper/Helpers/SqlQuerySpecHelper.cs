using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace Exos.Platform.TenancyHelper.Helpers;

/// <summary>
/// Helper extensions for working with <see cref="SqlQuerySpec" /> objects.
/// </summary>
public static class SqlQuerySpecHelper
{
    /// <summary>
    /// Resolves the <see cref="SqlQuerySpec.QueryText" /> and <see cref="SqlQuerySpec.Parameters" /> into an executable query.
    /// </summary>
    /// <param name="querySpec">The query spec.</param>
    /// <returns>The resolved query text.</returns>
    public static string UnparameterizeQuery(SqlQuerySpec querySpec)
    {
        ArgumentNullException.ThrowIfNull(querySpec?.QueryText, nameof(SqlQuerySpec.QueryText));
        ArgumentNullException.ThrowIfNull(querySpec?.Parameters, nameof(SqlQuerySpec.Parameters));

        var builder = new StringBuilder(querySpec.QueryText);
        foreach (var parameter in querySpec.Parameters)
        {
            // Cosmos currently uses our Newtonsoft defaults so this should 'match'
            var value = JsonConvert.SerializeObject(parameter.Value);
            builder.Replace(parameter.Name, value);
        }

        return builder.ToString();
    }
}