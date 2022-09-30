using System.Runtime.InteropServices;

namespace Engine.Parser;


/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
class EdgeListener : ListenerBase
{
    public EdgeListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
        UpdateModelSpits();
    }


    override public void EnterButtons(ButtonsContext ctx)
    {
        var first = findFirstChild<ParserRuleContext>(ctx);     // {Emergency, Auto, Start, Reset}ButtonsContext
        var targetDic =
            first switch
            {
                EmergencyButtonsContext => _system.EmergencyButtons,
                AutoButtonsContext => _system.AutoButtons,
                StartButtonsContext => _system.StartButtons,
                ResetButtonsContext => _system.ResetButtons,
                _ => throw new Exception("ERROR"),
            };

        var category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
        var key = (_system, category);
        if (ParserHelper.ButtonCategories.Contains(key))
            throw new Exception($"Duplicated button category {category} near {ctx.GetText()}");
        else
            ParserHelper.ButtonCategories.Add(key);

        var buttonDefs = enumerateChildren<ButtonDefContext>(first).ToArray();
        foreach (var bd in buttonDefs)
        {
            var buttonName = findFirstChild<ButtonNameContext>(bd).GetText();
            var flows =
                enumerateChildren<FlowNameContext>(bd)
                .Select(flowCtx => flowCtx.GetText())
                .Pipe(flowName => Verify($"Flow [{flowName}] not exists!", _system.Flows.Any(f => f.Name == flowName)))
                .Select(flowName => _system.Flows.First(f => f.Name == flowName))
                .ToArray()
                ;

            if (!targetDic.ContainsKey(buttonName))
                targetDic.Add(buttonName, new List<Flow>());

            targetDic[buttonName].AddRange(flows);
        }
    }


    public override void EnterSafetyBlock([NotNull] SafetyBlockContext ctx)
    {
        var safetyDefs = enumerateChildren<SafetyDefContext>(ctx);
        /*
         * safety block 을 parsing 해서 key / value 의 dictionary 로 저장
         * 
        [safety] = {
            Main = {P.F.Sp; P.F.Sm}
            Main2 = {P.F.Sp; P.F.Sm}
        }
        => "Main" = {"P.F.Sp"; "P.F.Sm"}
           "Main2" = {"P.F.Sp"; "P.F.Sm"}
         */
        var safetyKvs =
            from safetyDef in safetyDefs
            let key = collectNameComponents(findFirstChild(safetyDef, t => t is SafetyKeyContext))   // ["Main"] or ["My", "Flow", "Main"]
            let valueHeader = enumerateChildren<SafetyValuesContext>(safetyDef).First()
            let values = enumerateChildren<Identifier123Context>(valueHeader).Select(collectNameComponents).ToArray()
            select (key, values)
            ;


        foreach (var (key, values) in safetyKvs)
        {
            Segment seg = null;
            switch(key.Length)
            {
                case 1:
                    Assert(ctx.Parent is FlowContext);
                    seg = _model.FindGraphVertex<Segment>(AppendPathElement(key[0]));
                    break;
                case 3:
                    Assert(ctx.Parent is PropertyBlockContext);
                    seg = _model.FindGraphVertex<Segment>(key);
                    break;
                default:
                    throw new ParserException($"Invalid safety key[{key.Combine()}]", ctx);
            }

            foreach (var cond in values.Select(v => _model.FindGraphVertex(v) as Segment))
            {
                var added = seg.SafetyConditions.Add(cond);
                if (!added)
                    throw new ParserException($"Safety condition [{cond.QualifiedName}] duplicated on safety key[{key.Combine()}]", ctx);
            }
        }
    }



    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        var children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
        children.Iter((ctx, n) => Assert( n % 2 == 0 ? ctx is CausalTokensDNFContext : ctx is CausalOperatorContext));

        object findToken(CausalTokenContext ctx)
        {
            var path = AppendPathElement(collectNameComponents(ctx));
            if (path.Length == 5)
                path = AppendPathElement(collectNameComponents(ctx).Combine());
            var matches =
                _modelSpits
                .Where(spit => spit.NameComponents.IsStringArrayEqaul(path))
                ;
            var token = 
                matches
                .Where(spit =>spit.Obj is SegmentBase || spit.Obj is Child)
                .Select(spit => spit.Obj)
                .FirstOrDefault();
                ;
            Assert(token != null);
            return token;
        }
        for (int i = 0; i < children.Length - 2; i+=2)
        {
            var lefts = enumerateChildren<CausalTokenContext>(children[i]);         // CausalTokensCNFContext
            var op = children[i + 1].GetText();
            var rights = enumerateChildren<CausalTokenContext>(children[i+2]);

            foreach (var left in lefts)
            {
                foreach(var right in rights)
                {
                    var l = findToken(left);
                    var r = findToken(right);
                    if (l == null)
                        throw new ParserException($"ERROR: failed to find [{left.GetText()}]", ctx);
                    if (r == null)
                        throw new ParserException($"ERROR: failed to find [{right.GetText()}]", ctx);

                    if (_parenting == null)
                        _rootFlow.CreateEdges(l as SegmentBase, r as SegmentBase, op);
                    else
                        _parenting.CreateEdges(l as Child, r as Child, op);
                }
            }
            Console.WriteLine();
        }
    }

}
