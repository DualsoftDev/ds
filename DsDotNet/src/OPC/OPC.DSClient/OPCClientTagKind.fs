namespace OPC.DSClient

open System
open Engine.Core

[<AutoOpen>]
module OPCClientTagHelper =
    let dicTag = TagKindModule.allTagKinds 
    let opcList = 
        [
            (int)VertexTag.planStart         
            (int)VertexTag.planEnd           
            (int)VertexTag.motionStart       
            (int)VertexTag.motionEnd         
            (int)VertexTag.scriptStart       
            (int)VertexTag.scriptEnd         
            (int)VertexTag.realToken         
            (int)VertexTag.origin         
            (int)VertexTag.ready             
            (int)VertexTag.going             
            (int)VertexTag.finish            
            (int)VertexTag.homing            
            (int)VertexTag.callIn            
            (int)VertexTag.callOut           
            (int)VertexTag.txErrOnTimeUnder  
            (int)VertexTag.txErrOnTimeOver   
            (int)VertexTag.txErrOffTimeUnder 
            (int)VertexTag.txErrOffTimeOver  
            (int)VertexTag.rxErrShort        
            (int)VertexTag.rxErrOpen         
            (int)VertexTag.rxErrInterlock    
            (int)VertexTag.workErrOriginGoing
            (int)VertexTag.errorWork         
            (int)FlowTag.idle_mode           
            (int)FlowTag.test_state          
            (int)FlowTag.drive_state         
            (int)FlowTag.error_state         
            (int)FlowTag.pause_state         
            (int)FlowTag.emergency_state     
            (int)FlowTag.going_state         
            (int)FlowTag.ready_state         

            (int)SystemTag.autoMonitor      
            (int)SystemTag.manualMonitor    
            (int)SystemTag.driveMonitor     
            (int)SystemTag.pauseMonitor     
            (int)SystemTag.emergencyMonitor       
            (int)SystemTag.testMonitor      
            (int)SystemTag.readyMonitor     
            (int)SystemTag.originMonitor      

            (int)TaskDevTag.actionIn         
            (int)TaskDevTag.actionOut      
        ]

    let OPCClientTagKinds =
        opcList |> List.map (fun x -> (dicTag.[x], x)) |> dict

    let tryParseOPCClientTagKind (input: string) : TagKind option =
        if OPCClientTagKinds.ContainsKey(input)
        then 
            Some OPCClientTagKinds[input]
        else
            None
