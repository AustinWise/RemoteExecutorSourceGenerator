using System;
using System.Diagnostics;
using Xunit;

namespace TestProject
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Console.WriteLine("in local process, pid: " + Process.GetCurrentProcess().Id);

            using var remote = RemoteExecutorLib.RemoteExecutor.Invoke(() =>
            {
                Console.WriteLine("In remote process, pid: " + Process.GetCurrentProcess().Id);
            });

            Console.WriteLine("started remote executor, pid: " + remote.Process.Id);
        }
    }
}