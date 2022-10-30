using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteExecutorLib
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RemotelyInvokableAttribute : Attribute
    {
        // TODO: maybe duplicate RemoteInvokeOptions's properties here?
    }
}
