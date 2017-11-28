using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework
{
    /// <summary>
    ///     An <see cref="IAzureSet"/>.
    /// </summary>
    public interface IAzureSet
    {
        /// <summary>
        ///     Creates a <see cref="CloudTable"/> if it hasn't been created yet on the specified <see cref="CloudTableClient"/>.
        /// </summary>
        /// <param name="tableClient">The <see cref="CloudTableClient"/> to be used.</param>
        Task CreateTableIfNotExistsAsync(CloudTableClient tableClient);

        /// <summary>
        ///     Executes all batch operations to be performed.
        /// </summary>
        /// <param name="context">The <see cref="AzureContext"/> to be used to execute the operations against.</param>
        /// <returns>A task.</returns>
        Task ExecuteBatchOperationsAsync(AzureContext context);
    }
}
