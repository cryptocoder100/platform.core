#pragma warning disable CA1031 // Do not catch general exception types

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Exos.Platform.AspNetCore.Entities;
using Exos.Platform.AspNetCore.Security;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;

namespace Exos.Platform.AspNetCore.Helpers
{
    internal static class UserContextHelper
    {
        private static volatile byte[] _wotHeaderSecretBytes;

        public static byte[] GetWorkOrderTenantHeaderSigningSecretBytesSafe(UserContextOptions options)
        {
            // Because we don't use locking, it's possible this could get initialized more than once, but no harm with that.
            // We do this because we would be wasting cycles to convert from string to byte[] 2x (inbound and outbound) for every request.

            if (_wotHeaderSecretBytes == null)
            {
                try
                {
                    _wotHeaderSecretBytes = Convert.FromBase64String(options.WorkOrderTenantHeaderSigningSecret);
                }
                catch
                {
                    _wotHeaderSecretBytes = Array.Empty<byte>(); // Indicate initialized
                }
            }

            return _wotHeaderSecretBytes;
        }

        public static bool TryGetWorkOrderTenantFromClaims(ClaimsIdentity identity, out WorkOrderTenantEntity tenant)
        {
            if (identity == null || identity.Claims == null || !identity.Claims.Any())
            {
                tenant = null;
                return false;
            }

            var claims = identity.Claims;
            tenant = new WorkOrderTenantEntity
            {
                // We're using the raw claims constants here because we want to preserve
                // null which the IdentityExtensions methods don't do.

                ServicerGroupTenantId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.WorkorderServicerGroupTenantIdClaim)?.Value,
                VendorTenantId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.WorkOrderVendorTenantIdClaim)?.Value,
                SubContractorTenantId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.WorkOrderSubcontractorTenantIdClaim)?.Value,
                ClientTenantId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.WorkOrderMasterTenantIdClaim)?.Value,
                SubClientTenantId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.WorkOrderSubClientTenantIdClaim)?.Value,
                WorkOrderId = claims.FirstOrDefault(c => c.Type == IdentityExtensions.HeaderWorkOrderIdClaim)?.Value,
                SourceSystemWorkOrderNumber = claims.FirstOrDefault(c => c.Type == IdentityExtensions.SourceSystemWorkOrderNumberClaim)?.Value,
                SourceSystemOrderNumber = claims.FirstOrDefault(c => c.Type == IdentityExtensions.SourceSystemOrderNumberClaim)?.Value,
            };

            // Rudimentary check to see if we have any actual tenant data
            var hasWorkOrderTenant = false;

            hasWorkOrderTenant |= tenant.ServicerGroupTenantId != null;
            hasWorkOrderTenant |= tenant.VendorTenantId != null;
            hasWorkOrderTenant |= tenant.SubContractorTenantId != null;
            hasWorkOrderTenant |= tenant.ClientTenantId != null;
            hasWorkOrderTenant |= tenant.SubClientTenantId != null;
            hasWorkOrderTenant |= tenant.WorkOrderId != null;
            hasWorkOrderTenant |= tenant.SourceSystemWorkOrderNumber != null;
            hasWorkOrderTenant |= tenant.SourceSystemOrderNumber != null;

            return hasWorkOrderTenant;
        }

        public static WorkOrderTenantEntity VerifyAndParseWorkOrderTenant(string value, byte[] key)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            else if (key == null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Exception innerException = null;

            try
            {
                // Extract the signature and query from the form <signature>?<query>
                var queryIndex = value.IndexOf('?', StringComparison.Ordinal);
                if (queryIndex != -1)
                {
                    var query = value.Substring(queryIndex); // Include '?'

                    using var sha256 = new HMACSHA256(key);
                    var signature = WebEncoders.Base64UrlEncode(
                        sha256.ComputeHash(
                            Encoding.UTF8.GetBytes(query)));

                    // Verify the signature calculated matches the one in the string
                    if (MemoryExtensions.SequenceEqual(value.AsSpan(0, queryIndex), signature))
                    {
                        var pairs = QueryHelpers.ParseQuery(query);
                        var tenant = new WorkOrderTenantEntity
                        {
                            ServicerGroupTenantId = pairs.ContainsKey("servicerGroupTenantId") ? pairs["servicerGroupTenantId"] : (string)null,
                            VendorTenantId = pairs.ContainsKey("vendorTenantId") ? pairs["vendorTenantId"] : (string)null,
                            SubContractorTenantId = pairs.ContainsKey("subContractorTenantId") ? pairs["subContractorTenantId"] : (string)null,
                            ClientTenantId = pairs.ContainsKey("clientTenantId") ? pairs["clientTenantId"] : (string)null,
                            SubClientTenantId = pairs.ContainsKey("subClientTenantId") ? pairs["subClientTenantId"] : (string)null,
                            WorkOrderId = pairs.ContainsKey("workOrderId") ? pairs["workOrderId"] : (string)null,
                            SourceSystemWorkOrderNumber = pairs.ContainsKey("sourceSystemWorkOrderNumber") ? pairs["sourceSystemWorkOrderNumber"] : (string)null,
                            SourceSystemOrderNumber = pairs.ContainsKey("sourceSystemOrderNumber") ? pairs["sourceSystemOrderNumber"] : (string)null
                        };

                        return tenant;
                    }
                }
            }
            catch (Exception ex)
            {
                innerException = ex;
            }

            throw new InvalidOperationException("Work order tenant header failed signature validation.", innerException);
        }

        public static string SerializeAndSignWorkOrderTenant(WorkOrderTenantEntity tenant, byte[] key)
        {
            if (tenant == null)
            {
                throw new ArgumentNullException(nameof(tenant));
            }
            else if (key == null || key.Length == 0)
            {
                throw new ArgumentNullException(nameof(key));
            }

            // Build a query string representation of the form <signature>?<query>
            var builder = new QueryBuilder();

            AddParam(builder, "servicerGroupTenantId", tenant.ServicerGroupTenantId);
            AddParam(builder, "vendorTenantId", tenant.VendorTenantId);
            AddParam(builder, "subContractorTenantId", tenant.SubContractorTenantId);
            AddParam(builder, "clientTenantId", tenant.ClientTenantId);
            AddParam(builder, "subClientTenantId", tenant.SubClientTenantId);
            AddParam(builder, "workOrderId", tenant.WorkOrderId);
            AddParam(builder, "sourceSystemWorkOrderNumber", tenant.SourceSystemWorkOrderNumber);
            AddParam(builder, "sourceSystemOrderNumber", tenant.SourceSystemOrderNumber);

            var query = builder.ToQueryString().ToUriComponent();

            // Generate a SHA256 hash of the query string (including '?')
            using var sha256 = new HMACSHA256(key);
            var signature = WebEncoders.Base64UrlEncode(
                sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(query)));

            return signature + query;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddParam(QueryBuilder builder, string paramName, string paramValue)
        {
            // If a param is null, omit it from the query to indicate null instead of an empty string
            if (paramValue != null)
            {
                builder.Add(paramName, paramValue);
            }
        }
    }
}
