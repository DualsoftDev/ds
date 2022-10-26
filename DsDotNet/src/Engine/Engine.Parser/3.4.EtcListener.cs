namespace Engine.Parser;

using System.Runtime.Remoting.Contexts;

using static Engine.Core.CodeElements;

/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
class EtcListener : ListenerBase
{
    public EtcListener(dsParser parser, ParserHelper helper)
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
                AutoButtonsContext      => _system.AutoButtons,
                StartButtonsContext     => _system.StartButtons,
                ResetButtonsContext     => _system.ResetButtons,
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
                .Tap(flowName => Verify($"Flow [{flowName}] not exists!", _system.Flows.Any(f => f.Name == flowName)))
                .Select(flowName => _system.Flows.First(f => f.Name == flowName))
                .ToArray()
                ;

            if (!targetDic.ContainsKey(buttonName))
                targetDic.Add(buttonName, new List<Flow>());

            targetDic[buttonName].AddRange(flows);
        }
    }


    public override void EnterSafety([NotNull] SafetyContext ctx)
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
            let key         = collectNameComponents(findFirstChild(safetyDef, t => t is SafetyKeyContext))   // ["Main"] or ["My", "Flow", "Main"]
            let valueHeader = enumerateChildren<SafetyValuesContext>(safetyDef).First()
            let values      = enumerateChildren<Identifier123Context>(valueHeader).Select(collectNameComponents).ToArray()
            select (key, values)
            ;


        foreach (var (key, values) in safetyKvs)
        {
            Real seg = null;
            switch (key.Length)
            {
                case 1:
                    Assert(ctx.Parent is FlowContext);
                    seg = _model.FindGraphVertex<Real>(AppendPathElement(key[0]));
                    break;
                case 3:
                    Assert(ctx.Parent is PropertyBlockContext);
                    seg = _model.FindGraphVertex<Real>(key);
                    break;
                default:
                    throw new ParserException($"Invalid safety key[{key.Combine()}]", ctx);
            }

            foreach (var cond in values.Select(v => _model.FindGraphVertex(v) as Real))
            {
                var added = seg.SafetyConditions.Add(cond);
                if (!added)
                    throw new ParserException($"Safety condition [{cond.QualifiedName}] duplicated on safety key[{key.Combine()}]", ctx);
            }
        }
    }


    FunctionApplication CreateFunctionApplication(FunApplicationContext context)
    {
        var funName = findFirstChild<FunNameContext>(context).GetText();
        var argGroups =
            enumerateChildren<ArgumentGroupContext>(context)
            .Select(argGrpCtx =>
                enumerateChildren<ArgumentContext>(argGrpCtx)
                .Select(arg => arg.GetText())
                .ToArray()
                )
            .ToArray()
            ;
        var funAppl = new FunctionApplication(funName, argGroups);
        return funAppl;
    }

    override public void EnterVariableDef(VariableDefContext context)
    {
        var varName = findFirstChild<VarNameContext>(context).GetText();
        var varType = findFirstChild<VarTypeContext>(context).GetText();
        var init    = findFirstChild<ArgumentContext>(context).GetText();
        _model.Variables.Add(new Variable(varName, varType, init));
    }
    override public void EnterCommandDef(CommandDefContext context)
    {
        var cmdName    = findFirstChild<CmdNameContext>(context).GetText();
        var funApplCtx = findFirstChild<FunApplicationContext>(context);
        var funAppl    = CreateFunctionApplication(funApplCtx);
        var command    = new Command(cmdName, funAppl);
        _model.Commands.Add(command);
    }
    override public void EnterObserveDef(ObserveDefContext context)
    {
        var obsName    = findFirstChild<ObserveNameContext>(context).GetText();
        var funApplCtx = findFirstChild<FunApplicationContext>(context);
        var funAppl    = CreateFunctionApplication(funApplCtx);
        var observes   = new Observe(obsName, funAppl);
        _model.Observes.Add(observes);
    }


    override public void ExitModel(ModelContext ctx)
    {
        UpdateModelSpits();
        //[layouts] = {
        //       L.T.Cp = (30, 50)            // xy
        //       L.T.Cm = (60, 50, 20, 20)    // xywh
        //}

        var layouts = enumerateChildren<LayoutsContext>(ctx).ToArray();
        if (layouts.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var positionDefs = enumerateChildren<PositionDefContext>(ctx).ToArray();
        foreach (var posiDef in positionDefs)
        {
            var apiPath = collectNameComponents(posiDef.apiPath());
            var apiItem = _model.FindApiItem(apiPath);
            var xywh = posiDef.xywh();
            var (x, y, w, h) = (xywh.x().GetText(), xywh.y().GetText(), xywh.w()?.GetText(), xywh.h()?.GetText());
            apiItem.Xywh = new Xywh(int.Parse(x), int.Parse(y), w == null ? null : int.Parse(w), h == null ? null : int.Parse(h));
        }

        //[addresses] = {
        //    A.F.Am = (%Q123.23, %I12.1);        // FQSegmentName = (Start, Reset) Tag address
        //    A.F.Ap = (%Q123.24, %I12.2);
        //    B.F.Bm = (%Q123.25, %I12.3);
        //    B.F.Bp = (%Q123.26, %I12.4);
        //}
        var addresses = enumerateChildren<AddressesContext>(ctx).ToArray();
        if (addresses.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx);

        var addressDefs = enumerateChildren<AddressDefContext>(ctx).ToArray();
        //<<kwak>> help
        foreach (var addrDef in addressDefs)
        {
            var segNs = collectNameComponents(addrDef.segmentPath());
            var call =
                _modelSpits
                .Where(o => o.GetCore() is Call && o.NameComponents.IsStringArrayEqaul(segNs))
                .FirstOrDefault();

            var callCore = call.GetCore() as Call;
            var sre = addrDef.address();
            var (s, e) = (sre.startItem()?.GetText(), sre.endItem()?.GetText());
            callCore.Addresses = new Addresses(s, e);
        }
        //<<kwak>> help

        foreach (var alias in _modelSpits.Where(o => o.GetCore() is Alias))
        {
            var al = alias.GetCore() as Alias;
            var targetSys = _model.FindSystem(al.AliasKey[0]);
            if (targetSys != al.Parent.System)
                al.SetTarget(_model.FindCall(al.AliasKey));
            else
                al.SetTarget(_model.FindGraphVertex(al.AliasKey) as Real);
        }
    }
}