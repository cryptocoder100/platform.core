using System;
using System.Collections.Generic;
using System.Linq;

namespace Exos.Platform.Persistence
{
    /// <summary>
    /// Defines the <see cref="DapperQueryMultipleResults" />.
    /// </summary>
    public class DapperQueryMultipleResults
    {
        private readonly List<Type> _queryReturnTypes;
        private readonly List<IEnumerable<object>> _multipleQueryResults = new List<IEnumerable<object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DapperQueryMultipleResults"/> class.
        /// </summary>
        /// <param name="queryReturnTypes">The queryReturnTypes<see cref="List{Type}"/>.</param>
        internal DapperQueryMultipleResults(List<Type> queryReturnTypes)
        {
            _queryReturnTypes = queryReturnTypes;
        }

        /// <summary>
        /// Return the number of queries executed.
        /// </summary>
        /// <returns>The <see cref="int"/>.</returns>
        public int ResultsCount()
        {
            return _multipleQueryResults.Count;
        }

        /// <summary>
        /// Read the results of the query.
        /// </summary>
        /// <typeparam name="T">Model of Entity that the query will return..</typeparam>
        /// <returns>The <see cref="IEnumerable{T}"/>Collection of entity/models.</returns>
        public IEnumerable<T> ReadQueryResults<T>() => ReadQueryResultsImpl<T>(typeof(T));

        internal void AddQueryResult(IEnumerable<object> queryResult)
        {
            _multipleQueryResults.Add(queryResult);
        }

        private IEnumerable<T> ReadQueryResultsImpl<T>(Type type)
        {
            // Find the index of the query type
            int typeIndex = _queryReturnTypes.IndexOf(type);
            if (typeIndex < 0)
            {
                return null;
            }

            var result = _multipleQueryResults.ElementAt(typeIndex).Cast<T>();
            return result;
        }
    }
}
