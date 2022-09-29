using System.ComponentModel;
using System.Linq;
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
        var ns = collectNameComponents(ctx);
        Assert(ns.Length.IsOneOf(1, 2));

        var path = AppendPathElement(ns);

        var existing = _modelSpits.Where(spit => spit.NameComponents.IsStringArrayEqaul(path)).ToArray();
        if (existing.Where(spit => spit.Obj is SegmentBase || spit.Obj is Child).Any())
            return;

        var pathWithoutParenting =
            _parenting == null
            ? null
            : new[] { _system.Name, _rootFlow.Name }.Concat(ns).ToArray()
            ;

        var matches =
            _modelSpits
            .Where(spitResult =>
                Enumerable.SequenceEqual(spitResult.NameComponents, path)
                || (pathWithoutParenting != null
                    && Enumerable.SequenceEqual(spitResult.NameComponents, pathWithoutParenting)))
            .Select(spitResult => spitResult.Obj)
            .ToArray()
            ;
        if (matches.OfType<Segment>().Any())
            return;

        try
        {
            var alias = matches.OfType<SpitObjAlias>().FirstOrDefault();
            if (alias != null)
            {
                if (_parenting == null)
                {
                    Assert(false);
                }
                else
                {
                    Assert(ns.Length == 1);
                    var aliasKey =
                        matches
                            .OfType<SpitObjAlias>()
                            .Where(alias => alias.Mnemonic.IsStringArrayEqaul(pathWithoutParenting))
                            .Select(alias => alias.Key)
                            .FirstOrDefault()
                            ;

                    var aliasObj =
                        _modelSpitObjects
                            .OfType<ApiItem>()
                            .Where(api => api.NameComponents.IsStringArrayEqaul(aliasKey))
                            .FirstOrDefault();
                    Assert(aliasObj != null);
                    ChildAliased.Create(ns[0], aliasObj, _parenting);
                    return;
                }
                Console.WriteLine();
            }

            var directCall =
                _modelSpitObjects
                    .OfType<ApiItem>()
                    .Where(api => api.NameComponents.IsStringArrayEqaul(ns))
                    .FirstOrDefault();

            if (directCall != null)
            {
                if (_parenting == null)
                    SegmentApiCall.Create(directCall, _rootFlow);
                else
                    ChildApiCall.Create(directCall, _parenting);
                return;
            }


            var prop = _elements[path];
            if (_parenting == null)
            {
                Assert(ns.Length == 1);
                Segment.Create(ns[0], _rootFlow);
                return;
            }
            else
            {
                Assert(false);
            }
            Console.WriteLine();
        }
        finally
        {
            UpdateModelSpits();
        }

    }

    override public void EnterIdentifier12Listing(Identifier12ListingContext ctx)
    {
        var path = AppendPathElement(collectNameComponents(ctx));
        var prop = _elements[path];
        Console.WriteLine();
    }
}
