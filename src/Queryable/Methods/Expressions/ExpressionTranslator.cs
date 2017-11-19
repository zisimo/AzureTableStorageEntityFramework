using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace AzureTableStorageEntityFramework.Queryable.Methods.Expressions
{
    internal sealed class ExpressionTranslator : ExpressionVisitor
    {
        private readonly ExpressionEvaluator _constantEvaluator;
        private readonly IDictionary<string, string> _nameChanges;
        private StringBuilder _filter;
        private ITranslationResult _result;

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="nameChanges"></param>
        internal ExpressionTranslator(IDictionary<string, string> nameChanges)
        {
            _nameChanges = nameChanges;
            _constantEvaluator = new ExpressionEvaluator();
        }

        public void Translate(ITranslationResult result, MethodCallExpression method)
        {
            _result = result;

            if (method.Arguments.Count != 2)
            {
                return;
            }

            var lambda = (LambdaExpression)StripQuotes(method.Arguments[1]);

            if (lambda.Body.NodeType == ExpressionType.Constant)
            {
                return;
            }

            _filter = new StringBuilder();

            Visit(lambda.Body);

            _result.AddFilter(TrimString(_filter));
        }

        public void AddPostProcessing(MethodCallExpression method)
        {
            Type type = method.Arguments[0].Type.GetGenericArguments()[0];
            Type genericType = typeof(IQueryable<>).MakeGenericType(type);

            ParameterExpression parameter = Expression.Parameter(genericType, null);
            MethodInfo methodInfo = typeof(System.Linq.Queryable)
                .GetMethods()
                .Single(p => p.Name == method.Method.Name && p.GetParameters().Length == 1)
                .MakeGenericMethod(type);

            MethodCallExpression call = Expression.Call(methodInfo, parameter);

            _result.AddPostProcesing(Expression.Lambda(call, parameter));
        }

        private static string TrimString(StringBuilder builder)
        {
            string initialString = builder.ToString();
            char[] s = new char[initialString.Length];
            char toRemove = '$';
            Stack<int> stack = new Stack<int>();

            for (int i = 0; i < s.Length; i++)
            {
                s[i] = initialString[i];
                if (s[i] == '(')
                    stack.Push(i);
                else if (s[i] == ')')
                {
                    int start = stack.Pop();
                    var endParanthesis = start == 0 && i == s.Length - 1;
                    var middleParanthesis = start != 0 && s[start - 1] == '(' && s[i + 1] == ')';
                    if (endParanthesis || middleParanthesis)
                    {
                        s[start] = s[i] = toRemove;
                    }
                }
            }
            var newString = new string((from c in s where c != toRemove select c).ToArray());
            return newString.Trim();
        }

        public static Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
            {
                expression = ((UnaryExpression)expression).Operand;
            }

            return expression;
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            _filter.AppendFormat(" {0} ", unary.NodeType.Serialize());

            Visit(unary.Operand);

            return unary;
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            ExpressionType nodeType = binary.NodeType;
            ExpressionType leftType = binary.Left.NodeType;
            ExpressionType rightType = binary.Right.NodeType;

            bool paranthesesRequired = nodeType.IsSupported() && (leftType.IsSupported() || rightType.IsSupported());

            if (paranthesesRequired)
            {
                _filter.Append("(");
            }

            // Left part
            if (leftType == ExpressionType.Call)
            {
                if (AppendBinaryCall((MethodCallExpression)binary.Left, nodeType))
                {
                    return binary;
                }
            }
            else
            {
                AppendBinaryPart(binary.Left, leftType);
            }

            // Comparison
            _filter.AppendFormat(" {0} ", nodeType.Serialize());

            // Right part
            if (rightType == ExpressionType.Call)
            {
                var methodCall = (MethodCallExpression)binary.Right;

                if (AppendBinaryCall(methodCall, nodeType))
                {
                    string message = string.Format(Resources.TranslatorMethodNotSupported, methodCall.Method.Name);
                    throw new ArgumentException(message);
                }
            }
            else
            {
                AppendBinaryPart(binary.Right, rightType);
            }

            if (paranthesesRequired)
            {
                _filter.Append(")");
            }

            return binary;
        }

        private void AppendBinaryPart(Expression node, ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Invoke:
                    var invocation = (InvocationExpression)node;
                    Visit(invocation.Expression);
                    break;

                case ExpressionType.New:
                case ExpressionType.NewArrayInit:
                case ExpressionType.Constant:
                    AppendConstant(_constantEvaluator.Evaluate(node));
                    break;

                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    var unary = (UnaryExpression)node;
                    AppendConstant(_constantEvaluator.Evaluate(unary.Operand));
                    break;

                case ExpressionType.MemberAccess:
                    var member = (MemberExpression)node;
                    Expression expression = member.Expression;
                    if (expression != null && expression.NodeType == ExpressionType.Parameter)
                    {
                        AppendParameter(node);
                    }
                    else
                    {
                        AppendConstant(node);
                    }
                    break;

                default:
                    // Check whether expression is binary
                    if (type.IsSupported())
                    {
                        Visit(node);
                    }
                    else
                    {
                        string message = string.Format(Resources.TranslatorMemberNotSupported, type);
                        throw new ArgumentException(message);
                    }
                    break;
            }
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Contains":
                    if (node.Arguments.Count == 1)
                    {
                        var result = (ConstantExpression)_constantEvaluator.Evaluate(node.Object);

                        var enumerable = result.Value as IEnumerable;
                        if (enumerable == null)
                        {
                            string message = string.Format(Resources.TranslatorMethodInvalidArgument, node.Method.Name);
                            throw new ArgumentException(message);
                        }

                        Expression parameter = node.Arguments[0];

                        // determine equality value
                        string equality;

                        if (_filter.Length >= 5 &&
                            _filter.ToString(_filter.Length - 5, 5) == " not ")
                        {
                            _filter.Remove(_filter.Length - 5, 5);
                            equality = " ne ";
                        }
                        else
                        {
                            equality = " eq ";
                        }

                        _filter.Append("(");
                        int count = 0;

                        foreach (object value in enumerable)
                        {
                            AppendParameter(parameter);
                            _filter.Append(equality);
                            AppendConstant(Expression.Constant(value));
                            _filter.Append(" or ");
                            count++;
                        }

                        if (count > 0)
                        {
                            _filter.Remove(_filter.Length - 4, 4);
                            _filter.Append(")");
                        }
                        else
                        {
                            _filter.Remove(_filter.Length - 1, 1);
                        }
                    }
                    else
                    {
                        string message = string.Format(Resources.TranslatorMethodInvalidArgument, node.Method.Name);
                        throw new ArgumentException(message);
                    }
                    break;

                case "ToString":
                    ConstantExpression constant;

                    if (node.Object != null)
                    {
                        var instance = _constantEvaluator.Evaluate(node.Object);
                        constant = Expression.Constant(instance.ToString());
                    }
                    else
                    {
                        constant = Expression.Constant(string.Empty);
                    }

                    AppendConstant(constant);
                    break;

                default:
                    AppendConstant(_constantEvaluator.Evaluate(node));
                    break;
            }

            return node;
        }

        /// <summary>
        ///     Translates method call expression.
        /// </summary>
        /// <param name="node">Expression.</param>
        /// <param name="type">Expression type.</param>
        /// <returns>Whether expression has been completely translated.</returns>
        private bool AppendBinaryCall(MethodCallExpression node, ExpressionType type)
        {
            switch (node.Method.Name)
            {
                case "CompareTo":
                    AppendParameter(_constantEvaluator.Evaluate(node.Object));
                    _filter.AppendFormat(" {0} ", type.Serialize());
                    AppendConstant(_constantEvaluator.Evaluate(node.Arguments[0]));
                    return true;

                case "Compare":
                case "CompareOrdinal":
                    if (node.Arguments.Count >= 2)
                    {
                        AppendParameter(_constantEvaluator.Evaluate(node.Arguments[0]));
                        _filter.AppendFormat(" {0} ", type.Serialize());
                        AppendConstant(_constantEvaluator.Evaluate(node.Arguments[1]));
                    }
                    else
                    {
                        string message = string.Format(Resources.TranslatorMethodInvalidArgument, node.Method.Name);
                        throw new ArgumentException(message);
                    }
                    return true;

                default:
                    VisitMethodCall(node);
                    break;
            }

            return false;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            AppendParameter(node);
            return node;
        }

        private void AppendParameter(Expression node)
        {
            if (node.NodeType != ExpressionType.MemberAccess)
            {
                string message = string.Format(Resources.TranslatorMemberNotSupported, node.NodeType);
                throw new NotSupportedException(message);
            }

            var member = (MemberExpression)node;

            // Append member
            string memberName = member.Member.Name;
            if (!_nameChanges.TryGetValue(memberName, out string name))
            {
                name = memberName;
            }

            _filter.Append(name);
        }

        private void AppendConstant(Expression node)
        {
            // Evaluate if required
            if (node.NodeType != ExpressionType.Constant)
            {
                Expression result = _constantEvaluator.Evaluate(node);
                if (result.NodeType != ExpressionType.Constant)
                {
                    string message = string.Format(Resources.TranslatorUnableToEvaluateExpression, node);
                    throw new InvalidDataException(message);
                }

                node = result;
            }

            var constant = (ConstantExpression)node;
            _filter.Append(constant.Serialize());
        }
    }
}
