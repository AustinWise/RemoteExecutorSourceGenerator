using RemoteExecutorLib;
using System;

namespace RemoteExecutorExe
{
    internal class Program
    {
        static int Main(string[] args)
        {
            int? maybeExitCode = RemoteExecutorLib.Program.TryExecute(args);
            if (maybeExitCode.HasValue)
            {
                return maybeExitCode.Value;
            }

            // we should not get here
            Console.Error.WriteLine("Remote executor EXE started, but missing magic argument: " + RemoteExecutor.REMOTE_EXECUTOR_MARKER_ARG);
            return -1;
        }
    }
}