using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.FSharp.Core;
using Akka.Actor;

using Dual.Common.Akka;
using static Dual.Common.Akka.Sample.FullDuplexSampleServerSample;


namespace TestApp.Dual.Common.FS
{
    public partial class FormAkka : Form
    {
        //FullDuplexSampleServerActor
        IActorRef _serverActor;
        IActorRef _clientActor;
        ActorSystem _clientSystem;
        public FormAkka()
        {
            InitializeComponent();
        }

        private void btnLaunchServer_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                _serverActor = FullDuplexSampleServerActor.Create();
            });
        }

        private async void btnLaunchClient_Click(object sender, EventArgs e)
        {
            (_clientSystem, _clientActor) = FullDuplexSampleClientActor.Create();

            var actorPath = FullDuplexSampleServerActor.ActorPath;
            var actorSelection = _clientSystem.ActorSelection(actorPath);
            var timespan = (TimeSpan.FromSeconds(10.0));
            var serverActor = await actorSelection.ResolveOne(timespan);




            var response1 = await serverActor.Ask("Hello");
            Console.WriteLine($"============RESPONSE1: {response1}");

            var response2 = await serverActor.Ask("Hello", null);
            Console.WriteLine($"============RESPONSE2: {response2}");

            var response3 = serverActor.Inquire(new AmQuery(123, 9999));
            Console.WriteLine($"============RESPONSE2: {response3}");

            //serverActor.Tell(new AmRegisterClient(0, _clientActor));
            await serverActor.Ask(new AmRegisterClient(0, _clientActor), null);


        }

        private void FormAkka_Load(object sender, EventArgs args)
        {
            textBoxIp.Text = FullDuplexSampleServerActor.ServerIp;
            textBoxPort.Text = FullDuplexSampleServerActor.ServicePort.ToString();

            textBoxIp.TextChanged += (s, e) => { FullDuplexSampleServerActor.ServerIp = textBoxIp.Text; };
            textBoxPort.TextChanged += (s, e) =>
            {
                int port = -1;
                if (int.TryParse(textBoxPort.Text, out port))
                    FullDuplexSampleServerActor.ServicePort = port;
            };

            btnTerminateClient.Click += (s, e) =>
            {
                _clientActor.Tell(PoisonPill.Instance);
            };
        }
    }
}
