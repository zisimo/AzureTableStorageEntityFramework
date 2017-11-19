using System.Collections.Generic;
using System.Linq.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal class SingleTranslator : MethodTranslatorBase
    {
        public SingleTranslator(IDictionary<string, string> nameChanges) : base(nameChanges, "Single")
        {
        }

        public override void Translate(MethodCallExpression method, ITranslationResult result)
        {
            base.Translate(method, result);
            result.AddTop(2);
        }
    }
}
