// See https://aka.ms/new-console-template for more information
using AppApi;

Console.WriteLine("Web-Engine communication server start");
SignalR<SampleHub>.Configure("/samplehub");
