using log4net.Appender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using DevExpress.XtraEditors;
using static Dsu.PLCConverter.FS.XgiBaseXML;
using System.IO;
using System.Xml.Serialization;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiConfigModule;
using static Dsu.PLCConverter.FS.XgiSpecs.XgiOptionModule;
using Dsu.PLCConverter.FS;
using Dsu.PLCConverter.UI;

namespace MelsecConverter
{
    public partial class FormAddressMapper
        : DevExpress.XtraBars.FluentDesignSystem.FluentDesignForm
        , IAppender
    {

        private void OpenPouOrCommentCSV()
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "CSV file(*.csv)|*.csv|All files(*.*)|*.*";
            ofd.Multiselect = true;
            var result = ofd.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                _PouCommentPaths = ofd.FileNames.ToList();
                SavePathsToRegistry(_PouCommentPaths);
            }
        }



    }
}