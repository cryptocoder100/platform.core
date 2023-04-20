namespace Exos.Platform.Persistence.GenericRepo
{
    using System.Data;
    using System.Threading.Tasks;

    /// <summary>
    /// Generic Repository.
    /// </summary>
    public interface IGenericRepo
    {
        /// <summary>
        /// Save an Entity.
        /// </summary>
        /// <typeparam name="T">Type of Entity.</typeparam>
        /// <param name="entity">Entity to save.</param>
        /// <param name="id">Entity id.</param>
        /// <returns>Saved object.</returns>
        Task Save<T>(T entity, int id) where T : class;
    }

    /// <summary>
    /// Dapper Generic Repository.
    /// </summary>
    public interface IDapperGenericRepo : IGenericRepo
    {
        /// <summary>
        /// DB Connection.
        /// </summary>
        /// <param name="sqlConnection"><see cref="IDbConnection"/>.</param>
        void ProvideSqlConnection(IDbConnection sqlConnection);
    }

    /// <summary>
    /// Entity Framework Generic Repository.
    /// </summary>
    public interface IEfGenericRepo : IGenericRepo
    {
        /// <summary>
        /// DbContext.
        /// </summary>
        /// <param name="platformDbContext"><see cref="PlatformDbContext"/>.</param>
        void ProvideDbContext(PlatformDbContext platformDbContext);
    }
}