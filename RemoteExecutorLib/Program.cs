// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;

namespace RemoteExecutorLib
{
    /// <summary>
    /// Provides an entry point in a new process that will load a specified method and invoke it.
    /// </summary>
    public static class Program
    {
        internal static int Main(string[] args)
        {
            int? maybeExitCode = TryExecute(args);
            if (maybeExitCode.HasValue)
            {
                return maybeExitCode.Value;
            }

            // we should not get here
            Console.Error.WriteLine("Remote executor EXE started, but missing magic argument: " + RemoteExecutor.REMOTE_EXECUTOR_MARKER_ARG);
            return -1;
        }

        public static int? TryExecute(string[] args)
        {
            if (args.Length == 0 || args[0] != RemoteExecutor.REMOTE_EXECUTOR_MARKER_ARG)
            {
                return null;
            }

            // The program expects to be passed the target assembly name to load, the type
            // from that assembly to find, and the method from that assembly to invoke.
            // Any additional arguments are passed as strings to the method.
            if (args.Length < 4)
            {
                Console.Error.WriteLine("Usage: {0} " + RemoteExecutor.REMOTE_EXECUTOR_MARKER_ARG + " assemblyName methodKey exceptionFile [additionalArgs]", typeof(Program).GetTypeInfo().Assembly.GetName().Name);
                Environment.Exit(-1);
                return -1;
            }

            string assemblyName = args[1];
            string methodkey = args[2];
            string exceptionFile = args[3];
            string[] additionalArgs = args.Length > 4 ?
                args.Subarray(4, args.Length - 4) :
                Array.Empty<string>();

            // Load the specified assembly, type, and method, then invoke the method.
            // The program's exit code is the return value of the invoked method.
            int exitCode = RemoteExecutor.SuccessExitCode;
            try
            {
                // Load the assemly and run module initializers to register methods.
                Assembly a = Assembly.Load(new AssemblyName(assemblyName));
                System.Runtime.CompilerServices.RuntimeHelpers.RunModuleConstructor(a.ManifestModule.ModuleHandle);

                int? maybeExitCode = MethodRegistry.Invoke(methodkey, additionalArgs);
                if (maybeExitCode.HasValue)
                {
                    exitCode = maybeExitCode.Value;
                }
            }
            // There's a bug in the .NET 7 Preview 7 runtime that makes an AccessViolationException catchable.
            // For backward compatibility with the previous behavior, don't catch these exceptions.
            // See https://github.com/dotnet/runtime/issues/73794 for more info.
            catch (Exception exc) when (exc is not (TargetInvocationException { InnerException: AccessViolationException } or AccessViolationException))
            {
                if (exc is TargetInvocationException && exc.InnerException != null)
                    exc = exc.InnerException;

                var output = new StringBuilder();
                output.AppendLine();
                output.AppendLine("Child exception:");
                output.AppendLine("  " + exc);
                output.AppendLine();
                output.AppendLine("Child process:");
                output.AppendLine(string.Format("  {0} {1}", assemblyName, methodkey));
                output.AppendLine();

                if (additionalArgs.Length > 0)
                {
                    output.AppendLine("Child arguments:");
                    output.AppendLine("  " + string.Join(", ", additionalArgs));
                }

                File.WriteAllText(exceptionFile, output.ToString());

                ExceptionDispatchInfo.Capture(exc).Throw();
            }
            finally
            {
                // We have seen cases where current directory holds a handle to a directory
                // for a period after RemoteExecutor exits, preventing that directory being
                // deleted. Tidy up by resetting it to the temp path.
                Directory.SetCurrentDirectory(Path.GetTempPath());
            }

            // Use Exit rather than simply returning the exit code so that we forcibly shut down
            // the process even if there are foreground threads created by the operation that would
            // end up keeping the process alive potentially indefinitely.
            try
            {
                Environment.Exit(exitCode);
            }
            catch (PlatformNotSupportedException)
            {
            }

            return exitCode;
        }

        private static T[] Subarray<T>(this T[] arr, int offset, int count)
        {
            var newArr = new T[count];
            Array.Copy(arr, offset, newArr, 0, count);
            return newArr;
        }
    }
}
