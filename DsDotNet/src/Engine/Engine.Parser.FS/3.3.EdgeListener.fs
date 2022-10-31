using static Engine.Core.CoreModule

namespace Engine.Parser.FS


/// <summary>
/// 모든 vertex 가 생성 된 이후, edge 연결 작업 수행
/// </summary>
class EdgeListener : ListenerBase
{
    public EdgeListener(dsParser parser, ParserHelper helper)
        : base(parser, helper)
    {
        UpdateModelSpits()
    }

    override public void EnterModel(ModelContext ctx)
    {
        foreach (let ac in ParserHelper.AliasCreators)
        {
            let (name, parent, target) = (ac.Name, ac.Parent, ac.Target)
            let graph = parent.Graph
            let existing = graph.FindVertex(name)
            if (existing == null)
            {
                Call dummyCall = null
                switch (target)
                {
                    case AliasTargetReal real:
                        let realTarget = _modelSpits.First(spit => spit.GetCore() is Real r && r.NameComponents.IsStringArrayEqaul(real.TargetFqdn)).GetCore() as Real
                        Alias.Create(name, AliasTargetType.NewRealTarget(realTarget), parent)
                        break
                    case AliasTargetDirectCall directCall:
                        let apiTarget = _modelSpits.First(spit => spit.GetCore() is ApiItem a && a.NameComponents.IsStringArrayEqaul(directCall.TargetFqdn)).GetCore() as ApiItem
                        dummyCall = Call.CreateNowhere(apiTarget, parent)
                        Alias.Create(name, AliasTargetType.NewCallTarget(dummyCall), parent)
                        break
                    case AliasTargetApi api:
                        dummyCall = Call.CreateNowhere(api.ApiItem, parent)
                        Alias.Create(name, AliasTargetType.NewCallTarget(dummyCall), parent)
                        break
                }
            }
            Console.WriteLine()
        }

        UpdateModelSpits()
    }


    override public void EnterCausalPhrase(CausalPhraseContext ctx)
    {
        let children = ctx.children.ToArray();      // (CausalTokensDNF CausalOperator)+ CausalTokensDNF
        children.Iter((ctx, n) => Assert( n % 2 == 0 ? ctx is CausalTokensDNFContext : ctx is CausalOperatorContext))

        object findToken(CausalTokenContext ctx)
        {
            let ns = collectNameComponents(ctx)
            let path = AppendPathElement(ns)
            if (path.Length == 5)
                path = AppendPathElement(ns.Combine())
            let matches =
                _modelSpits
                .Where(spit => spit.NameComponents.IsStringArrayEqaul(path)
                                || spit.NameComponents.IsStringArrayEqaul(AppendPathElement(new[] {ns.Combine()}))
                )
                
            let token =
                matches
                .Where(spit =>spit.GetCore() is Vertex)
                .Select(spit => spit.GetCore())
                .FirstOrDefault()
                
            Assert(token != null)
            return token
        }

        /*
            children[0] > children[2] > children[4]     where (child[1] = '>', child[3] = '>')
            ===> children[0] > children[2],
                 children[2] > children[4]

            e.g "A, B > C, D > E"
            ===> children[0] = {A; B},
                 children[2] = {C; D},
                 children[4] = {E},

            todo: "A, B" 와 "A ? B" 에 대한 구분 없음.
         */
        for (int i = 0; i < children.Length - 2; i+=2)
        {
            let lefts = enumerateChildren<CausalTokenContext>(children[i]);         // CausalTokensCNFContext
            let op = children[i + 1].GetText()
            let rights = enumerateChildren<CausalTokenContext>(children[i+2])

            foreach (let left in lefts)
            {
                foreach(let right in rights)
                {
                    let l = findToken(left)
                    let r = findToken(right)
                    if (l == null)
                        throw new ParserException($"ERROR: failed to find [{left.GetText()}]", ctx)
                    if (r == null)
                        throw new ParserException($"ERROR: failed to find [{right.GetText()}]", ctx)

                    if (_parenting == null)
                        _flow.CreateEdges(l as Vertex, r as Vertex, op)
                    else
                        _parenting.CreateEdges(l as Vertex, r as Vertex, op)
                }
            }
        }
    }
}
