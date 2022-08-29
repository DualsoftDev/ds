using Dsu.PLC.Common;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Dsu.PLC.LS.FS_DEMO
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var plcIp = "192.168.0.100";
            var conn = new LsConnection(new LsConnectionParameters(plcIp, new FSharpOption<ushort>(2004), TransportProtocol.Tcp, 3000.0));
            conn.PerRequestDelay = 20;
            if (conn.Connect())
            {
             

                var testBits = new List<LsTag>();
                testBits.Add((LsTag)conn.CreateTag("%MX900"));
                testBits.Add((LsTag)conn.CreateTag("%MX901"));
                conn.AddMonitoringTags(testBits);


                _ = Task.Run(async() =>
                    {
                        bool heaetBit = true;
                        while(true)
                        {
                            testBits[0].Value = heaetBit;
                            conn.WriteRandomTags(testBits.ToArray());
                            await Task.Delay(1000);
                            heaetBit = !heaetBit;
                        }
                        
                    });


                conn.Subject
                    .OfType<TagValueChangedEvent>()
                    .Subscribe(evt =>
                    {
                        var tag = (LsTag)evt.Tag;
                        //..Update 
                        Trace.WriteLine($"{tag.Name} value changed {tag.OldValue} => {tag.Value}");
                    });

                    await conn.StartDataExchangeLoopAsync();
            }
        }

    }
}
