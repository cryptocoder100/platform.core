namespace Exos.Platform.Persistence.Tests.DbContext
{
    using System;
    using Exos.Platform.Persistence.Encryption;
    using Exos.Platform.Persistence.Tests.Entities;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Test DBContext.
    /// </summary>
    public class TestDbContextEncrypted : PlatformDbContext
    {
        private readonly ILogger<TestDbContextEncrypted> _logger;
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly IPolicyHelper _policyHelper;
        private readonly IPolicyContext _policyContext;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContextEncrypted"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="dbcontextOptions"><see cref="DbContextOptions"/>.</param>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="policyHelper"><see cref="IPolicyHelper"/>.</param>
        /// <param name="policyContext"><see cref="IPolicyContext"/>.</param>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        /// <param name="databaseEncryption"><see cref="IDatabaseEncryption"/>.</param>
        public TestDbContextEncrypted(
            ILogger<TestDbContextEncrypted> logger,
            DbContextOptions<TestDbContextEncrypted> dbcontextOptions,
            IUserHttpContextAccessorService userHttpContextAccessorService,
            IPolicyHelper policyHelper = null,
            IPolicyContext policyContext = null,
            IServiceProvider serviceProvider = null,
            IDatabaseEncryption databaseEncryption = null)
            : base(dbcontextOptions, userHttpContextAccessorService, logger, policyHelper, policyContext, serviceProvider, databaseEncryption)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            _policyHelper = policyHelper;
            _policyContext = policyContext;
            _serviceProvider = serviceProvider;
            DatabaseEncryption = databaseEncryption ?? throw new ArgumentNullException(nameof(databaseEncryption));
        }

        /// <summary>
        /// Gets or sets the BorrowerEntities.
        /// </summary>
        public DbSet<BorrowerEntity> BorrowerEntities { get; set; }

        public DbSet<LoanEntity> LoanEntities { get; set; }

        public DbSet<PropertyEntity> PropertyEntities { get; set; }

        /// <summary>
        /// Gets or sets the EventQueueEntityEntities.
        /// </summary>
        public DbSet<EventQueueEntity> EventQueueEntityEntities { get; set; }

        /// <summary>
        /// Gets the DatabaseEncryption.
        /// </summary>
        public IDatabaseEncryption DatabaseEncryption { get; }
    }
}
