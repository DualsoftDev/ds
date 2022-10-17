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
        }
    }
}
