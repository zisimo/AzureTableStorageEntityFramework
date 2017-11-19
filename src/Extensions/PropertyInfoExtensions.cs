using System;
using System.Reflection;

namespace AzureTableStorageEntityFramework.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="PropertyInfo"/>.
    /// </summary>
    public static class PropertyInfoExtensions
    {
        /// <summary>
        ///      Gets the <see cref="Attribute"/> that is registered on the <see cref="PropertyInfo"/>.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the <see cref="Attribute"/>.</typeparam>
        /// <param name="property">The Property on which the <see cref="Attribute"/> is registered.</param>
        /// <returns>The <see cref="Attribute"/> registered on the <see cref="object"/>.</returns>
        public static TAttribute GetPropertyAttribute<TAttribute>(PropertyInfo property) where TAttribute : Attribute
        {
            if (property == null)
            {
                throw new ArgumentNullException(nameof(property));
            }

            return (TAttribute)property.GetCustomAttribute(typeof(TAttribute));
        }
    }
}
