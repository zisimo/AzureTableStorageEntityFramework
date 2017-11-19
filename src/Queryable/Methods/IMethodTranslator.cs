using System.Linq.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal interface IMethodTranslator
    {
        void Translate(MethodCallExpression methodCall, ITranslationResult result);
        string Name { get; }
    }
}