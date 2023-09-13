using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DSModeler.Tree;

[SupportedOSPlatform("windows")]
public class LogicStatement
{
    private readonly CommentedStatement _cs;
    public LogicStatement(CommentedStatement cs) { _cs = cs; }
    public CommentedStatement GetCommentedStatement()
    {
        return _cs;
    }

    public string TargetName => _cs.TargetName;

    public string TargetValue => _cs.TargetValue.ToString();
    [DisplayName("Comment"), Editable(false)]
    public string Comment => _cs.comment;
    [Browsable(false)]
    public string Display => $"{_cs.TargetName} : {_cs.TargetValue}";
}
[SupportedOSPlatform("windows")]
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
        IEnumerable<Engine.Cpu.RunTime.DsCPU> dsCPUs =
                device ?
                  PcContr.RunCpus.Where(w => !w.Systems.Contains(Global.ActiveSys))
                : PcContr.RunCpus.Where(w => w.Systems.Contains(Global.ActiveSys));

        IEnumerable<LogicStatement> css = dsCPUs
                    .SelectMany(c => c.CommentedStatements
                        .Select(s => new LogicStatement(s)));
        return css;
    }
}