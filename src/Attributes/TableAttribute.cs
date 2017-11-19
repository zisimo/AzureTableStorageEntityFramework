using System;
using System.Linq;

namespace AzureTableStorageEntityFramework.Attributes
{
    /// <summary>
    ///     An Attribute that can be used to override the default naming of tables.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TableAttribute : Attribute
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="name">The name of the table.</param>
        public TableAttribute(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException(Resources.TableNameCannotBeEmpty, nameof(name));
            }

            TableName = name;
        }

        /// <summary>
        ///     The name of the Table.
        /// </summary>
        public string TableName { get; }
    }
}
