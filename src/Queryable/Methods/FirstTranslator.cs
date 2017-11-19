using System.Collections.Generic;
using System.Linq.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal class FirstTranslator : MethodTranslatorBase
    {
        public FirstTranslator(IDictionary<string, string> nameChanges) : base(nameChanges, "First")
        {
        }

        public override void Translate(MethodCallExpression method, ITranslationResult result)
        {
            base.Translate(method, result);
            result.AddTop(1);
        }
    }
}
