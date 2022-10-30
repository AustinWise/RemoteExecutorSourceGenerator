namespace RemoteExecutorLib
{
    public static class MethodRegistry
    {
        private static readonly Dictionary<string, (int, Func<string[], int?>)> sMethods = new();

        public static void RegisterMethod(string key, int numberOfParameters, Func<string[], int?> func)
        {
            lock (sMethods)
            {
                sMethods.Add(key, (numberOfParameters, func));
            }
        }

        internal static int? Invoke(string key, string[] args)
        {
            (int numberOfParameters, Func<string[], int?> func) tup;
            lock (sMethods)
            {
                if (!sMethods.TryGetValue(key, out tup))
                {
                    throw new InvalidOperationException("Key not found: " + key);
                }
            }

            if (args.Length != tup.numberOfParameters)
            {
                throw new InvalidOperationException($"For key '{key}', expected {tup.numberOfParameters} arguments but got {args.Length}.");
            }

            return tup.func(args);
        }
    }
}
