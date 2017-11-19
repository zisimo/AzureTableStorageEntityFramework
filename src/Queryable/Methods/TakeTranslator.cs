using System;
using System.Linq.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    internal class TakeTranslator : IMethodTranslator
    {
        private const string MethodName = "Take";

        public string Name => MethodName;

        public void Translate(MethodCallExpression method, ITranslationResult result)
        {
            if (method.Method.Name != MethodName || method.Arguments.Count != 2)
            {
                throw new ArgumentOutOfRangeException(nameof(method), $"Method '{nameof(method)}' is not supported");
            }

            var constant = (ConstantExpression)method.Arguments[1];

            result.AddTop((int)constant.Value);
        }
    }
}
