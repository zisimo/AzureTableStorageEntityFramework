using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AzureTableStorageEntityFramework.Extensions;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework.Queryable
{
    public class QueryProvider<T> : IAsyncQueryProvider<T> where T : ITableEntity, new()
    {
        private readonly QueryTranslator _queryTranslator;
        private readonly CloudTable _cloudTable;

        public QueryProvider(CloudTable cloudTable)
        {
            _cloudTable = cloudTable;
            _queryTranslator = new QueryTranslator(new Dictionary<string, string>());
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(AzureSet<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException;
            }
        }

        public IQueryable<TResult> CreateQuery<TResult>(Expression expression)
        {
            return (IQueryable<TResult>)Activator.CreateInstance(typeof(AzureSet<>).MakeGenericType(typeof(T)), new object[] { this, expression });
        }

        public object Execute(Expression expression)
        {
            return Execute(expression, false);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            bool isEnumerable = typeof(IEnumerable<T>).IsAssignableFrom(typeof(TResult));
            var result = Execute(expression, isEnumerable);
            return (TResult)result;
        }

        public object Execute(Expression expression, bool isEnumerable)
        {
            var task = ExecuteAsync(expression);
            task.ConfigureAwait(false);
            var result = task.Result;
            if (isEnumerable)
            {
                return result;
            }
            return result.FirstOrDefault();
        }

        /// <summary>
        ///     Executes expression query asynchronously.
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task<IEnumerable<T>> ExecuteAsync(Expression expression, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var result = _queryTranslator.Translate(expression);

            return _cloudTable.ExecuteQueryAsync<T>(result.TableQuery, cancellationToken);
        }
    }
}
