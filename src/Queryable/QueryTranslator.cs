using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using AzureTableStorageEntityFramework.Queryable.Methods;

namespace AzureTableStorageEntityFramework.Queryable
{
    public class QueryTranslator
    {
        private readonly IDictionary<string, IMethodTranslator> _methodTranslators;

        /// <summary>
        ///     Constructor.
        /// </summary>
        internal QueryTranslator(IDictionary<string, string> nameChanges)
            : this(GetTranslators(nameChanges))
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="methodTranslators">LINQ Expression methods translators.</param>
        internal QueryTranslator(IEnumerable<IMethodTranslator> methodTranslators)
        {
            _methodTranslators = methodTranslators.ToDictionary(translator => translator.Name);
        }

        public ITranslationResult Translate(Expression expression)
        {
            return Translate(expression, new TranslationResult());
        }

        /// <summary>
        ///     Translates a LINQ expression into collection of query segments.
        /// </summary>
        /// <param name="expression">LINQ expression.</param>
        /// <param name="result">Translation result.</param>
        /// <returns>Collection of query segments.</returns>
        public ITranslationResult Translate(Expression expression, ITranslationResult result)
        {
            if (expression.NodeType != ExpressionType.Call)
            {
                return result;
            }

            var methodCall = (MethodCallExpression)expression;

            VisitMethodCall(methodCall, result);

            // ReSharper disable ForCanBeConvertedToForeach

            // Visit arguments
            for (int i = 0; i < methodCall.Arguments.Count; i++)
            {
                Expression argument = methodCall.Arguments[i];
                if (argument.NodeType == ExpressionType.Call)
                {
                    Translate(argument, result);
                }
            }

            // ReSharper restore ForCanBeConvertedToForeach

            return result;
        }

        private static IEnumerable<IMethodTranslator> GetTranslators(IDictionary<string, string> nameChanges)
        {
            return new List<IMethodTranslator>
            {
                new WhereTranslator(nameChanges),
                new FirstTranslator(nameChanges),
                new FirstOrDefaultTranslator(nameChanges),
                new SingleTranslator(nameChanges),
                new SingleOrDefaultTranslator(nameChanges),
                new SelectTranslator(nameChanges),
                new TakeTranslator()
            };
        }

        private void VisitMethodCall(MethodCallExpression methodCall, ITranslationResult result)
        {
            if (methodCall.Method.DeclaringType != typeof(System.Linq.Queryable))
            {
                throw new NotSupportedException(string.Format(Resources.TranslatorMethodNotSupported, methodCall.Method.Name));
            }

            // Get a method translator
            if (!_methodTranslators.TryGetValue(methodCall.Method.Name, out IMethodTranslator translator))
            {
                string message = string.Format(Resources.TranslatorMethodNotSupported, methodCall.Method.Name);
                throw new NotSupportedException(message);
            }

            translator.Translate(methodCall, result);

        }
    }
}
