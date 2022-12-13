namespace Engine.CodeGenHMI

open Engine.Core
open Newtonsoft.Json.Linq

[<AutoOpen>]
module CpuGenModule =
    let GenCpuCode(model:Model) = 
        let cpuCode = 
            """
            {
                "gates": [
                    {
                        "GateName": "GateAND",
                        "Out": "O1",
                        "In": ["A;B;C;D"]
                    },
                    {
                        "GateName": "GateSR",
                        "Out": "O1",
                        "In": ["A;!B;C;D", "E;F"]
                    }
                ]
            }
            """
            
        let body = JObject.Parse(cpuCode)
        { from = "cpu"; succeed = true; body = body["gates"]; error = ""; }