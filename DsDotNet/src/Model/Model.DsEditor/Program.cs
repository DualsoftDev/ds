using Dsu.PLC;
using Dsu.PLC.LS;
using Microsoft.FSharp.Core;
using Old.Dsu.PLC.Common;
using System;
using System.Reflection;
using System.Windows.Forms;

namespace Model.DsEditor
{
    internal static class Program
    {
        /// <summary>
        /// 해당 애플리케이션의 주 진입점입니다.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Engine.CodeGenCPU.ModuleInitializer.Initialize();


           var conn = new LsConnection(new LsConnectionParameters("192.168.0.100", new FSharpOption<ushort>(2004), TransportProtocol.Udp, 3000.0));



            var form = new FormMain();
            Application.Run(form);

        }
    }
}
