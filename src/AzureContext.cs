using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework
{
    /// <summary>
    ///     An <see cref="AzureContext"/> to be used like a DbContext.
    /// </summary>
    public class AzureContext
    {
        private readonly IDictionary<Type, IAzureSet> _setsDictionary;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="storageAccount">The <see cref="CloudStorageAccount"/> to be used.</param>
        public AzureContext(CloudStorageAccount storageAccount)
        {
            TableClient = storageAccount.CreateCloudTableClient();
            _setsDictionary = new Dictionary<Type, IAzureSet>();
        }

        /// <summary>
        ///     The <see cref="CloudTableClient"/> that is created by the <see cref="CloudStorageAccount"/>
        ///     provided in the constructor.
        /// </summary>
        public CloudTableClient TableClient { get; }

        /// <summary>
        ///     Pushes the registered changes to the <see cref="CloudTableClient"/>.
        /// </summary>
        /// <returns>A task with 1 if all whent well.</returns>
        public async Task<int> SaveChangesAsync()
        {
            for (int i = 0; i < _setsDictionary.Count; i++)
            {
                var azureSet = _setsDictionary.ElementAt(i).Value;
                await azureSet.ExecuteBatchOperationsAsync(this).ConfigureAwait(false);
            }
            return 1;
        }

        /// <summary>
        ///     Creates if needed an <see cref="AzureSet{T}"/> or returns the on that is already registered
        ///     under this <see cref="AzureContext"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="TableEntity"/> objects.</typeparam>
        /// <returns>
        ///     The unique <see cref="AzureSet{T}"/> for the registered implementation of <see cref="TableEntity"/>.
        /// </returns>
        public AzureSet<T> Set<T>() where T : TableEntity, new()
        {
            if (_setsDictionary.ContainsKey(typeof(T)))
            {
                return (AzureSet<T>)_setsDictionary[typeof(T)];
            }

            var newSet = new AzureSet<T>(this);
            newSet.CreateTableIfNotExistsAsync(TableClient);
            _setsDictionary[typeof(T)] = newSet;
            return newSet;
        }
    }
}
