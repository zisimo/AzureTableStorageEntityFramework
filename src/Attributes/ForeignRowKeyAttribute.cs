using System;
using System.Linq;

namespace AzureTableStorageEntityFramework.Attributes
{
    /// <summary>
    ///     An Attribute that can be used to corelate foreign keys on Properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ForeignRowKeyAttribute : Attribute
    {
        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="name">The name of the table the property's value is foreign RowKey.</param>
        public ForeignRowKeyAttribute(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Any(char.IsWhiteSpace))
            {
                throw new ArgumentException(Resources.TableNameCannotBeEmpty, nameof(name));
            }

            ForeignTable = name;
        }

        /// <summary>
        ///     The name of the table the property's value is foreign RowKey.
        /// </summary>
        public string ForeignTable { get; }
    }
}