#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable CA1502 // Avoid excessive complexity
namespace Exos.Platform.TenancyHelper.MultiTenancy.FragmentsImpl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Parse Multi-Tenancy policy document.
    /// </summary>
    internal class TenantParser
    {
        private readonly IPolicyContext _policyContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantParser"/> class.
        /// </summary>
        /// <param name="policyContext">Policy Context.</param>
        public TenantParser(IPolicyContext policyContext)
        {
            _policyContext = policyContext;
        }

        /// <summary>
        /// Gets or sets a value indicating whether if is an Insert Policy.
        /// </summary>
        protected bool IsInsertPolicy { get; set; }

        /// <summary>
        /// Validate Policy Fragment.
        /// </summary>
        /// <param name="clientTenantIdPolicyFragment">Policy Fragment.</param>
        public void ValidateClientIdPolicyFragment(dynamic clientTenantIdPolicyFragment)
        {
            if (clientTenantIdPolicyFragment == null)
            {
                throw new ArgumentNullException(nameof(clientTenantIdPolicyFragment), "Policy does not have a valid fragment!");
            }
        }

        /// <summary>
        /// Process Context of Source Filed in the Multi-Tenancy Policy.
        /// </summary>
        /// <param name="tenantIdPolicyCtxFragment">Policy Fragment.</param>
        /// <param name="objectWithTenantIds">Object with Tenant Id.</param>
        /// <param name="targetContextField">Context Field.</param>
        /// <returns>Tenancy Values.</returns>
        public ProcessIfElseCtxDataResponse ProcessContextOrSourceField(dynamic tenantIdPolicyCtxFragment, object objectWithTenantIds, bool targetContextField)
        {
            if (tenantIdPolicyCtxFragment == null)
            {
                throw new ArgumentNullException(nameof(tenantIdPolicyCtxFragment));
            }

            if (objectWithTenantIds == null)
            {
                throw new ArgumentNullException(nameof(objectWithTenantIds));
            }

            ProcessIfElseCtxDataResponse resp = new ProcessIfElseCtxDataResponse() { UseReturnValue = true };

            if (tenantIdPolicyCtxFragment != null)
            {
                if (tenantIdPolicyCtxFragment is JValue && tenantIdPolicyCtxFragment.Type == JTokenType.String)
                {
                    object sourceFieldValue = null;
                    if (targetContextField)
                    {
                        // look in user context
                        if (_policyContext.GetUserContext().GetType().GetProperty((string)tenantIdPolicyCtxFragment) != null)
                        {
                            sourceFieldValue = ReflectionHelper.GetPropValue(_policyContext.GetUserContext(), (string)tenantIdPolicyCtxFragment);
                        }
                        else
                        {
                            // look in policy context
                            if (!_policyContext.ContainsCustomField((string)tenantIdPolicyCtxFragment))
                            {
                                throw new ArgumentException($"Could not find custom field in the context:{(string)tenantIdPolicyCtxFragment}");
                            }

                            sourceFieldValue = _policyContext.GetCustomIntField((string)tenantIdPolicyCtxFragment);
                        }
                    }
                    else
                    {
                        sourceFieldValue = ReflectionHelper.GetPropValue(objectWithTenantIds, (string)tenantIdPolicyCtxFragment);
                    }

                    if (sourceFieldValue != null)
                    {
                        resp.ReturnVal = GetValue(sourceFieldValue);
                        return resp;
                    }
                    else
                    {
                        resp.ReturnVal = null;
                        return resp;
                    }
                }
                else
                {
                    if (tenantIdPolicyCtxFragment.IfPresent != null)
                    {
                        if (tenantIdPolicyCtxFragment.IfPresent is JValue && tenantIdPolicyCtxFragment.IfPresent.Type == JTokenType.String)
                        {
                            object sourceFieldValue = null;
                            if (targetContextField)
                            {
                                if (_policyContext.GetUserContext().GetType().GetProperty((string)tenantIdPolicyCtxFragment.IfPresent) != null)
                                {
                                    sourceFieldValue = ReflectionHelper.GetPropValue(_policyContext.GetUserContext(), (string)tenantIdPolicyCtxFragment.IfPresent);
                                }
                                else
                                {
                                    if (_policyContext.ContainsCustomField((string)tenantIdPolicyCtxFragment.IfPresent))
                                    {
                                        sourceFieldValue = _policyContext.GetCustomIntField((string)tenantIdPolicyCtxFragment.IfPresent);
                                    }
                                }
                            }
                            else
                            {
                                sourceFieldValue = objectWithTenantIds.GetType().GetProperty((string)tenantIdPolicyCtxFragment.IfPresent) != null ? ReflectionHelper.GetPropValue(objectWithTenantIds, (string)tenantIdPolicyCtxFragment.IfPresent) : null;
                            }

                            var value = GetValue(sourceFieldValue);
                            if (value != null && value.Count > 0 && value.Where(i => i != 0).ToList().Count > 0)
                            {
                                resp.ReturnVal = value;
                                return resp;
                            }
                            else
                            {
                                if (tenantIdPolicyCtxFragment.Else is JValue && tenantIdPolicyCtxFragment.Else.Type == JTokenType.Integer)
                                {
                                    var sourceFieldElseValue = (long)tenantIdPolicyCtxFragment.Else;
                                    if (long.TryParse(tenantIdPolicyCtxFragment.Else.ToString(), out long outValResp) && outValResp != 0)
                                    {
                                        if (outValResp == -3)
                                        {
                                            resp.ReturnVal = new List<long>() { _policyContext.GetUserContext().TenantId };
                                            return resp;
                                        }
                                        else
                                        {
                                            resp.ReturnVal = new List<long>() { outValResp };
                                            return resp;
                                        }
                                    }
                                    else
                                    {
                                        resp.ReturnVal = null;
                                        return resp;
                                    }
                                }
                                else
                                {
                                    resp.ReturnVal = new List<long>() { 0 };
                                    resp.UseReturnValue = false;
                                    resp.ProcessElse = true;
                                    return resp;
                                }
                            }
                        }
                        else
                        {
                            resp.ReturnVal = new List<long>() { 0 };
                            resp.UseReturnValue = false;
                            resp.ProcessIf = true;
                            return resp;
                        }
                    }
                    else
                    {
                        throw new ArgumentException("Helper could not calculate tenant Id based on provided policy. Most likely, missing policy fragment.");
                    }
                }
            }
            else
            {
                return resp;
            }
        }

        /// <summary>
        /// Get the Tenant Id.
        /// </summary>
        /// <param name="tenantIdPolicyFragment">Tenant ID Policy Fragment.</param>
        /// <param name="objectWithTenantIds">Object to set Tenant Id.</param>
        /// <returns>List of Tenant ID Values.</returns>
        public List<long> GetTenantId(dynamic tenantIdPolicyFragment, object objectWithTenantIds)
        {
            if (tenantIdPolicyFragment == null)
            {
                return null;
            }

            if (objectWithTenantIds == null)
            {
                throw new ArgumentNullException(nameof(objectWithTenantIds));
            }

            ValidateClientIdPolicyFragment(tenantIdPolicyFragment);
            if (tenantIdPolicyFragment is JValue && tenantIdPolicyFragment.Type == JTokenType.Integer)
            {
                if ((int)tenantIdPolicyFragment == -3)
                {
                    return new List<long>() { _policyContext.GetUserContext().TenantId };
                }
                else
                {
                    return new List<long>() { (int)tenantIdPolicyFragment };
                }
            }
            else
            {
                if (tenantIdPolicyFragment.useContextField != null)
                {
                    ProcessIfElseCtxDataResponse processIfElseCtxDataResponse = ProcessContextOrSourceField(tenantIdPolicyFragment.useContextField, objectWithTenantIds, true);
                    if (processIfElseCtxDataResponse.UseReturnValue)
                    {
                        return processIfElseCtxDataResponse.ReturnVal;
                    }
                    else
                    {
                        if (processIfElseCtxDataResponse.ProcessElse)
                        {
                            return GetTenantId(tenantIdPolicyFragment.useContextField.Else, objectWithTenantIds);
                        }
                        else
                        {
                            if (processIfElseCtxDataResponse.ProcessIf)
                            {
                                return GetTenantId(tenantIdPolicyFragment.useContextField.IfPresent, objectWithTenantIds);
                            }
                            else
                            {
                                throw new InvalidOperationException("Invalid execution flow");
                            }
                        }
                    }
                }
                else
                {
                    if (tenantIdPolicyFragment.useSourceField != null)
                    {
                        ProcessIfElseCtxDataResponse processIfElseCtxDataResponse = ProcessContextOrSourceField(tenantIdPolicyFragment.useSourceField, objectWithTenantIds, false);

                        if (processIfElseCtxDataResponse.UseReturnValue)
                        {
                            return processIfElseCtxDataResponse.ReturnVal;
                        }
                        else
                        {
                            if (processIfElseCtxDataResponse.ProcessElse)
                            {
                                return GetTenantId(tenantIdPolicyFragment.useSourceField.Else, objectWithTenantIds);
                            }
                            else
                            {
                                if (processIfElseCtxDataResponse.ProcessIf)
                                {
                                    return GetTenantId(tenantIdPolicyFragment.useSourceField.IfPresent, objectWithTenantIds);
                                }
                                else
                                {
                                    throw new InvalidOperationException("Invalid execution flow");
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (_policyContext.GetUserContext().TenantType.ToLowerInvariant())
                        {
                            case "subclient":
                            case "client":
                                {
                                    dynamic fragment;
                                    if (IsInsertPolicy)
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByClient;
                                    }
                                    else
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByClient;
                                    }

                                    if (fragment != null)
                                    {
                                        return GetTenantId(tenantIdPolicyFragment.ifCreatedByClient, objectWithTenantIds);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Not able to identify tenant, ifCreatedByClient section of the policy not specified.");
                                    }
                                }

                            case "masterclient":
                                {
                                    dynamic fragment;
                                    if (IsInsertPolicy)
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByClient;
                                    }
                                    else
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByClient;
                                    }

                                    if (fragment != null)
                                    {
                                        return GetTenantId(tenantIdPolicyFragment.ifCreatedByClient, objectWithTenantIds);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Not able to identify tenant, ifCreatedByClient section of the policy not specified.");
                                    }
                                }

                            case "vendor":
                                {
                                    dynamic fragment;
                                    if (IsInsertPolicy)
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByVendor;
                                    }
                                    else
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedBySubContractor;
                                    }

                                    if (fragment != null)
                                    {
                                        return GetTenantId(fragment, objectWithTenantIds);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Not able to identify tenant, ifCreatedByVendor section of the policy not specified.");
                                    }
                                }

                            case "subcontractor":
                                {
                                    dynamic fragment;
                                    if (IsInsertPolicy)
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedBySubContractor;
                                    }
                                    else
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedBySubContractor;
                                    }

                                    if (fragment != null)
                                    {
                                        return GetTenantId(fragment, objectWithTenantIds);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Not able to identify tenant, ifCreatedBySubcontractor section of the policy not specified.");
                                    }
                                }

                            case "servicer":
                                {
                                    dynamic fragment;
                                    if (IsInsertPolicy)
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByServicer;
                                    }
                                    else
                                    {
                                        fragment = tenantIdPolicyFragment.ifCreatedByServicer;
                                    }

                                    if (fragment != null)
                                    {
                                        return GetTenantId(fragment, objectWithTenantIds);
                                    }
                                    else
                                    {
                                        throw new ArgumentException("Not able to identify tenant, ifCreatedByServicer section of the policy not specified.");
                                    }
                                }

                            default: throw new ArgumentException("Invalid user type");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate the Tenant Id Value.
        /// </summary>
        /// <param name="tenantIdPolicyFragmentDynamic">Policy Fragment.</param>
        /// <param name="objectWithTenantIds">Object to set the Tenant Id.</param>
        /// <param name="valueToSet">Value to Set.</param>
        /// <param name="useReturnValue">Use return value.</param>
        /// <returns>Tenant Id.</returns>
        private int GetTenantIdFromOverrideOrDefaults(dynamic tenantIdPolicyFragmentDynamic, object objectWithTenantIds, object valueToSet, out bool useReturnValue)
        {
            useReturnValue = false;

            if (valueToSet == null)
            {
                if (tenantIdPolicyFragmentDynamic.ifSourceFieldIsNull != null)
                {
                    dynamic ifSourceFieldIsNullFrag = tenantIdPolicyFragmentDynamic.ifSourceFieldIsNull;
                    if (ifSourceFieldIsNullFrag is JValue && tenantIdPolicyFragmentDynamic.Type == JTokenType.Integer)
                    {
                        useReturnValue = true;
                        return (int)ifSourceFieldIsNullFrag;
                    }
                    else
                    {
                        if (ifSourceFieldIsNullFrag.useSourceField != null)
                        {
                            var value = ReflectionHelper.GetPropValue(objectWithTenantIds, (string)ifSourceFieldIsNullFrag.useSourceField);

                            if (value != null && value.GetType() == typeof(int))
                            {
                                useReturnValue = true;
                                return (int)value;
                            }
                            else
                            {
                                useReturnValue = false;
                                return 0;
                            }
                        }
                        else
                        {
                            useReturnValue = false;
                            return 0;
                        }
                    }
                }
                else
                {
                    useReturnValue = false;
                    return 0;
                }
            }
            else
            {
                if (tenantIdPolicyFragmentDynamic.ifSourceFieldIsNotNull != null)
                {
                    dynamic ifSourceFieldIsNotNullFrag = tenantIdPolicyFragmentDynamic.ifSourceFieldIsNotNull; // TenantIdPolicyFragmentJToken["ifSourceFieldIsNotNull"];

                    if (ifSourceFieldIsNotNullFrag is JValue && tenantIdPolicyFragmentDynamic.Type == JTokenType.Integer)
                    {
                        useReturnValue = true;
                        return (int)ifSourceFieldIsNotNullFrag;
                    }
                    else
                    {
                        if (ifSourceFieldIsNotNullFrag.useSourceField != null)
                        {
                            var value = ReflectionHelper.GetPropValue(objectWithTenantIds, (string)ifSourceFieldIsNotNullFrag.useSourceField);

                            if (value != null && value.GetType() == typeof(int))
                            {
                                useReturnValue = true;
                                return (int)value;
                            }
                            else
                            {
                                useReturnValue = false;
                                return 0;
                            }
                        }
                        else
                        {
                            useReturnValue = false;
                            return 0;
                        }
                    }
                }
                else
                {
                    useReturnValue = false;
                    return 0;
                }
            }
        }

        /// <summary>
        /// Get a List of long values from the field.
        /// </summary>
        /// <param name="sourceFieldValue">Field.</param>
        /// <returns>List of long values.</returns>
        private List<long> GetValue(object sourceFieldValue)
        {
            if (sourceFieldValue != null && (long.TryParse(sourceFieldValue.ToString(), out long outVal) ||
                (sourceFieldValue is IEnumerable<long> && (sourceFieldValue as List<long>).Count > 0)))
            {
                if (sourceFieldValue is IEnumerable<long>)
                {
                    return sourceFieldValue as List<long>;
                }
                else
                {
                    return new List<long>() { outVal };
                }
            }

            return null;
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA1822 // Mark members as static
#pragma warning restore CA1502 // Avoid excessive complexity