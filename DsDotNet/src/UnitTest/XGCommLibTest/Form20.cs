using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using XGCommLib;

namespace XGCommLibTest
{
    public partial class XGCommLib20Test : Form
    {
        CommObjectFactory20 factory;
        MLDPCommObject20 commObjectMLDP;
        public XGCommLib20Test()
        {
            InitializeComponent();
        }
        private void XGCommLib20Test_Load(object sender, EventArgs e)
        {
            factory = new CommObjectFactory20();
            commObjectMLDP = factory.GetMLDPCommObject20("192.168.0.111:2004") as MLDPCommObject20;
            if (0 == commObjectMLDP.Connect(""))
            {
                MessageBox.Show("failed to connect");
                return;
            }
            //commObjectMLDP = factory.GetMLDPCommObject("192.168.0.111:2004");      //
            //if (0 == commObjectMLDP.Connect(""))
            //{
            //    MessageBox.Show("failed to connect");
            //    return;
            //}
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            //CommObject commObject = factory.GetUSBCommObject("");      //

            //if (0 == commObject.Connect(""))
            //{
            //    MessageBox.Show("failed to connect");
            //    return;
            //}

            //DeviceInfo info = factory.CreateDevice();
            //info.ucDataType = (byte)'B';
            //info.ucDeviceType = (byte)'M';

            //for (int i = 0 ; i < 10 ; i++ )
            //{
            //    info.lSize = 8;
            //    info.lOffset = i * 8;

            //    commObject.AddDeviceInfo(info);
            //}

            //byte[] buf = new byte[1024];
            //buf[0] = 0xab;
            //commObject.ReadRandomDevice(buf);
        }

        private void Connect_ETH_Click(object sender, EventArgs e)
        {
            DeviceInfo info = factory.CreateDevice();
            info.ucDataType = (byte)'B';
            info.ucDeviceType = (byte)'M';

            for (int i = 0; i < 11; i++)
            {
                info.lSize = 8;
                info.lOffset = i * 8;

                commObjectMLDP.AddDeviceInfo(info);
            }

            byte[] buf = new byte[1024];
            buf[0] = 0xab;
            commObjectMLDP.ReadRandomDevice(buf);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //CommObject commObject = factory.GetXGCommObject("");      //

            //if (0 == commObject.Connect(""))
            //{
            //    MessageBox.Show("failed to connect");
            //    return;
            //}

            //DeviceInfo info = factory.CreateDevice();
            //info.ucDataType = (byte)'B';
            //info.ucDeviceType = (byte)'M';

            //for (int i = 0; i < 11; i++)
            //{
            //    info.lSize = 8;
            //    info.lOffset = i * 8;

            //    commObject.AddDeviceInfo(info);
            //}

            //byte[] buf = new byte[1024];
            //buf[0] = 0xab;
            //commObject.ReadRandomDevice(buf);
        }

        private void btnBlock_Click(object sender, EventArgs e)
        {
            var offset = 0;
            var nRead = 0;
            var rBuf = new byte[512];
            commObjectMLDP.ReadDevice_Block("M", offset, ref rBuf[0], 512, ref nRead);
            Console.WriteLine("");
        }

    }
}
