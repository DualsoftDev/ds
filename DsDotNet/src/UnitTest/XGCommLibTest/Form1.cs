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
    public partial class XGCommLibTest : Form
    {
        public XGCommLibTest()
        {
            InitializeComponent();
        }

        private void Connect_Click(object sender, EventArgs e)
        {
            CommObjectFactory factory = new CommObjectFactory();
            CommObject commObject = factory.GetUSBCommObject("");      //

            if (0 == commObject.Connect(""))
            {
                MessageBox.Show("failed to connect");
                return;
            }

            DeviceInfo info = factory.CreateDevice();
            info.ucDataType = (byte)'B';
            info.ucDeviceType = (byte)'M';

            for (int i = 0 ; i < 10 ; i++ )
            {
                info.lSize = 8;
                info.lOffset = i * 8;

                commObject.AddDeviceInfo(info);
            }

            byte[] buf = new byte[1024];
            buf[0] = 0xab;
            commObject.ReadRandomDevice(buf);
        }

        private void Connect_ETH_Click(object sender, EventArgs e)
        {
            CommObjectFactory factory = new CommObjectFactory();
            CommObject commObject = factory.GetMLDPCommObject("192.168.0.111:2004");      //

            if (0 == commObject.Connect(""))
            {
                MessageBox.Show("failed to connect");
                return;
            }

            DeviceInfo info = factory.CreateDevice();
            info.ucDataType = (byte)'B';
            info.ucDeviceType = (byte)'M';

            for (int i = 0; i < 11; i++)
            {
                info.lSize = 8;
                info.lOffset = i * 8;

                commObject.AddDeviceInfo(info);
            }

            byte[] buf = new byte[1024];
            buf[0] = 0xab;
            commObject.ReadRandomDevice(buf);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CommObjectFactory factory = new CommObjectFactory();
            CommObject commObject = factory.GetXGCommObject("");      //

            if (0 == commObject.Connect(""))
            {
                MessageBox.Show("failed to connect");
                return;
            }

            DeviceInfo info = factory.CreateDevice();
            info.ucDataType = (byte)'B';
            info.ucDeviceType = (byte)'M';

            for (int i = 0; i < 11; i++)
            {
                info.lSize = 8;
                info.lOffset = i * 8;

                commObject.AddDeviceInfo(info);
            }

            byte[] buf = new byte[1024];
            buf[0] = 0xab;
            commObject.ReadRandomDevice(buf);
        }
    }
}
