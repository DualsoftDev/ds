using DevExpress.XtraEditors;
using Dual.Common.Winform;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using static Engine.Core.ExpressionModule;

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
        public static void UpdateExpr(GridLookUpEdit gExpr, bool device)
        {
            gExpr.Do(() =>
            {
                IEnumerable<LogicStatement> css = GetLogicStatement(device);
                gExpr.Properties.DataSource = css;
            });
        }

        public static IEnumerable<LogicStatement> GetLogicStatement(bool device)
        {
            var dsCPUs =
                    device ?
                      PcControl.RunCpus.Where(w => !w.Systems.Contains(Global.ActiveSys))
                    : PcControl.RunCpus.Where(w => w.Systems.Contains(Global.ActiveSys));

            var css = dsCPUs
                        .SelectMany(c => c.CommentedStatements
                            .Select(s => new LogicStatement(s)));
            return css;
        }
    }
}


