namespace Engine.CodeGen

[<AutoOpen>]
module CodeGen =
    type Initializer = {
            from:string;
            succeed:bool;
            body:string;
            error:string;
        }