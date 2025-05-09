namespace PressR.Debug.ValueMonitor.Resolver
{
    public class ValueResolutionResult
    {
        public object Value { get; }
        public string Error { get; }
        public bool IsSuccess => Error == null;

        private ValueResolutionResult(object value, string error)
        {
            Value = value;
            Error = error;
        }

        public static ValueResolutionResult Success(object value)
        {
            return new ValueResolutionResult(value, null);
        }

        public static ValueResolutionResult Failure(string error)
        {
            return new ValueResolutionResult(null, error);
        }
    }
}
