using System.Runtime.InteropServices;

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

    override public void EnterCausalToken(CausalTokenContext ctx)
    {
        var path = AppendPathElement(collectNameComponents(ctx));
        var matches =
            _system.Spit()
            .Where(spitResult => Enumerable.SequenceEqual(spitResult.NameComponents, path))
            .ToArray()
            ;
        var prop = _elements[path];
        if (_parenting == null)
        {

        }
        else
        {

        }
        Console.WriteLine();
    }

    override public void EnterIdentifier12Listing(Identifier12ListingContext ctx)
    {
        var path = AppendPathElement(collectNameComponents(ctx));
        var prop = _elements[path];
        Console.WriteLine();
    }
}
