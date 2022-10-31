namespace Engine.Parser.FS
using static Engine.Core.CodeElements

/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
class EtcListener : ListenerBase
{
    public EtcListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
        UpdateModelSpits()
    }

    override public void EnterButtons(ButtonsContext ctx)
    {
        let first = findFirstChild<ParserRuleContext>(ctx);     // {Emergency, Auto, Start, Reset}ButtonsContext
        let targetDic =
            first switch
            {
                EmergencyButtonsContext => _system.EmergencyButtons,
                AutoButtonsContext      => _system.AutoButtons,
                StartButtonsContext     => _system.StartButtons,
                ResetButtonsContext     => _system.ResetButtons,
                _ => throw new Exception("ERROR"),
            }

        let category = first.GetChild(1).GetText();       // [| '[', category, ']', buttonBlock |] 에서 category 만 추려냄 (e.g 'emg')
        let key = (_system, category)
        if (ParserHelper.ButtonCategories.Contains(key))
            throw new Exception($"Duplicated button category {category} near {ctx.GetText()}")
        else
            ParserHelper.ButtonCategories.Add(key)

        let buttonDefs = enumerateChildren<ButtonDefContext>(first).ToArray()
        foreach (let bd in buttonDefs)
        {
            let buttonName = findFirstChild<ButtonNameContext>(bd).GetText()
            let flows =
                enumerateChildren<FlowNameContext>(bd)
                .Select(flowCtx => flowCtx.GetText())
                .Tap(flowName => Verify($"Flow [{flowName}] not exists!", _system.Flows.Any(f => f.Name == flowName)))
                .Select(flowName => _system.Flows.First(f => f.Name == flowName))
                .ToArray()
                

            if (!targetDic.ContainsKey(buttonName))
                targetDic.Add(buttonName, new ResizeArray<Flow>())

            targetDic[buttonName].AddRange(flows)
        }
    }


    public override void EnterSafety([NotNull] SafetyContext ctx)
    {
        let safetyDefs = enumerateChildren<SafetyDefContext>(ctx)
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
        let safetyKvs =
            from safetyDef in safetyDefs
            let key         = collectNameComponents(findFirstChild(safetyDef, t => t is SafetyKeyContext))   // ["Main"] or ["My", "Flow", "Main"]
            let valueHeader = enumerateChildren<SafetyValuesContext>(safetyDef).First()
            let values      = enumerateChildren<Identifier123Context>(valueHeader).Select(collectNameComponents).ToArray()
            select (key, values)
            


        foreach (let (key, values) in safetyKvs)
        {
            Real seg = null
            switch (key.Length)
            {
                case 1:
                    Assert(ctx.Parent is FlowContext)
                    seg = _model.FindGraphVertex<Real>(AppendPathElement(key[0]))
                    break
                case 3:
                    Assert(ctx.Parent is ModelPropertyBlockContext)
                    seg = _model.FindGraphVertex<Real>(key)
                    break
                default:
                    throw new ParserException($"Invalid safety key[{key.Combine()}]", ctx)
            }

            foreach (let cond in values.Select(v => _model.FindGraphVertex(v) as Real))
            {
                let added = seg.SafetyConditions.Add(cond)
                if (!added)
                    throw new ParserException($"Safety condition [{cond.QualifiedName}] duplicated on safety key[{key.Combine()}]", ctx)
            }
        }
    }


    FunctionApplication CreateFunctionApplication(FunApplicationContext context)
    {
        let funName = findFirstChild<FunNameContext>(context).GetText()
        let argGroups =
            enumerateChildren<ArgumentGroupContext>(context)
            .Select(argGrpCtx =>
                enumerateChildren<ArgumentContext>(argGrpCtx)
                .Select(arg => arg.GetText())
                .ToArray()
                )
            .ToArray()
            
        let funAppl = new FunctionApplication(funName, argGroups)
        return funAppl
    }

    override public void EnterVariableDef(VariableDefContext context)
    {
        let varName = findFirstChild<VarNameContext>(context).GetText()
        let varType = findFirstChild<VarTypeContext>(context).GetText()
        let init    = findFirstChild<ArgumentContext>(context).GetText()
        _model.Variables.Add(new Variable(varName, varType, init))
    }
    override public void EnterCommandDef(CommandDefContext context)
    {
        let cmdName    = findFirstChild<CmdNameContext>(context).GetText()
        let funApplCtx = findFirstChild<FunApplicationContext>(context)
        let funAppl    = CreateFunctionApplication(funApplCtx)
        let command    = new Command(cmdName, funAppl)
        _model.Commands.Add(command)
    }
    override public void EnterObserveDef(ObserveDefContext context)
    {
        let obsName    = findFirstChild<ObserveNameContext>(context).GetText()
        let funApplCtx = findFirstChild<FunApplicationContext>(context)
        let funAppl    = CreateFunctionApplication(funApplCtx)
        let observes   = new Observe(obsName, funAppl)
        _model.Observes.Add(observes)
    }


    override public void ExitModel(ModelContext ctx)
    {
        UpdateModelSpits()
        //[layouts] = {
        //       L.T.Cp = (30, 50)            // xy
        //       L.T.Cm = (60, 50, 20, 20)    // xywh
        //}

        let layouts = enumerateChildren<LayoutsContext>(ctx).ToArray()
        if (layouts.Length > 1)
            throw new ParserException("Layouts block should exist only once", ctx)

        let positionDefs = enumerateChildren<PositionDefContext>(ctx).ToArray()
        foreach (let posiDef in positionDefs)
        {
            let apiPath = collectNameComponents(posiDef.apiPath())
            let apiItem = _model.FindApiItem(apiPath)
            let xywh = posiDef.xywh()
            let (x, y, w, h) = (xywh.x().GetText(), xywh.y().GetText(), xywh.w()?.GetText(), xywh.h()?.GetText())
            apiItem.Xywh = new Xywh(int.Parse(x), int.Parse(y), w == null ? null : int.Parse(w), h == null ? null : int.Parse(h))
        }

        /*
            [sys] / [prop] / [addresses] = {
                ApiName = (Start, End) Tag address
                A."" + "" = (% Q1234.2343, % I1234.2343)
                A."" - "" = (START, END)
            }
        */
        let api2Address = (
            from sysCtx in enumerateChildren<SystemContext>(ctx)
            from addrDefCtx in enumerateChildren<AddressDefContext>(sysCtx)
            let apiPath = collectNameComponents(addrDefCtx.apiPath())
            let sre = addrDefCtx.address()
            let s = sre.startItem()?.GetText()
            let e = sre.endItem()?.GetText()
            select (sysCtx, apiPath, new Addresses(s, e))
        ).ToArray()

        foreach (let (sysCtx, apiPath, address) in api2Address)
        {
            let sys = _model.FindSystem(sysCtx.systemName().GetText())
            sys.ApiAddressMap.Add(apiPath, address)
        }

        foreach (let o in _modelSpits)
        {
            if (o.GetCore() is Call call)
            {
                let map = call.GetSystem().ApiAddressMap
                if (map.ContainsKey(call.NameComponents))
                {
                    let address = map[call.NameComponents]
                    Assert(call.Addresses == null || call.Addresses == address)
                    call.Addresses = address
                }
            }
        }
    }
}