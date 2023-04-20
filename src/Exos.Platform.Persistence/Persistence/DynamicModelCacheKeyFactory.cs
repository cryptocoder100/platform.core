namespace Exos.Platform.Persistence
{
    using System;
    using System.Collections.Generic;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Infrastructure;

    /// <inheritdoc/>
    public class DynamicModelCacheKeyFactory : IModelCacheKeyFactory
    {
        /// <inheritdoc/>
        public object Create(DbContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (context is PlatformDbContext platformDbContext)
            {
                string currentUser = platformDbContext.GetCurrentUser();
                if (currentUser != null)
                {
                    // Find TenantId to use it for key to cache the models, if TenantId doesn't exist IdentityExtensions.GetTenantId returns 0.
                    long tenantId = platformDbContext.GetTenantId();

                    // Find ServicerGroups to use it for key to cache the models, if ServicerGroups are not found returns null.
                    List<long> servicerGroupsIds = platformDbContext.GetServicerGroups();

                    string servicerGroups = servicerGroupsIds != null ? string.Join(',', servicerGroupsIds) : string.Empty;
                    string modelKey = currentUser + "," + tenantId + "," + servicerGroups;
                    return (context.GetType(), modelKey);
                }
            }

            return context.GetType();
        }
    }
}
