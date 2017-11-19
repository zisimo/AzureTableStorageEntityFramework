using System;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="CloudTableClient"/>.
    /// </summary>
    public static class CloudTableClientExtensions
    {
        /// <summary>
        ///     Creates a <see cref="CloudTable"/> if it hasn't been created yet on the specified <see cref="CloudTableClient"/>.
        /// </summary>
        /// <param name="tableClient">The <see cref="CloudTableClient"/> to be used.</param>
        /// <param name="tableName">The name of the table.</param>
        public static void CreateTableIfNotExists(this CloudTableClient tableClient, string tableName)
        {
            if (tableClient == null)
            {
                throw new ArgumentNullException(nameof(tableClient));
            }

            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentException("Argument cannot be null or empty", nameof(tableName));
            }

            var table = tableClient.GetTableReference(tableName);
            table.CreateIfNotExists();
        }
    }
}
