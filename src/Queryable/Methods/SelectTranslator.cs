using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AzureTableStorageEntityFramework.Queryable.Methods.Expressions;

namespace AzureTableStorageEntityFramework.Queryable.Methods
{
    /// <summary>
    ///     LINQ Select method translator.
    /// </summary>
    internal sealed class SelectTranslator : IMethodTranslator
    {
        private const string MethodName = "Select";
        private readonly IDictionary<string, string> _nameChanges;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="nameChanges">Entity properties name changes.</param>
        internal SelectTranslator(IDictionary<string, string> nameChanges)
        {
            _nameChanges = nameChanges;
        }

        public string Name => MethodName;

        public void Translate(MethodCallExpression method, ITranslationResult result)
        {
            if (method.Method.Name != MethodName || method.Arguments.Count != 2)
            {
                var message = string.Format(Resources.TranslatorMethodNotSupported, method.Method.Name);
                throw new ArgumentOutOfRangeException(nameof(method), message);
            }

            var lambda = (LambdaExpression)ExpressionTranslator.StripQuotes(method.Arguments[1]);

            // ReSharper disable ForCanBeConvertedToForeach

            if (lambda.Body.NodeType == ExpressionType.MemberInit)
            {
                var memberInit = (MemberInitExpression)lambda.Body;

                for (int i = 0; i < memberInit.Bindings.Count; i++)
                {
                    MemberBinding binding = memberInit.Bindings[i];
                    AddColumn(binding.Member.Name, result);
                }
            }
            else if (lambda.Body.NodeType == ExpressionType.New)
            {
                var newInstance = (NewExpression)lambda.Body;

                for (int i = 0; i < newInstance.Arguments.Count; i++)
                {
                    Expression argument = newInstance.Arguments[i];
                    var member = (MemberExpression)argument;
                    AddColumn(member.Member.Name, result);
                }
            }
            else
            {
                throw new NotSupportedException(string.Format(Resources.TranslatorMemberNotSupported, lambda.Body.NodeType));
            }

            // ReSharper restore ForCanBeConvertedToForeach

            AddPostProcessing(method, result);
        }

        private void AddColumn(string parameterName, ITranslationResult result)
        {
            if (!_nameChanges.TryGetValue(parameterName, out string name))
            {
                name = parameterName;
            }

            result.AddColumn(name);
        }

        private void AddPostProcessing(MethodCallExpression method, ITranslationResult result)
        {
            Type type = method.Arguments[0].Type.GetGenericArguments()[0];
            ParameterExpression parameter = Expression.Parameter(typeof(IQueryable<>).MakeGenericType(type), null);
            MethodCallExpression call = Expression.Call(method.Method, parameter, method.Arguments[1]);

            result.AddPostProcesing(Expression.Lambda(call, parameter));
        }
    }
}