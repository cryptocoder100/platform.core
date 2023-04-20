#pragma warning disable CA1813 // Avoid unsealed attributes
#pragma warning disable CA1019 // Define accessors for attribute arguments
namespace Exos.Platform.Persistence
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Storage;

    /// <summary>
    /// Action Filter to handle db transactions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class TransactionRequiredAttribute : ActionFilterAttribute
    {
        private readonly PlatformDbContext _platformDbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionRequiredAttribute"/> class.
        /// </summary>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        public TransactionRequiredAttribute(PlatformDbContext platformDbContext)
        {
            _platformDbContext = platformDbContext ?? throw new ArgumentNullException(nameof(platformDbContext));
        }

        /// <inheritdoc/>
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            IExecutionStrategy executionStrategy = _platformDbContext.Database.CreateExecutionStrategy();
            await executionStrategy.ExecuteAsync(async () =>
            {
                using IDbContextTransaction dbcontextTransaction = await _platformDbContext.Database.BeginTransactionAsync();
                ActionExecutedContext actionExecutedContext = await next();
                if (actionExecutedContext.Exception == null)
                {
                    await dbcontextTransaction.CommitAsync();
                }
                else
                {
                    await dbcontextTransaction.RollbackAsync();
                }
            });
        }
    }
}
#pragma warning restore CA1813 // Avoid unsealed attributes
#pragma warning restore CA1019 // Define accessors for attribute arguments