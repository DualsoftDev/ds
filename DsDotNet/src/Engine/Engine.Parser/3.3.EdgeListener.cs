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
            return _modelSpits
                .Where(spit =>
                    spit.NameComponents.IsStringArrayEqaul(path)
                    && (spit.Obj is SegmentBase || spit.Obj is Child))
                .Select(spit => spit.Obj)
                .FirstOrDefault();
                ;
        }
        for (int i = 0; i < children.Length - 2; i+=2)
        {
            var lefts = enumerateChildren<CausalTokenContext>(children[i]);         // CausalTokensCNFContext
            var op = children[i+1] as CausalOperatorContext;
            var rights = enumerateChildren<CausalTokenContext>(children[i+2]);

            foreach (var left in lefts)
            {
                foreach(var right in rights)
                {
                    var l = findToken(left);
                    var r = findToken(right);
                    Console.WriteLine();
                }
            }
            Console.WriteLine();
        }
    }

}
