using RemoteExecutorLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public partial class GlobalNamespaceTestCases
{
    [RemotelyInvokable]
    static void MyRemoteMethod(string arg)
    {
    }

    [RemotelyInvokable]
    static int TwoArgs(string arg, string arg2)
    {
        Console.WriteLine($"in remote pid {System.Diagnostics.Process.GetCurrentProcess().Id}, got two args: {arg}, {arg2}");
        return 1;
    }

    [Fact]
    public void Test1()
    {
        using var remote = InvokeTwoArgs("a", "b", new RemoteInvokeOptions() { ExpectedExitCode = 1 });
        Assert.True(true);
    }
}

