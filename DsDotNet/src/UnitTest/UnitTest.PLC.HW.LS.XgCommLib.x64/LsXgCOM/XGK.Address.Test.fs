#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open NUnit.Framework
open AddressConvert
open Engine.Core.CoreModule
open Dual.Common.Core.FS
open System.Text.RegularExpressions

type XgkAddressTest() =
    inherit TestBaseClass("HWPLCLogger")




