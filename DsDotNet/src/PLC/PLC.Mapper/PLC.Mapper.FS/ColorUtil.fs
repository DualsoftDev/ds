namespace PLC.Mapper.FS

open System
open System.Drawing
open System.Collections.Generic

module ColorUtilModule =

   

    let hsvToColor (h: float) (s: float) (v: float) : Color =
        let hi = int (Math.Floor(h / 60.0)) % 6
        let f = h / 60.0 - Math.Floor(h / 60.0)
        let v = v * 255.0
        let p = v * (1.0 - s)
        let q = v * (1.0 - f * s)
        let t = v * (1.0 - (1.0 - f) * s)

        let r, g, b =
            match hi with
            | 0 -> v, t, p
            | 1 -> q, v, p
            | 2 -> p, v, t
            | 3 -> p, q, v
            | 4 -> t, p, v
            | _ -> v, p, q

        Color.FromArgb(255, int r, int g, int b)
