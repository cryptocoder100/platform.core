namespace Exos.Platform.Persistence.GenericRepo
{
    /// <summary>
    /// SqlRepositoryOptions used by generic Dapper repo.
    /// </summary>
    public class SqlRepositoryOptions
    {
        /// <summary>
        /// Gets or sets ConnectionString.
        /// </summary>
        public string ConnectionString { get; set; }
    }
}