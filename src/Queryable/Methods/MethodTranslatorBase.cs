using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using AzureTableStorageEntityFramework.Queryable.Methods.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal abstract class MethodTranslatorBase : IMethodTranslator
    {
        private readonly string _methodName;
        private readonly IDictionary<string, string> _nameChanges;

        protected MethodTranslatorBase(IDictionary<string, string> nameChanges, string methodName)
        {
            _nameChanges = nameChanges;
            _methodName = methodName;
        }

        public string Name => _methodName;

        public virtual void Translate(MethodCallExpression method, ITranslationResult result)
        {
            if (method.Method.Name != _methodName)
            {
                throw new ArgumentOutOfRangeException($"Method '{nameof(method)}' is not supported");
            }

            var expressionTranslator = new ExpressionTranslator(_nameChanges);

            MethodCallExpression targetMethod = method;

            if (method.Arguments.Count == 1 && method.Arguments[0].NodeType == ExpressionType.Call)
            {
                targetMethod = (MethodCallExpression)method.Arguments[0];
            }

            expressionTranslator.Translate(result, targetMethod);
            expressionTranslator.AddPostProcessing(method);
        }
    }
}
