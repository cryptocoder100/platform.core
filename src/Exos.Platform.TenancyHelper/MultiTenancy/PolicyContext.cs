namespace Exos.Platform.TenancyHelper.MultiTenancy
{
    using System.Collections.Generic;
    using Exos.Platform.TenancyHelper.Interfaces;

    /// <inheritdoc/>
    public class PolicyContext : IPolicyContext
    {
        private readonly IUserContextService _userContextService;
        private IUserContext _userContext;
        private Dictionary<string, long> _customKeyValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="PolicyContext"/> class.
        /// </summary>
        /// <param name="userContextService">User Context Service.</param>
        public PolicyContext(IUserContextService userContextService)
        {
            _userContextService = userContextService;
            _customKeyValue = new Dictionary<string, long>();
        }

        /// <inheritdoc/>
        public string PolicyDocName { get; set; }

        /// <inheritdoc/>
        public string PolicyDoc { get; set; }

        /// <inheritdoc/>
        public void SetUserContext(IUserContext context)
        {
            _userContext = context;
        }

        /// <inheritdoc/>
        public IUserContext GetUserContext()
        {
            return _userContextService.GetUserContext();
        }

        /// <inheritdoc/>
        public void SetCustomIntField(string fieldName, long fieldValue)
        {
            // if keys does not exists, add it.
            if (!_customKeyValue.ContainsKey(fieldName))
            {
                _customKeyValue.Add(fieldName, fieldValue);
            }
            else
            {
                _customKeyValue[fieldName] = fieldValue;
            }
        }

        /// <inheritdoc/>
        public long GetCustomIntField(string fieldName)
        {
            return _customKeyValue[fieldName];
        }

        /// <inheritdoc/>
        public bool ContainsCustomField(string fieldName)
        {
            return _customKeyValue.ContainsKey(fieldName);
        }

        /// <inheritdoc/>
        public bool IsCustomFieldNullOrEmpty(string fieldName)
        {
            bool retVal = false;
            if (!_customKeyValue.ContainsKey(fieldName))
            {
                retVal = true;
            }

            return retVal;
        }
    }
}
