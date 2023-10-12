server:
	dotnet build
	ln -s bin/Debug/net7.0/ZeroMQ.exe

cpp-client:CppClient.cpp
	g++ CppClient.cpp -o cppClient -lzmq

fsharp-client:FSharpClient.fs
	fsc FSharpClient.fs

clean:
	rm -f cppClient
	rm -f ZeroMQ.exe