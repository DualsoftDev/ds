using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using Dual.Common.Winform;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Engine.Core.ExpressionModule;
using static Engine.Cpu.RunTime;

namespace DSModeler.Tree
{
    public class LogicStatement
    {
        CommentedStatement _cs;
        public LogicStatement(CommentedStatement cs) { _cs = cs; }
        public CommentedStatement GetCommentedStatement() => _cs;

        public string TargetName => _cs.TargetName;

        public string TargetValue => _cs.TargetValue.ToString();
        [DisplayName("Comment"), Editable(false)]
        public string Comment => _cs.comment;
        [Browsable(false)]
        public string Display => $"{_cs.TargetName} : {_cs.TargetValue}";
    }

    public static class LogicTree
    {
        public static void InitControl(GridLookUpEdit gle, GridView gv)
        {

            gle.Properties.DisplayMember = "Display";

            gv.PreviewLineCount = 20;
            gv.OptionsSelection.EnableAppearanceFocusedCell = false;
            gv.OptionsView.ShowAutoFilterRow = true;
            gv.OptionsView.ShowGroupPanel = false;
        }

        public static void CreateRungExprCombobox(DsCPU dsCPU
        , FormMain formMain
        , DevExpress.XtraBars.Docking2010.Views.Tabbed.TabbedView tabbedView1
        , GridLookUpEdit gridLookUpEdit_Expr)
        {


            formMain.Do(() =>
            {
                var css = dsCPU.CommentedStatements.Select(s => new LogicStatement(s));
                gridLookUpEdit_Expr.Properties.DataSource = css;
            });
        }
    }
}


