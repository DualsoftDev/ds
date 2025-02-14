using DevExpress.LookAndFeel;
using DevExpress.LookAndFeel.Design;
using DevExpress.Skins;
using DevExpress.Skins.Info;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dual.Common.DevExpressLib
{
    public partial class FormSkinSelect : Form
    {
        static public bool _bCustom = false; // my + SkinStyle ex) my Office2019Black
        static string _RegSkinName = "SkinName";
        static string _RegStartingShowSkin = "StartingShowSkin";

        static RegistryKey GetRegistryKey()
        {
            string RegPath = $@"Dualsoft\UI\{System.Reflection.Assembly.GetEntryAssembly().GetName().Name}";
            RegistryKey rkeyOpen = Registry.CurrentUser.OpenSubKey(RegPath, true);

            if (rkeyOpen == null)
                rkeyOpen = Registry.CurrentUser.CreateSubKey(RegPath, true);

            return rkeyOpen;
        }

        static public bool IsInitSkinShow()
        {
            string StartingOpen = GetRegistryKey().GetValue(_RegStartingShowSkin, "False").ToString();
            return Convert.ToBoolean(StartingOpen);
        }
        static public void SetRegistedSkin()
        {
            string skin = GetRegistryKey().GetValue(_RegSkinName, _SkinStyleWhite).ToString();
            if (_bCustom)
                UserLookAndFeel.Default.SetSkinStyle("my " + skin);
            else
                UserLookAndFeel.Default.SetSkinStyle(GetSkin(skin));
        }


        static string _SkinStyleWhite = "Bezier";
        static string _SkinStyleBlack = "Office2019Black";

        static SkinStyle GetSkin(string skinName)
        {
            SkinStyle select;

            switch (skinName)
            {
                case "Basic": select = SkinStyle.Basic; break;
                case "Office2019Black": select = SkinStyle.Office2019Black; break;
                case "Bezier": select = SkinStyle.Bezier; break;

                default: select = SkinStyle.Bezier; break;
            }

            return select;
        }

        public FormSkinSelect()
        {
            InitializeComponent();
        }
        private void FormSkinSelect_Load(object sender, EventArgs e)
        {
            string skin = GetRegistryKey().GetValue(_RegSkinName, _SkinStyleWhite).ToString();

            if (skin == _SkinStyleWhite)
                radioButton_White.Checked = true;
            else
                radioButton_Black.Checked = true;


            string StartingOpen = GetRegistryKey().GetValue(_RegStartingShowSkin, "False").ToString();
            checkEdit_Showing.Checked = Convert.ToBoolean(StartingOpen);
        }

        private void simpleButton_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void radioButton_White_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_White.Checked)
            {
                if (UserLookAndFeel.Default.SkinName != _SkinStyleWhite)
                {
                    if (_bCustom)
                        UserLookAndFeel.Default.SetSkinStyle("my " + _SkinStyleWhite.ToLower());
                    else
                        UserLookAndFeel.Default.SetSkinStyle(GetSkin(_SkinStyleWhite));
                }
                GetRegistryKey().SetValue(_RegSkinName, _SkinStyleWhite);
            }
        }

        private void radioButton_Black_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton_Black.Checked)
            {
                if (UserLookAndFeel.Default.SkinName != _SkinStyleBlack)
                    if (_bCustom)
                        UserLookAndFeel.Default.SetSkinStyle("my " + _SkinStyleBlack.ToLower());
                    else
                        UserLookAndFeel.Default.SetSkinStyle(GetSkin(_SkinStyleBlack));
                GetRegistryKey().SetValue(_RegSkinName, _SkinStyleBlack);
            }
        }

        private void checkEdit_Showing_CheckedChanged(object sender, EventArgs e)
        {
            GetRegistryKey().SetValue(_RegStartingShowSkin, checkEdit_Showing.Checked);
        }

    }
}
