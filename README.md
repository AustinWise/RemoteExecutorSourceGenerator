
An tweak of the
[RemoteExecutor](https://github.com/dotnet/arcade/tree/main/src/Microsoft.DotNet.RemoteExecutor/src)
used in the dotnet/runtime unit tests.

There is also a goal to make this all work for all of these app models:

* .NET Framework: executor is an EXE that is directly executable by the OS
* .NET Core: use the app host (dotnet.exe) to load and execute the executor DLL
* SingleFile: the .NET Core model, but packaged into a single EXE
* NativeAOT: compiled ahead-of-time into a single native EXE

The main problem the existing version of this library presents is its duel nature
as both a library and EXE. The single file builder does not elegantly handle having
multiple EXEs packaged together. And both single-file and NativeAOT don't have good
ways to invoke a seperate EXE entry point.

All this repo does is move the EXE entry point to a separate assembly. The remote
executor library exposes an entrypoint that this stub EXE can all. The
[SingleFileTestRunner](https://github.com/dotnet/runtime/blob/385e1bafaa307736541c6024ab29008f98f400f8/src/libraries/Common/tests/SingleFileTestRunner/SingleFileTestRunner.cs)
in dotnet/runtime is augmented to also support running as the remote executor
host by calling in to the library.
