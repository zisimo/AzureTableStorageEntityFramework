namespace AzureTableStorageEntityFramework
{
    internal class Resources
    {
        public static string ExpressionEvaluatorInvalidCast => "Unable to cast type '{0}' to target type '{1}'.";
        public static string ExpressionEvaluatorTypeNotSupported => "Type '{0}' does not supported.";
        public static string ExpressionEvaluatorUnableToEvaluate => "Unable to get value of the node: '{0}'.";
        public static string SerializationExtensionsNotSupportedType => "Unable to serialize type: '{0}'.";
        public static string TableNameCannotBeEmpty => "A Table name cannot be empty or contain whitespace";
        public static string TranslatorMemberNotSupported => "Member '{0}' does not supported.";
        public static string TranslatorMethodInvalidArgument => "Invalid method '{0}' arguments.";
        public static string TranslatorMethodNotSupported => "Method '{0}' does not supported.";
        public static string TranslatorOperatorNotSupported => "Operator '{0}' does not supported.";
        public static string TranslatorUnableToEvaluateExpression => "Unable to evaluate an expression: '{0}'.";
    }
}