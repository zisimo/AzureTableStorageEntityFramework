using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AzureTableStorageEntityFramework.Extensions
{
    /// <summary>
    ///     Extension class for <see cref="IQueryable"/>.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public static class IQueryableExtensions
    {

        /// <summary>
        ///     Async create of an array from an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="list">The <see cref="IQueryable{T}"/> to create the array from.</param>
        /// <returns> An Array that contains elements from the input sequence.</returns>
        public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> list)
        {
            return Task.Run(() => list.ToArray());
        }

        /// <summary>
        ///     Async get the first or default item from an <see cref="IQueryable{T}"/>.
        /// </summary>
        /// <typeparam name="T">The type of the elements of source.</typeparam>
        /// <param name="list">The <see cref="IQueryable{T}"/> to create the array from.</param>
        /// <param name="expression"></param>
        /// <returns>A task with the result of the FirstOrDefault.</returns>
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> list, Expression<Func<T, bool>> expression)
        {
            return Task.Run(() => list.FirstOrDefault(expression));
        }
    }
}
