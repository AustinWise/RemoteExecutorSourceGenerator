using RemoteExecutorLib;
using Xunit;

namespace TestCasesNamespace
{
    public partial class TestCase
    {
        [RemotelyInvokable]
        static void MyRemoteMethod()
        {

        }

        [Fact]
        public void Test1()
        {
            Assert.True(true);
        }
    }
}