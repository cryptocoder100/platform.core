namespace Exos.Platform.Persistence.Tests.DbContext
{
    using System;
    using Exos.Platform.Persistence.Tests.Entities;
    using Exos.Platform.TenancyHelper.Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Test DBContext.
    /// </summary>
    public class TestDbContext : PlatformDbContext
    {
        private readonly ILogger<TestDbContext> _logger;
        private readonly IUserHttpContextAccessorService _userHttpContextAccessorService;
        private readonly IPolicyHelper _policyHelper;
        private readonly IPolicyContext _policyContext;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestDbContext"/> class.
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/>.</param>
        /// <param name="dbcontextOptions"><see cref="DbContextOptions"/>.</param>
        /// <param name="userHttpContextAccessorService"><see cref="IUserHttpContextAccessorService"/>.</param>
        /// <param name="policyHelper"><see cref="IPolicyHelper"/>.</param>
        /// <param name="policyContext"><see cref="IPolicyContext"/>.</param>
        /// <param name="serviceProvider"><see cref="IServiceProvider"/>.</param>
        public TestDbContext(
            ILogger<TestDbContext> logger,
            DbContextOptions<TestDbContext> dbcontextOptions,
            IUserHttpContextAccessorService userHttpContextAccessorService,
            IPolicyHelper policyHelper = null,
            IPolicyContext policyContext = null,
            IServiceProvider serviceProvider = null)
            : base(dbcontextOptions, userHttpContextAccessorService, logger, policyHelper, policyContext, serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userHttpContextAccessorService = userHttpContextAccessorService ?? throw new ArgumentNullException(nameof(userHttpContextAccessorService));
            _policyHelper = policyHelper;
            _policyContext = policyContext;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Gets or sets BorrowerEntities.
        /// </summary>
        public DbSet<BorrowerEntity> BorrowerEntities { get; set; }
    }
}
