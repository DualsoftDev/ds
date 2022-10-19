using static Engine.Core.GraphModule;

namespace Engine.Parser;


/// <summary>
/// Alias map 및 Api map 가 생성된 이후의 처리
/// </summary>
class ElementListener : ListenerBase
{
    public ElementListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
    }


    override public void EnterParenting(ParentingContext ctx)
    {
        base.EnterParenting(ctx);
    }

    public override void EnterInterfaceDef(InterfaceDefContext ctx)
    {
        var hash = _system.ApiItems;
        var interrfaceNameCtx = findFirstChild<InterfaceNameContext>(ctx);
        var interfaceName = collectNameComponents(interrfaceNameCtx)[0];
        string[][] collectCallComponents(CallComponentsContext ctx) =>
            enumerateChildren<Identifier123Context>(ctx)
                .Select(collectNameComponents)
                .ToArray()
                ;
        bool isWildcard(string[] cc) => cc.Length == 1 && cc[0] == "_";
        Real[] findSegments(string[][] fqdns) =>
            fqdns
            .Where(fqdn => fqdn != null)
            .Select(s => _model.FindGraphVertex<Real>(s))
            .Tap(x => Assert(x != null))
            .ToArray()
            ;
        var ser =   // { start ~ end ~ reset }
            enumerateChildren<CallComponentsContext>(ctx)
            .Select(collectCallComponents)
            .Tap(callComponents => Assert(callComponents.ForAll(cc => cc.Length == 2 || isWildcard(cc))))
            .Select(callCompnents => callCompnents.Select(cc => isWildcard(cc) ? null : cc.Prepend(_system.Name).ToArray()).ToArray())
            .ToArray()
            ;
        var item = hash.First(it => it.Name == interfaceName);
        var n = ser.Length;

        Assert(n == 2 || n == 3);
        item.AddTXs(findSegments(ser[0]));
        item.AddRXs(findSegments(ser[1]));

        Console.WriteLine();
    }
    override public void EnterCausalToken(CausalTokenContext ctx)
    {
        var ns = collectNameComponents(ctx);
        Assert(ns.Length.IsOneOf(1, 2));

        var path = AppendPathElement(ns);

        var existing = _modelSpits.Where(spit => spit.NameComponents.IsStringArrayEqaul(path)).ToArray();
        if (existing.Where(spit => spit.Obj is Vertex).Any())
            return;

        var pathWithoutParenting = new[] { _system.Name, _rootFlow.Name }.Concat(ns).ToArray();

        var matches =
            _modelSpits
            .Where(spitResult =>
                Enumerable.SequenceEqual(spitResult.NameComponents, path)
                || Enumerable.SequenceEqual(spitResult.NameComponents, pathWithoutParenting))
            .Select(spitResult => spitResult.Obj)
            .ToArray()
            ;
        if (matches.OfType<Real>().Any())
            return;

        try
        {
            var alias = matches.OfType<SpitObjAlias>().FirstOrDefault();
            if (alias != null)
            {
                var aliasKey =
                    matches
                        .OfType<SpitObjAlias>()
                        .Where(alias => alias.Mnemonic.IsStringArrayEqaul(pathWithoutParenting))
                        .Select(alias => alias.Key)
                        .FirstOrDefault()
                        ;


                switch(aliasKey.Length)
                {
                    case 2:
                        var apiItem =
                            _modelSpitObjects
                                .OfType<ApiItem>()
                                .Where(api => api.NameComponents.IsStringArrayEqaul(aliasKey))
                                .FirstOrDefault();
                        Assert(apiItem != null);

                        if (_parenting == null)
                            Alias.CreateInFlow(ns.Combine(), aliasKey, _rootFlow);
                        else 
                            Alias.CreateInReal(ns.Combine(), apiItem, _parenting);
                        return;
                    case 1:
                        Assert(false);
                        break;
                }
            }

            var apiCall =
                _modelSpitObjects
                    .OfType<ApiItem>()
                    .Where(api => api.NameComponents.IsStringArrayEqaul(ns))
                    .FirstOrDefault();

            if (apiCall != null)
            {
                if (_parenting == null)
                    Call.CreateInFlow(apiCall, _rootFlow);
                else
                    Call.CreateInReal(apiCall, _parenting);
                return;
            }


            var prop = _elements[path];
            if (_parenting == null)
            {
                if(ns.Length != 1)
                    throw new ParserException($"ERROR: unknown token [{ns.Combine()}].", ctx);
                Real.Create(ns[0], _rootFlow);
                return;
            }
            else
                throw new ParserException($"ERROR: unknown token [{ns.Combine()}].", ctx);
        }
        finally
        {
            UpdateModelSpits();
        }

    }

    override public void EnterIdentifier12Listing(Identifier12ListingContext ctx)
    {
        // side effects
        var path = AppendPathElement(collectNameComponents(ctx));
        var prop = _elements[path];
        if (_parenting != null)
            throw new ParserException($"ERROR: identifier [{path.Combine()}] not allowed!", ctx);

        Real.Create(path.Last(), _rootFlow);
    }
}
