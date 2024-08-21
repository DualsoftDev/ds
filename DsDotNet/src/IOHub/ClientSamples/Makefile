server:
	dotnet build
	ln -s bin/Debug/net8.0/ZeroMQ.exe

# apt-get update
# apt-get install build-essential gdb
# apt install libzmq3-dev
cpp-client:CppClient.cpp
	g++ CppClient.cpp -o cppClient -lzmq

fsharp-client:FSharpClient.fs
	fsc FSharpClient.fs

clean:
	rm -f cppClient
	rm -f ZeroMQ.exe
