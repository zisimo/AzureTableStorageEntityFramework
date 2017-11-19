using System;

namespace AzureTableStorageEntityFramework.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="object"/>.
    /// </summary>
    public static class ClassExtensions
    {
        /// <summary>
        ///     Gets the <see cref="Attribute"/> that is registered on the <see cref="object"/>.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the <see cref="Attribute"/>.</typeparam>
        /// <param name="o">The <see cref="object"/> on which the <see cref="Attribute"/> is registered.</param>
        /// <returns>The <see cref="Attribute"/> registered on the <see cref="object"/>.</returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this object o) where TAttribute : Attribute
        {
            if (o == null)
            {
                throw new ArgumentNullException(nameof(o));
            }

            return (TAttribute)Attribute.GetCustomAttribute(o.GetType(), typeof(TAttribute));
        }
    }
}
