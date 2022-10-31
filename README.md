
An alternate take on the
[RemoteExecutor](https://github.com/dotnet/arcade/tree/main/src/Microsoft.DotNet.RemoteExecutor/src)
used in the dotnet/runtime unit tests. Instead of using reflection, it uses a
source generator to register available remote method and to expose strongly-typed
wrapper for executing them.

There is also a goal to make this all work for all of these app models:

* .NET Framework: executor is an EXE that is directly executable by the OS
* .NET Core: use the app host (dotnet.exe) to load and execute the executor DLL
* SingleFile: the .NET Core model, but packaged into a single EXE
* NativeAOT: compiled ahead-of-time into a single native EXE

Currently, it does work across all of those models. But you have to set the
`OutputType` of RemoteExecutorLib to `Exe` to work with the first two.
