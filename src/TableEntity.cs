using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AzureTableStorageEntityFramework.Attributes;
using AzureTableStorageEntityFramework.Extensions;

namespace AzureTableStorageEntityFramework
{
    /// <summary>
    ///     A TableEntity that extends <see cref="Microsoft.WindowsAzure.Storage.Table.TableEntity"/>.
    /// </summary>
    public class TableEntity : Microsoft.WindowsAzure.Storage.Table.TableEntity
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        public TableEntity()
        {
            if (string.IsNullOrEmpty(PartitionKey))
            {
                PartitionKey = GeneratePartitionKey();
            }
        }

        #region Overriding_GetHashCode

        /// <summary>
        ///     Overriding the GetHashCode method to ensure that there is only one item in the dictionary with the same
        ///     <see cref="TableEntity.PartitionKey"/> and <see cref="TableEntity.RowKey"/> 
        /// </summary>
        /// <returns>The generated HashCode</returns>
        public override int GetHashCode()
        {
            return $"{PartitionKey}_{RowKey}".GetHashCode();
        }

        #endregion

        /// <summary>
        ///     Adds in the <see cref="AzureContext"/> properties that implement <see cref="TableEntity"/>
        ///     or <see cref="IEnumerable{T}"/> of <see cref="TableEntity"/> .
        /// </summary>
        /// <param name="context">The <see cref="AzureContext"/> to register the properties.</param>
        internal void AddComplexProperties(AzureContext context)
        {
            foreach (var property in GetType().GetProperties())
            {
                if (property.CanRead && typeof(TableEntity).IsAssignableFrom(property.PropertyType))
                {
                    AddComplexPropertyAssignableFromTableEntity(context, property);
                }

                else if (property.CanRead && typeof(IEnumerable<TableEntity>).IsAssignableFrom(property.PropertyType))
                {
                    AddComplexPropertyAssignableFromIEnumerableTableEntity(context, property);
                }
            }
        }

        private void AddComplexPropertyAssignableFromTableEntity(AzureContext context, PropertyInfo property)
        {
            var propertyTableEntity = (TableEntity)property.GetValue(this);
            if (propertyTableEntity == null)
            {
                return;
            }

            var set = typeof(AzureContext)
                .GetMethod("Set")
                ?.MakeGenericMethod(propertyTableEntity.GetType())
                .Invoke(context, null);

            propertyTableEntity.SetForeignRowKeyAttribute(RowKey, PartitionKey);

            set?.GetType()
                .GetMethod("Add")
                ?.Invoke(set, new object[] { propertyTableEntity });
        }

        private void AddComplexPropertyAssignableFromIEnumerableTableEntity(AzureContext context, PropertyInfo property)
        {
            var propertyIEnumerableTableEntity = (IEnumerable<TableEntity>)property.GetValue(this);
            if (propertyIEnumerableTableEntity == null)
            {
                return;
            }

            var set = typeof(AzureContext)
                .GetMethod("Set")
                ?.MakeGenericMethod(propertyIEnumerableTableEntity.GetType().GetElementType())
                .Invoke(context, null);

            foreach (var propertyTableEntity in propertyIEnumerableTableEntity)
            {
                propertyTableEntity.SetForeignRowKeyAttribute(RowKey, PartitionKey);
            }

            set?.GetType()
                .GetMethod("AddRange")
                ?.Invoke(set, new object[] { propertyIEnumerableTableEntity });
        }

        /// <summary>
        ///     Creates the Partition Key based on the name of the class by pluralizing the name of the class
        ///     or the <see cref="TableAttribute"/> registered on the <see cref="TableEntity"/>'s implementation.
        /// </summary>
        /// <returns>A string with the Partition Key.</returns>
        private string GeneratePartitionKey()
        {
            var tableAttribute = GetTableAttribute();
            if (tableAttribute != null)
            {
                return tableAttribute.TableName;
            }

            var partitionKey = GetType().Name;
            if (partitionKey.Substring(partitionKey.Length - 1, 1).ToLower() == "y")
            {
                partitionKey = partitionKey.Substring(0, partitionKey.Length - 1) + "ies";
            }

            if (partitionKey.Substring(partitionKey.Length - 1, 1).ToLower() != "s")
            {
                partitionKey = partitionKey + "s";
            }
            return partitionKey;
        }

        /// <summary>
        ///     Gets the <see cref="TableAttribute"/> that is registered on this <see cref="TableEntity"/>.
        /// </summary>
        /// <returns>The registered <see cref="TableEntity"/> or null if the attribute does not exist.</returns>
        private TableAttribute GetTableAttribute()
        {
            return this.GetCustomAttribute<TableAttribute>();
        }

        private void SetForeignRowKeyAttribute(string foreignRowKey, string foreignPartitionKey)
        {
            var property = GetType().GetProperties()
                .FirstOrDefault(x => x.GetCustomAttribute<ForeignRowKeyAttribute>()?.ForeignTable == foreignPartitionKey);

            if (property != null)
            {
                property.SetValue(this, foreignRowKey);
            }
        }
    }
}
