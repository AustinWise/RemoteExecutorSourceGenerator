using RemoteExecutorLib;
using Xunit;

namespace TestCasesNamespace
{
    public partial class TestCase
    {
        [RemotelyInvokable]
        static void MyRemoteMethod()
        {
            Console.WriteLine("In remote process: " + System.Diagnostics.Process.GetCurrentProcess().Id);
        }

        [Fact]
        public void Test1()
        {
            using (RemoteInvokeHandle remote = InvokeMyRemoteMethod())
            {
                Console.WriteLine("In main process, my id: " + System.Diagnostics.Process.GetCurrentProcess().Id);
                Console.WriteLine("Remote id: " + remote.Process.Id);
            }
            Assert.True(true);
        }
    }
}