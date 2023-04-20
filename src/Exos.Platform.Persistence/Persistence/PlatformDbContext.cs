#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1309 // Use ordinal StringComparison
#pragma warning disable CA2201 // Do not raise reserved exception types
#pragma warning disable CA1502 // Avoid excessive complexity

namespace Exos.Platform.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Exos.Platform.AspNetCore.Encryption;
    using Exos.Platform.AspNetCore.Middleware;
    using Exos.Platform.Persistence.Encryption;
    using Exos.Platform.Persistence.Entities;
    using Exos.Platform.Persistence.EventTracking;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.ChangeTracking;
    using Microsoft.EntityFrameworkCore.Query;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Nito.AsyncEx;

    /// <inheritdoc/>
    public class PlatformDbContext : DbContext
    {
        private readonly IPolicyHelper _policyHelper;
        private readonly ILogger<PlatformDbContext> _logger;
        private readonly IPolicyContext _policyContext;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly IDatabaseEncryption _databaseEncryption;
        private readonly IDatabaseHashing _databaseHashing;
        private TenantValue _tenant;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformDbContext"/> class.
        /// </summary>
        /// <param name="options">DbContextOptions.</param>
        /// <param name="userHttpContextAccessorService">IUserHttpContextAccessorService.</param>
        /// <param name="logger">ILogger.</param>
        /// <param name="policyHelper">IPolicyHelper.</param>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="serviceProvider">IServiceProvider.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption" />.</param>
        /// <param name="databaseHashing">The databaseHashing<see cref="IDatabaseHashing"/>.</param>
        public PlatformDbContext(
            DbContextOptions options,
            IUserHttpContextAccessorService userHttpContextAccessorService,
            ILogger<PlatformDbContext> logger,
            IPolicyHelper policyHelper = null,
            IPolicyContext policyContext = null,
            IServiceProvider serviceProvider = null,
            IDatabaseEncryption databaseEncryption = null,
            IDatabaseHashing databaseHashing = null)
            : base(options)
        {
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _policyHelper = policyHelper;
            _policyContext = policyContext;
            _serviceProvider = serviceProvider;
            PopulateTenantValues();
            _databaseEncryption = databaseEncryption;
            _databaseHashing = databaseHashing;

            // Breaking change in EF Core 3.1, Cascade deletions now happen immediately by default
            // Below 2 lines restore previous behaivour
            // https://docs.microsoft.com/en-us/ef/core/what-is-new/ef-core-3.x/breaking-changes#cascade
            ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
        }

        /// <summary>
        /// Asynchronously saves all changes made in this context to the database.
        /// </summary>
        /// <param name="policyContext"><see cref="IPolicyContext"/>.</param>
        /// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
        /// <returns>A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.</returns>
        public async Task<int> SaveChangesAsync(IPolicyContext policyContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (_policyHelper != null)
            {
                int updatedEntities = await UpdateTenancyFields(policyContext, cancellationToken).ConfigureAwait(false);
            }

            // Encrypt Fields
            IEnumerable<EntityEntry> entityEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            HashEntities(entityEntries);
            EncryptEntities(entityEntries);

            // Update audit fields
            UpdateAuditFields();
            var savedEntities = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            // if (savedEntities > 0)
            // {
            //    entityEntries = ChangeTracker.Entries();
            //    DecryptEntities(entityEntries);
            // }

            return savedEntities;
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (_policyHelper != null)
            {
                int updatedEntities = await UpdateTenancyFields(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Encrypt fields
            IEnumerable<EntityEntry> entityEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            HashEntities(entityEntries);
            EncryptEntities(entityEntries);

            // Update audit fields
            UpdateAuditFields();

            int savedEntities;

            try
            {
                savedEntities = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException error)
            {
                throw new ConflictException("Error at DbUpdateConcurrency", error);
            }

            // if (savedEntities > 0)
            // {
            //    entityEntries = ChangeTracker.Entries();
            //    DecryptEntities(entityEntries);
            // }

            return savedEntities;
        }

        /// <inheritdoc/>
        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            var eventTrackingService = _serviceProvider?.GetService<IEventTrackingService>();
            if (eventTrackingService != null)
            {
                string clientKeyIdentifier = _userHttpContextAccessorService.GetClientKeyIdentifier();
                if (!string.IsNullOrEmpty(clientKeyIdentifier))
                {
                    eventTrackingService.AddMetadata(EncryptionConstants.EncryptionRequestHeader, clientKeyIdentifier);
                }

                var eventTrackingList = eventTrackingService.CreateEventTracking(this);
                var result = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
                await eventTrackingService.UpdateEventTrackingAsync(eventTrackingList, this).ConfigureAwait(false);
                return result;
            }
            else
            {
                return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc/>
        public override int SaveChanges()
        {
            if (_policyHelper != null)
            {
                int updatedEntities = AsyncContext.Run<int>(async () => await UpdateTenancyFields().ConfigureAwait(false));
            }

            // Encrypt fields
            IEnumerable<EntityEntry> entityEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
            HashEntities(entityEntries);
            EncryptEntities(entityEntries);

            // Update audit fields
            UpdateAuditFields();

            int savedEntities;

            try
            {
                savedEntities = base.SaveChanges();
            }
            catch (DbUpdateConcurrencyException error)
            {
                throw new ConflictException("Error at DbUpdateConcurrency", error);
            }

            // if (savedEntities > 0)
            // {
            //    entityEntries = ChangeTracker.Entries();
            //    DecryptEntities(entityEntries);
            // }

            return savedEntities;
        }

        /// <inheritdoc/>
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var eventTrackingService = _serviceProvider?.GetService<IEventTrackingService>();
            if (eventTrackingService != null)
            {
                var eventTrackingList = eventTrackingService.CreateEventTracking(this);
                var result = base.SaveChanges(acceptAllChangesOnSuccess);
                eventTrackingService.UpdateEventTracking(eventTrackingList, this);
                return result;
            }
            else
            {
                return base.SaveChanges(acceptAllChangesOnSuccess);
            }
        }

        /// <summary>
        /// Get Current User.
        /// </summary>
        /// <returns>Current User.</returns>
        public string GetCurrentUser()
        {
            return _userHttpContextAccessorService.GetCurrentUser();
        }

        /// <summary>
        /// Get Tenant Id.
        /// </summary>
        /// <returns>Tenant Id.</returns>
        public long GetTenantId()
        {
            return _userHttpContextAccessorService.GetTenantId();
        }

        /// <summary>
        /// Get Tenant Type.
        /// </summary>
        /// <returns>Tenant Type.</returns>
        public string GetTenantType()
        {
            return _userHttpContextAccessorService.GetTenantType();
        }

        /// <summary>
        /// Get a list of Servicer Groups.
        /// </summary>
        /// <returns>List of Servicer Groups.</returns>
        public List<long> GetServicerGroups()
        {
            return _userHttpContextAccessorService.GetServicerGroups();
        }

        /// <summary>
        /// Decrypt an Object.
        /// Will decrypt only if document is not attached to EF ChangeTracker.
        /// </summary>
        /// <typeparam name="T">Object Type.</typeparam>
        /// <param name="toDecrypt">Object to decrypt.</param>
        /// <returns>Decrypted Object.</returns>
        public T DecryptEntity<T>(T toDecrypt)
        {
            if (_databaseEncryption != null)
            {
                // If object is on change tracker throw exception
                var entityEntries = ChangeTracker.Entries().Where(entity => entity.Entity.GetType() == toDecrypt.GetType());
                foreach (EntityEntry entityEntry in entityEntries.ToList())
                {
                    if (ReferenceEquals(entityEntry.Entity, toDecrypt))
                    {
                        if (entityEntry.State != EntityState.Detached)
                        {
                            throw new ExosPersistenceException("Decryption of Entity Framework objects is not allowed, object still attached to dbcontext.");
                        }
                    }
                }

                _databaseEncryption.DecryptEntityFrameworkObject<T>(toDecrypt);
            }

            return toDecrypt;
        }

        /// <summary>
        /// Decrypt an Enumerable Object.
        /// Will decrypt only if document is not attached to EF ChangeTracker.
        /// </summary>
        /// <typeparam name="T">Object Type to encrypt.</typeparam>
        /// <param name="toDecrypt">Object to decrypt <see cref="IEnumerable{T}"/>.</param>
        /// <returns>The <see cref="IEnumerable{T}"/> decrypted.</returns>
        public IEnumerable<T> DecryptEntities<T>(IEnumerable<T> toDecrypt)
        {
            if (_databaseEncryption != null)
            {
                if (toDecrypt != null && toDecrypt.Any())
                {
                    foreach (var result in toDecrypt)
                    {
                        DecryptEntity<T>(result);
                    }
                }
            }

            return toDecrypt;
        }

        /// <inheritdoc/>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            if (modelBuilder == null)
            {
                throw new ArgumentNullException(nameof(modelBuilder));
            }

            // Add the tenancy filter and IsActive filter
            var auditAndTenantEntities = modelBuilder.Model.GetEntityTypes().Where(e => typeof(IAuditable).IsAssignableFrom(e.ClrType) && typeof(TenancyHelper.Entities.ITenant).IsAssignableFrom(e.ClrType)).ToList();
            if (auditAndTenantEntities.Any() && _policyContext != null)
            {
                Expression<Func<TenancyHelper.Entities.ITenant, bool>> tenancyFilter = GetTenancyFilter();
                if (tenancyFilter != null)
                {
                    auditAndTenantEntities.ForEach(t =>
                    {
                        LambdaExpression exp = ConvertFilterExpression(tenancyFilter, t.ClrType);
                        modelBuilder.Entity(t.ClrType).HasQueryFilter(exp);
                    });
                }
            }
            else
            {
                // Add the isActive filter
                var auditEntities = modelBuilder.Model.GetEntityTypes().Where(e => typeof(IAuditable).IsAssignableFrom(e.ClrType)).ToList();
                if (auditEntities.Any())
                {
                    auditEntities.ForEach(t => { modelBuilder.Entity(t.ClrType).HasQueryFilter(ConvertFilterExpression<IAuditable>(f => f.IsActive, t.ClrType)); });
                }
            }

            base.OnModelCreating(modelBuilder);
        }

        /// <summary>
        /// Create a Lambda Expression for Tenancy Filter.
        /// </summary>
        /// <typeparam name="TInterface">TInterface.</typeparam>
        /// <param name="filterExpression">Filter Expression.</param>
        /// <param name="entityType">Entity Type.</param>
        /// <returns>LambdaExpression for Tenancy Filter.</returns>
        private static LambdaExpression ConvertFilterExpression<TInterface>(Expression<Func<TInterface, bool>> filterExpression, Type entityType)
        {
            var newParam = Expression.Parameter(entityType);
            var newBody = ReplacingExpressionVisitor.Replace(filterExpression.Parameters.Single(), newParam, filterExpression.Body);
            return Expression.Lambda(newBody, newParam);
        }

        /// <summary>
        /// Apply tenancy filter to current user.
        /// </summary>
        private void PopulateTenantValues()
        {
            _tenant = new TenantValue
            {
                UserName = GetCurrentUser(),
                TenantType = GetTenantType(),
            };
            if (!string.IsNullOrEmpty(_tenant.TenantType))
            {
                _tenant.TenantType = _tenant.TenantType.ToLowerInvariant();
            }

            _tenant.TenantId = (int)GetTenantId();
            if (string.Compare(_tenant.TenantType, "servicer", true, CultureInfo.InvariantCulture) == 0)
            {
                _tenant.ServicerTenantId = (short)_tenant.TenantId;
            }
            else
            {
                _tenant.ServicerTenantId = -1;
            }

            var masterClientIds = GetMasterClients();
            _tenant.MasterClientIds = masterClientIds != null ? masterClientIds.Select(c => (int)c).ToList() : new List<int>();
            var subClientIds = GetSubClients();
            _tenant.SubClientIds = subClientIds != null ? subClientIds.Select(c => (int)c).ToList() : new List<int>();
            var vendorIds = GetVendors();
            _tenant.VendorIds = vendorIds != null ? vendorIds.Select(v => (int)v).ToList() : new List<int>();
            var subVendorIds = GetSubVendors();
            _tenant.SubcontractorIds = subVendorIds != null ? subVendorIds.Select(s => (int)s).ToList() : new List<int>();
            var servicerGroupIds = GetServicerGroups();
            _tenant.ServicerGroupIds = servicerGroupIds != null ? servicerGroupIds.Select(g => (short)g).ToList() : new List<short>();
        }

        /// <summary>
        /// Update audit fields before saving the entity.
        /// </summary>
        private void UpdateAuditFields()
        {
            string currentUser = GetCurrentUser();
            if (currentUser == null)
            {
                currentUser = "username";
            }

            foreach (var auditableEntityEntry in ChangeTracker.Entries<IAuditable>())
            {
                if (auditableEntityEntry.State == EntityState.Added ||
                    auditableEntityEntry.State == EntityState.Modified ||
                    auditableEntityEntry.State == EntityState.Deleted)
                {
                    if (auditableEntityEntry.State == EntityState.Added)
                    {
                        DateTime createdDate = DateTime.UtcNow;
                        auditableEntityEntry.Entity.IsActive = true;
                        auditableEntityEntry.Entity.CreatedDate = createdDate;
                        auditableEntityEntry.Entity.CreatedBy = currentUser;
                        auditableEntityEntry.Entity.LastUpdatedDate = createdDate;
                        auditableEntityEntry.Entity.LastUpdatedBy = currentUser;
                    }

                    if (auditableEntityEntry.State == EntityState.Modified || auditableEntityEntry.State == EntityState.Deleted)
                    {
                        // Update the original version column value to the one passed from UI.
                        var versionProperty = auditableEntityEntry.Properties.Where(p => p.Metadata.Name == "Version").First();
                        versionProperty.OriginalValue = versionProperty.CurrentValue;

                        // Modify updated date and updated by fields
                        auditableEntityEntry.Entity.LastUpdatedDate = DateTime.UtcNow;
                        auditableEntityEntry.Entity.LastUpdatedBy = currentUser;

                        // Avoid the modification of the IsActive field,
                        // and created date and created by fields
                        if (auditableEntityEntry.State == EntityState.Modified)
                        {
                            auditableEntityEntry.Property(p => p.IsActive).IsModified = false;
                            auditableEntityEntry.Property(p => p.CreatedDate).IsModified = false;
                            auditableEntityEntry.Property(p => p.CreatedBy).IsModified = false;
                        }

                        // Implement soft Delete when the Remove method is called,
                        // also update the IsActive Flag to false,
                        // and the state to Modified.
                        if (auditableEntityEntry.State == EntityState.Deleted)
                        {
                            auditableEntityEntry.Entity.IsActive = false;
                            auditableEntityEntry.State = EntityState.Modified;
                            auditableEntityEntry.Property(p => p.CreatedDate).IsModified = false;
                            auditableEntityEntry.Property(p => p.CreatedBy).IsModified = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update Tenancy fields.
        /// </summary>
        /// <param name="policyContext">IPolicyContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>Number of rows updated.</returns>
        private async Task<int> UpdateTenancyFields(IPolicyContext policyContext = null, CancellationToken cancellationToken = default)
        {
            int updatedEntities = 0;

            // Update tenancy fields calling Tenancy Helper
            foreach (var tenancyEntityEntry in ChangeTracker.Entries<TenancyHelper.Entities.ITenant>())
            {
                if (tenancyEntityEntry.State == EntityState.Added)
                {
                    if (policyContext == null)
                    {
                        await _policyHelper.SetTenantIdsForInsert(tenancyEntityEntry.Entity, _policyContext).ConfigureAwait(false);
                    }
                    else
                    {
                        await _policyHelper.SetTenantIdsForInsert(tenancyEntityEntry.Entity, policyContext).ConfigureAwait(false);
                    }

                    updatedEntities++;
                }
                else if (tenancyEntityEntry.State == EntityState.Modified)
                {
                    if (policyContext == null)
                    {
                        await _policyHelper.SetTenantIdsForUpdate(tenancyEntityEntry.Entity, _policyContext).ConfigureAwait(false);
                    }
                    else
                    {
                        await _policyHelper.SetTenantIdsForUpdate(tenancyEntityEntry.Entity, policyContext).ConfigureAwait(false);
                    }

                    updatedEntities++;
                }
            }

            return updatedEntities;
        }

        /// <summary>
        /// Get the tenancy filter by user.
        /// </summary>
        /// <param name="userName">The userName<see cref="string"/>.</param>
        /// <param name="tenantType">The tenantType<see cref="string"/>.</param>
        /// <param name="userTenantId">The userTenantId<see cref="long"/>.</param>
        /// <returns>Tenancy filter.</returns>
        private Expression<Func<TenancyHelper.Entities.ITenant, bool>> GetTenancyFilter(string userName, string tenantType, long userTenantId)
        {
            Expression<Func<TenancyHelper.Entities.ITenant, bool>> tenancyFilter = null;
            List<int> fullAccessTenantIds = new List<int>() { -1, -2 };
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(tenantType))
            {
                switch (tenantType.ToLowerInvariant())
                {
                    case "masterclient":
                        {
                            short tenantId = Convert.ToInt16(userTenantId);
                            List<long> subClientIds = GetSubClients();
                            if (subClientIds != null && subClientIds.Any())
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      (te.ClientTenantId == tenantId ||
                                                       (fullAccessTenantIds.Contains(te.ClientTenantId) && (fullAccessTenantIds.Contains(te.SubClientTenantId) || subClientIds.Contains(te.SubClientTenantId))));
                            }
                            else
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.ClientTenantId) && fullAccessTenantIds.Contains(te.SubClientTenantId)) || (te.ClientTenantId == tenantId));
                            }

                            break;
                        }

                    case "subclient":
                        {
                            int tenantId = Convert.ToInt32(userTenantId);
                            List<long> masterClientIds = GetMasterClients();
                            if (masterClientIds != null && masterClientIds.Any())
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      (te.SubClientTenantId == tenantId ||
                                                       (fullAccessTenantIds.Contains(te.SubClientTenantId) && (fullAccessTenantIds.Contains(te.ClientTenantId) || masterClientIds.Contains(te.ClientTenantId))));
                            }
                            else
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.SubClientTenantId) && fullAccessTenantIds.Contains(te.ClientTenantId)) || (te.SubClientTenantId == tenantId));
                            }

                            break;
                        }

                    case "vendor":
                        {
                            int tenantId = Convert.ToInt32(userTenantId);
                            List<long> subVendorIds = GetSubVendors();
                            if (subVendorIds != null && subVendorIds.Any())
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      (te.VendorTenantId == tenantId ||
                                                       (fullAccessTenantIds.Contains(te.VendorTenantId) && (fullAccessTenantIds.Contains(te.SubContractorTenantId) || subVendorIds.Contains(te.SubContractorTenantId))));
                            }
                            else
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.VendorTenantId) && fullAccessTenantIds.Contains(te.SubContractorTenantId)) || (te.VendorTenantId == tenantId));
                            }

                            break;
                        }

                    case "subcontractor":
                        {
                            int tenantId = Convert.ToInt32(userTenantId);
                            List<long> vendorIds = GetVendors();
                            if (vendorIds != null && vendorIds.Any())
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      (te.SubContractorTenantId == tenantId ||
                                                       (fullAccessTenantIds.Contains(te.SubContractorTenantId) && (fullAccessTenantIds.Contains(te.VendorTenantId) || vendorIds.Contains(te.VendorTenantId))));
                            }
                            else
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.SubContractorTenantId) && fullAccessTenantIds.Contains(te.VendorTenantId)) || (te.SubContractorTenantId == tenantId));
                            }

                            break;
                        }

                    case "servicer":
                        {
                            short tenantId = Convert.ToInt16(userTenantId);
                            List<long> servicerGroups = GetServicerGroups();
                            if (servicerGroups != null && servicerGroups.Any())
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.ServicerTenantId) && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)) ||
                                                       (te.ServicerTenantId == tenantId && (fullAccessTenantIds.Contains(te.ServicerGroupTenantId) || servicerGroups.Contains(te.ServicerGroupTenantId))) ||
                                                       (fullAccessTenantIds.Contains(te.ServicerTenantId) && servicerGroups.Contains(te.ServicerGroupTenantId)));
                            }
                            else
                            {
                                tenancyFilter = te => EF.Property<bool>(te, "IsActive") &&
                                                      ((fullAccessTenantIds.Contains(te.ServicerTenantId) && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)) ||
                                                       (te.ServicerTenantId == tenantId && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)));
                            }

                            break;
                        }

                    default:
                        {
                            throw new Exception("Invalid user type");
                        }
                }
            }

            return tenancyFilter;
        }

        /// <summary>
        /// Get the tenancy filter by user.
        /// </summary>
        /// <returns>Tenancy Filter.</returns>
        private Expression<Func<TenancyHelper.Entities.ITenant, bool>> GetTenancyFilter()
        {
            Expression<Func<TenancyHelper.Entities.ITenant, bool>> tenancyFilter = null;
            List<int> fullAccessTenantIds = new List<int>() { -1, -2 };

            tenancyFilter = te => (
                                      !string.IsNullOrEmpty(_tenant.UserName) &&
                                      !string.IsNullOrEmpty(_tenant.TenantType) &&
                                      EF.Property<bool>(te, "IsActive") &&
                                      (
                                          (_tenant.TenantType == "masterclient" && _tenant.SubClientIds.Count > 0 && (te.ClientTenantId == _tenant.TenantId || (fullAccessTenantIds.Contains(te.ClientTenantId) && fullAccessTenantIds.Contains(te.SubClientTenantId)) || _tenant.SubClientIds.Contains(te.SubClientTenantId))) ||
                                          (_tenant.TenantType == "masterclient" && _tenant.SubClientIds.Count == 0 && ((fullAccessTenantIds.Contains(te.ClientTenantId) && fullAccessTenantIds.Contains(te.SubClientTenantId)) || (te.ClientTenantId == _tenant.TenantId))) ||
                                          (_tenant.TenantType == "subclient" && _tenant.MasterClientIds.Count > 0 && (te.SubClientTenantId == _tenant.TenantId || (fullAccessTenantIds.Contains(te.SubClientTenantId) && fullAccessTenantIds.Contains(te.ClientTenantId)) || _tenant.MasterClientIds.Contains(te.ClientTenantId))) ||
                                          (_tenant.TenantType == "subclient" && _tenant.MasterClientIds.Count == 0 && ((fullAccessTenantIds.Contains(te.SubClientTenantId) && fullAccessTenantIds.Contains(te.ClientTenantId)) || (te.SubClientTenantId == _tenant.TenantId))) ||
                                          (_tenant.TenantType == "vendor" && _tenant.SubcontractorIds.Count > 0 && (te.VendorTenantId == _tenant.TenantId || (fullAccessTenantIds.Contains(te.VendorTenantId) && (fullAccessTenantIds.Contains(te.SubContractorTenantId) || _tenant.SubcontractorIds.Contains(te.SubContractorTenantId))))) ||
                                          (_tenant.TenantType == "vendor" && _tenant.SubcontractorIds.Count == 0 && ((fullAccessTenantIds.Contains(te.VendorTenantId) && fullAccessTenantIds.Contains(te.SubContractorTenantId)) || (te.VendorTenantId == _tenant.TenantId))) ||
                                          (_tenant.TenantType == "subcontractor" && _tenant.VendorIds.Count > 0 && (te.SubContractorTenantId == _tenant.TenantId || (fullAccessTenantIds.Contains(te.SubContractorTenantId) && (fullAccessTenantIds.Contains(te.VendorTenantId) || _tenant.VendorIds.Contains(te.VendorTenantId))))) ||
                                          (_tenant.TenantType == "subcontractor" && _tenant.VendorIds.Count == 0 && ((fullAccessTenantIds.Contains(te.SubContractorTenantId) && fullAccessTenantIds.Contains(te.VendorTenantId)) || (te.SubContractorTenantId == _tenant.TenantId))) ||
                                          (_tenant.TenantType == "servicer" && _tenant.ServicerGroupIds.Count > 0 && ((fullAccessTenantIds.Contains(te.ServicerTenantId) && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)) || (te.ServicerTenantId == _tenant.ServicerTenantId && (fullAccessTenantIds.Contains(te.ServicerGroupTenantId) || _tenant.ServicerGroupIds.Contains(te.ServicerGroupTenantId))) || (fullAccessTenantIds.Contains(te.ServicerTenantId) && _tenant.ServicerGroupIds.Contains(te.ServicerGroupTenantId)))) ||
                                          (_tenant.TenantType == "servicer" && _tenant.ServicerGroupIds.Count == 0 && ((fullAccessTenantIds.Contains(te.ServicerTenantId) && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)) || (te.ServicerTenantId == _tenant.ServicerTenantId && fullAccessTenantIds.Contains(te.ServicerGroupTenantId)))))) ||
                                  (
                                      string.IsNullOrEmpty(_tenant.UserName) &&
                                      string.IsNullOrEmpty(_tenant.TenantType) &&
                                      EF.Property<bool>(te, "IsActive"));

            return tenancyFilter;
        }

        /// <summary>
        /// Get a List of Vendors.
        /// </summary>
        /// <returns>List of Vendors.</returns>
        private List<long> GetVendors()
        {
            return _userHttpContextAccessorService.GetVendors();
        }

        /// <summary>
        /// Get a List of SubVendors.
        /// </summary>
        /// <returns>List of SubVendors.</returns>
        private List<long> GetSubVendors()
        {
            return _userHttpContextAccessorService.GetSubVendors();
        }

        /// <summary>
        /// Get a List of MasterClients.
        /// </summary>
        /// <returns>List of MasterClients.</returns>
        private List<long> GetMasterClients()
        {
            return _userHttpContextAccessorService.GetMasterClients();
        }

        /// <summary>
        /// Get a List of SubClients.
        /// </summary>
        /// <returns>List of SubClients.</returns>
        private List<long> GetSubClients()
        {
            return _userHttpContextAccessorService.GetSubClients();
        }

        /// <summary>
        /// Get Servicer Group Tenant Id.
        /// </summary>
        /// <returns>Servicer Group Tenant Id.</returns>
        private long GetServicerGroupTenantId()
        {
            var result = _userHttpContextAccessorService.GetUserContext();
            return result.ServicerGroupTenantId;
        }

        private void EncryptEntities(IEnumerable<EntityEntry> entityEntries)
        {
            if (_databaseEncryption != null)
            {
                EncryptionKey currentEncrytionKey = null;

                // Encrypt Entities.
                foreach (EntityEntry entityEntry in entityEntries.ToList())
                {
                    // Check if data encrypted match the current encryption key,
                    // In updates it's posible that SL encrypted data is encrypted with boa key by the update call.
                    // in an update data needs to be encrypted with the same key that is created.
                    if (entityEntry.State == EntityState.Modified)
                    {
                        // Get all the properties that are encryptable.
                        IEnumerable<PropertyInfo> encryptedProperties = entityEntry.Entity
                            .GetType()
                            .GetProperties()
                            .Where(p => p.GetCustomAttributes(typeof(EncryptedAttribute), true)
                            .Any(a => p.PropertyType == typeof(string)));

                        var originalValues = entityEntry.OriginalValues;
                        EncryptedFieldHeaderParser headerParser;
                        foreach (PropertyInfo encryptedPropertyInfo in encryptedProperties.ToList())
                        {
                            var originalValue = originalValues.GetValue<string>(encryptedPropertyInfo.Name);

                            if (!string.IsNullOrEmpty(originalValue))
                            {
                                headerParser = new EncryptedFieldHeaderParser(originalValue);
                                if (headerParser.IsEncrypted)
                                {
                                    var encryptionKey = headerParser.EncryptionKey;
                                    if (!_databaseEncryption.CurrentEncryptionKey.Equals(encryptionKey))
                                    {
                                        currentEncrytionKey = _databaseEncryption.CurrentEncryptionKey;
                                        _logger.LogInformation($"Current encryption key set is {_databaseEncryption.CurrentEncryptionKey.KeyName} but data is created with {encryptionKey.KeyName}, update transaction is encrypted using {encryptionKey.KeyName}.");
                                        _databaseEncryption.SetEncryptionKey(encryptionKey);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    _databaseEncryption.EncryptObject<object>(entityEntry.Entity);
                    if (currentEncrytionKey != null && currentEncrytionKey.IsKeyReadyToUse)
                    {
                        _databaseEncryption.SetEncryptionKey(currentEncrytionKey);
                    }
                }
            }
        }

        private void DecryptEntities(IEnumerable<EntityEntry> entityEntries)
        {
            if (_databaseEncryption != null)
            {
                // Decrypt entities
                foreach (EntityEntry entityEntry in entityEntries.ToList())
                {
                    _databaseEncryption.DecryptObject<object>(entityEntry.Entity);
                }
            }
        }

        private void HashEntities(IEnumerable<EntityEntry> entityEntries)
        {
            if (_databaseHashing != null)
            {
                foreach (EntityEntry entityEntry in entityEntries.ToList())
                {
                    // Find properties decorated with the HashAttribute
                    IEnumerable<PropertyInfo> hashedProperties = entityEntry.Entity.GetType().GetProperties()
                        .Where(p => p.GetCustomAttributes(typeof(HashAttribute), true).Any(a => p.PropertyType == typeof(string)));
                    foreach (PropertyInfo hashedPropertyInfo in hashedProperties.ToList())
                    {
                        HashAttribute hashAttribute = (HashAttribute)Attribute.GetCustomAttribute(hashedPropertyInfo, typeof(HashAttribute));
                        // Find the value to hash from the mapped column
                        PropertyInfo propertyInfoToHash = entityEntry.Entity.GetType().GetProperties()
                            .Single(p => p.Name == hashAttribute.ColumnToHash);
                        string columnToHashValue = propertyInfoToHash.GetValue(entityEntry.Entity) as string;
                        if (!string.IsNullOrEmpty(columnToHashValue))
                        {
                            // Hash the value and set the value to the property.
                            string hashedValue = _databaseHashing.HashStringToHex(columnToHashValue);
                            hashedPropertyInfo.SetValue(entityEntry.Entity, hashedValue);
                        }
                    }
                }
            }
        }
    }
}
#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA1309 // Use ordinal StringComparison
#pragma warning restore CA2201 // Do not raise reserved exception types
#pragma warning disable CA1502 // Avoid excessive complexity