using System.Linq.Expressions;
using Microsoft.WindowsAzure.Storage.Table;

namespace AzureTableStorageEntityFramework.Queryable
{
    public interface ITranslationResult
    {
        TableQuery TableQuery { get; }

        void AddColumn(string name);
        void AddTop(int i);
        void AddPostProcesing(LambdaExpression lambda);
        void AddFilter(string trimString);
    }
}
