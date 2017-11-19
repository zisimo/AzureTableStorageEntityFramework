using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AzureTableStorageEntityFramework.Queryable.Methods.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal class WhereTranslator : IMethodTranslator
    {
        private const string MethodName = "Where";
        private readonly IDictionary<string, string> _nameChanges;

        public WhereTranslator(IDictionary<string, string> nameChanges)
        {
            _nameChanges = nameChanges;
        }

        public string Name => MethodName;

        public void Translate(MethodCallExpression method, ITranslationResult result)
        {
            if (method.Method.Name != MethodName)
            {
                throw new ArgumentOutOfRangeException(nameof(method), $"Method '{nameof(method)}' is not supported");
            }

            var expressionTranslator = new ExpressionTranslator(_nameChanges);
            expressionTranslator.Translate(result, method);
        }
    }
}
