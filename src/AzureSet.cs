using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AzureTableStorageEntityFramework.Extensions;
using AzureTableStorageEntityFramework.Queryable;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework
{
    /// <summary>
    ///     An <see cref="AzureSet{T}"/> is to be used like a DbSet.
    /// </summary>
    /// <typeparam name="T">The type of objects the <see cref="AzureSet{T}"/> holds.</typeparam>
    public class AzureSet<T> : IAzureSet, IOrderedQueryable<T>, IQueryable<T>, IEnumerable<T> where T : TableEntity, new()
    {
        private const int TableServiceBatchMaximumOperations = 100;

        private IDictionary<T, ChangeAction> _entitiesDictionary;

        public AzureSet(AzureContext azureContext)
        {
            if (azureContext == null)
            {
                throw new ArgumentNullException(nameof(azureContext));
            }

            _entitiesDictionary = new Dictionary<T, ChangeAction>();
            Expression = Expression.Constant(this);
            Provider = new QueryProvider<T>(azureContext.TableClient.GetTableReference(new T().PartitionKey));
        }

        /// <summary>
        ///     Constructor.
        ///     This is used internally by the <see cref="IQueryable"/>.
        /// </summary>
        /// <param name="provider">The <see cref="IQueryProvider"/> to be used.</param>
        /// <param name="expression">The <see cref="Expression"/> to be used.</param>
        public AzureSet(IQueryProvider provider, Expression expression)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            Provider = provider;
            Expression = expression;
        }

        #region IQueryable_Implementation

        /// <summary>
        ///     See <seealso cref="IQueryable"/>
        /// </summary>
        public Expression Expression { get; }
        /// <summary>
        ///     See <seealso cref="IQueryable"/>
        /// </summary>
        public Type ElementType => typeof(T);
        /// <summary>
        ///     See <seealso cref="IQueryable"/>
        /// </summary>
        public IQueryProvider Provider { get; }

        #endregion

        #region IAzureSet_Implementation

        /// <summary>
        ///     See <see cref="IAzureSet.CreateTableIfNotExists"/>.
        /// </summary>
        public Task CreateTableIfNotExistsAsync(CloudTableClient tableClient)
        {
            return tableClient.CreateTableIfNotExistsAsync(new T().PartitionKey);
        }

        /// <summary>
        ///     See <see cref="IAzureSet.ExecuteBatchOperationsAsync"/>.
        /// </summary>
        public Task ExecuteBatchOperationsAsync(AzureContext context)
        {
            var tasks = new List<Task>();
            var batch = new TableBatchOperation();
            var table = context.TableClient.GetTableReference(new T().PartitionKey);

            for (var i = 0; i < _entitiesDictionary.Count; i++)
            {
                AddOperationToBatch(context, _entitiesDictionary.ElementAt(i), batch);

                if (i % TableServiceBatchMaximumOperations == 0)
                {
                    tasks.Add(ExecuteBatchAsync(batch, table));
                    batch = new TableBatchOperation();
                }
            }
            tasks.Add(ExecuteBatchAsync(batch, table));
            _entitiesDictionary = new Dictionary<T, ChangeAction>();
            return Task.WhenAll(tasks);
        }

        #endregion

        #region IEnumerable<T>_Implementation

        /// <summary>
        ///     See <see cref="IEnumerable"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator()
        {
            return Provider.Execute<IEnumerable<T>>(Expression).GetEnumerator();
        }

        /// <summary>
        ///     See <see cref="IEnumerable"/>
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return Provider.Execute<IEnumerable>(Expression).GetEnumerator();
        }

        #endregion

        /// <summary>
        ///     Adds the provided entity to the <see cref="AzureSet{T}"/> and marks it with a <see cref="ChangeAction.Insert"/> operation.
        ///     The entity will be inserted or override any existing with the same <see cref="TableEntity.RowKey"/>.
        /// </summary>
        /// <param name="entity">The <see cref="TableEntity"/> to be inserted.</param>
        /// <returns>The <see cref="TableEntity"/>.</returns>
        public T Add(T entity)
        {
            _entitiesDictionary[entity] = ChangeAction.Insert;
            return entity;
        }

        /// <summary>
        ///     Same like <see cref="Add"/> but works on an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="entities">The entities to be added.</param>
        /// <returns>The entities added.</returns>
        public IEnumerable<T> AddRange(IEnumerable<T> entities)
        {
            var added = new List<T>();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    added.Add(Add(entity));
                }
            }
            return added;
        }

        /// <summary>
        ///     Adds the provided entity to the <see cref="AzureSet{T}"/> and marks it with a <see cref="ChangeAction.Delete"/> operation.
        ///     The entity will be deleted if it exists based on it's <see cref="TableEntity.RowKey"/> and it's
        ///     <see cref="Microsoft.WindowsAzure.Storage.Table.TableEntity.Timestamp"/>.
        /// </summary>
        /// <param name="entity">The <see cref="TableEntity"/> to be deleted.</param>
        /// <returns>The <see cref="TableEntity"/>.</returns>
        public T Remove(T entity)
        {
            _entitiesDictionary[entity] = ChangeAction.Delete;
            return entity;
        }

        /// <summary>
        ///     Same like <see cref="Remove"/> but works on an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="entities">The entities to be deleted.</param>
        /// <returns>The entities deleted.</returns>
        public IEnumerable<T> RemoveRange(IEnumerable<T> entities)
        {
            var removed = new List<T>();
            if (entities != null)
            {
                foreach (var entity in entities)
                {
                    removed.Add(Remove(entity));
                }
            }
            return removed;
        }

        private void AddOperationToBatch(AzureContext context, KeyValuePair<T, ChangeAction> entry, TableBatchOperation batch)
        {
            switch (entry.Value)
            {
                case ChangeAction.Delete:
                    batch.Delete(entry.Key);
                    break;

                case ChangeAction.Insert:
                    entry.Key.AddComplexProperties(context);
                    batch.InsertOrReplace(entry.Key);
                    break;

                case ChangeAction.None:
                    break;

                case ChangeAction.Update:
                    entry.Key.AddComplexProperties(context);
                    batch.InsertOrReplace(entry.Key);
                    break;

                default:
                    break;
            }
        }

        private Task ExecuteBatchAsync(TableBatchOperation batch, CloudTable table)
        {
            return batch.Count == 0
                ? Task.CompletedTask
                : table.ExecuteBatchAsync(batch);
        }
    }
}
