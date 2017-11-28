using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="CloudTable"/>.
    /// </summary>
    public static class CloudTableExtensions
    {
        /// <summary>
        ///     Executes a given <see cref="TableQuery"/> on the <see cref="CloudTable"/>.
        /// </summary>
        /// <typeparam name="T">An object that inherits from <see cref="ITableEntity"/>.</typeparam>
        /// <param name="table">The <see cref="CloudTable"/> on which to perform the query.</param>
        /// <param name="query">The <see cref="TableQuery"/> to be executed.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <param name="onProgress">An <see cref="Action"/> to be perfromed on the result</param>
        /// <param name="resolver">The <see cref="EntityResolver{T}"/> to be used. If left null, 
        ///     <see cref="GenericEntityResolver{T}()"/> will be used.
        /// </param>
        /// <returns>A task with the result after execution of the <see cref="query"/>.</returns>
        public static async Task<IEnumerable<T>> ExecuteQueryAsync<T>(this CloudTable table, TableQuery query,
            CancellationToken cancellationToken = default(CancellationToken), Action<IList<T>> onProgress = null,
            EntityResolver<T> resolver = null)
            where T : ITableEntity, new()
        {
            var items = new List<T>();
            TableContinuationToken token = null;

            if (resolver == null)
            {
                resolver = GenericEntityResolver<T>();
            }

            var runningQuery = new TableQuery<T>
            {
                FilterString = query.FilterString,
                SelectColumns = query.SelectColumns,
            };

            do
            {
                runningQuery.TakeCount = query.TakeCount - items.Count;

                var tableQuerySegment = await table.ExecuteQuerySegmentedAsync(query, resolver, token);
                token = tableQuerySegment.ContinuationToken;
                items.AddRange(tableQuerySegment);
                onProgress?.Invoke(items);

            }
            while (token != null && !cancellationToken.IsCancellationRequested &&
                (query.TakeCount == null || items.Count < query.TakeCount.Value));

            return items;
        }

        /// <summary>
        ///     A generic implementation of <see cref="EntityResolver{T}"/>
        /// </summary>
        /// <typeparam name="T">The type that is being resolved. It must inherit from <see cref="ITableEntity"/></typeparam>
        /// <returns>The created <see cref="EntityResolver{T}"/></returns>
        private static EntityResolver<T> GenericEntityResolver<T>() where T : ITableEntity, new()
        {
            return (partitionKey, rowKey, timestamp, props, etag) =>
            {
                var resolvedEntity = new T
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    Timestamp = timestamp,
                    ETag = etag
                };

                resolvedEntity.ReadEntity(props, null);

                return resolvedEntity;
            };
        }
    }
}
