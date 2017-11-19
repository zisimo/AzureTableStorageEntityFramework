using System.Collections.Generic;
using System.Linq.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal class SingleOrDefaultTranslator : MethodTranslatorBase
    {
        public SingleOrDefaultTranslator(IDictionary<string, string> nameChanges)
            : base(nameChanges, "SingleOrDefault")
        {
        }

        public override void Translate(MethodCallExpression method, ITranslationResult result)
        {
            base.Translate(method, result);
            result.AddTop(2);
        }
    }
}
