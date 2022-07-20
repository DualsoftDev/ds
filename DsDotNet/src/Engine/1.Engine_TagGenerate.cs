using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Engine.Common;
using Engine.Core;
using Engine.Graph;


namespace Engine
{
    partial class Engine
    {
        class TagGenInfo
        {
            public TagType Type;
            public Segment Segment;
            // Generated tag using this. Will be filled in later.   추후, edge 의 dependency 판정 등에 사용 됨.
            public Tag GeneratedTag { get; set; }

            //public Child Child;
            //public RootCall RootCall;
            public ICoin Child;     // Child or RootCall

            public Edge Edge;       // 사용된 edge
            public bool IsSource;   // edge 의 source 쪽인지 여부

            public string Context;
            // Tag 가 null 인 상태에서도 tag name 을 가져 올 수 있어야 함.  Tag 가 non null 이면 Tag.Name 과 동일
            public string TagName
            {
                get
                {
                    if (Child == Segment)
                        return Segment.QualifiedName;

                    return $"{Child.GetQualifiedName()}_{Segment.QualifiedName}_{Type}";
                }
            }


            /// <summary>
            /// 
            /// </summary>
            /// <param name="type">생성할 tag type</param>
            /// <param name="segment">tag 가 직접 제어할 segment</param>
            /// <param name="child">segment 를 포함하는 Child.  (Child or RootCall) </param>
            /// <param name="context">생성할 tag 가 사용되는 context</param>
            public TagGenInfo(TagType type, Segment segment, ICoin child, string context, Edge edge, bool isSource)
            {
                Debug.Assert(child is Segment || child is Child || child is RootCall);
                Type = type;
                Segment = segment;
                Context = context;
                Child = child;
                Edge = edge;
                IsSource = isSource;
                GeneratedTag = null;
            }
        }

        TagGenInfo[] CreateTags4Child(CpuBase cpu, RootFlow[] activeFlows)
        {
            Tag createTag(TagGenInfo tgi, ICoin owner, TagType tagType, CpuBase ownerCpu)
            {
                var tag = new Tag(owner, tgi.TagName, tagType, ownerCpu);
                tgi.GeneratedTag = tag;
                return tag;
            }

            var tagGenInfos =
                collectTagGenInfo()
                .GroupBy(tgi => tgi.TagName).Select(g => g.First())      // DistinctBy : https://stackoverflow.com/questions/2537823/distinct-by-property-of-class-with-linq
                //.Select(gi => gi.TagName)
                .ToArray();

            // tag 가 사용된 위치(child) 및 tag type 으로 grouping
            var tgiGroups = tagGenInfos.GroupByToDictionary(tgi => (tgi.Child, tgi.Type));
            foreach (var kv in tgiGroups)
            {
                (var location, var type) = kv.Key;
                var tgis = kv.Value;
                var tags = tgis.Select(tgi => createTag(tgi, location, type, cpu)).ToArray();

                List<Tag> storage = null;
                switch (location)
                {
                    case Child child:
                        storage = type switch
                        {
                            TagType.Start => child.TagsStart,
                            TagType.Reset => child.TagsReset,
                            TagType.End => child.TagsEnd,
                            _ => throw new Exception("ERROR")
                        };
                        break;
                    case RootCall rootCall:
                        storage = type switch
                        {
                            TagType.Start => rootCall.TxTags,
                            TagType.End => rootCall.RxTags,
                            _ => throw new Exception("ERROR")
                        };
                        break;
                    case Segment child:
                        storage = type switch
                        {
                            TagType.Start => child.TagsStart,
                            TagType.Reset => child.TagsReset,
                            TagType.End => child.TagsEnd,
                            _ => throw new Exception("ERROR")
                        };
                        break;
                    default:
                        throw new Exception("ERROR");
                }

                //Debug.Assert(storage.IsNullOrEmpty());
                storage.AddRange(tags);
                var tagNames = String.Join(", ", tgis.Select(tgi => tgi.TagName));
                Global.Logger.Debug($"Adding Child Tags {tagNames} to child [{location.GetQualifiedName()}]");

                // apply to segment
                foreach (var tgi in tgis)
                {
                    var seg = tgi.Segment;
                    var segStorage = tgi.Type switch
                    {
                        TagType.Start => seg.TagsStart,
                        TagType.Reset => seg.TagsReset,
                        TagType.End => seg.TagsEnd,
                        _ => throw new Exception("ERROR")
                    };

                    Global.Logger.Debug($"Adding Export {tgi.Type} Tag [{tgi.TagName}] to segment [{seg.QualifiedName}]");
                    segStorage.Add(createTag(tgi, seg, type, seg.OwnerCpu));
                }
            }

            return tagGenInfos.ToArray();

            /// Root flow 에 존재하는 
            /// - root call 
            /// - root segment 의 하부 call 및 external segment call 
            /// 의 호출을 위한 start/reset/end tag 를 생성하기 위한 정보를 생성
            IEnumerable<TagGenInfo> collectTagGenInfo()
            {
                /// 하나의 [external segment call 을 위한 Child] 에 대해서 Set 및 Reset 명령이 동시에 들어올 수 없음을 check
                void verifyExternalSegmentCallChild_SingleCommandType(Child child)
                {
                    if (child.IsCall)
                        return;

                    var ies =
                        GraphUtil.getIncomingEdges(child.Parent.GraphInfo.Graph, child)
                            .Select(qge => qge.OriginalEdge)
                            .Distinct()
                            ;
                    if (ies.Any())
                    {
                        // incoming edge 를 reset edge 여부로 grouping 한 것.
                        var iesGroups = ies.GroupByToDictionary(e => e is IResetEdge);
                        if (iesGroups.Count() > 1)
                            throw new Exception("ERROR: Both reset and start edges exists for external segment child call.");
                    }
                }

                /// Child 에 대한 End tag 생성 정보를 반환
                IEnumerable<TagGenInfo> createEndTagGenInfos(Child child, string context, Edge edge, bool isSource)
                {
                    switch (child.Coin)
                    {
                        case SubCall call:
                            var segEnd = call.Prototype.RXs.OfType<Segment>();
                            foreach (var e in segEnd)
                                yield return new TagGenInfo(TagType.End, e, child, context, edge, isSource);
                            break;

                        case ExSegmentCall exSeg:
                            var seg = exSeg.ExternalSegment;
                            yield return new TagGenInfo(TagType.End, seg, child, context, edge, isSource);
                            break;
                        default:
                            throw new Exception("ERROR");
                    }

                }


                IEnumerable<TagGenInfo> createTagGenInfos4Edge(Edge edge)
                {
                    var sourceTgis = edge.Sources.SelectMany(s => createTagGenInfos4RootVertex(s, edge, true));
                    foreach (var tgi in sourceTgis)
                        yield return tgi;

                    var tagetTgis = createTagGenInfos4RootVertex(edge.Target, edge, false);
                    foreach (var tgi in tagetTgis)
                        yield return tgi;

                }



                IEnumerable<TagGenInfo> createTagGenInfos4RootVertex(IVertex root, Edge edge, bool isSource)
                {
                    switch (root)
                    {
                        case RootCall rootCall:
                            foreach (var txSeg in rootCall.Prototype.TXs.OfType<Segment>())
                                yield return new TagGenInfo(TagType.Start, txSeg, rootCall, rootCall.QualifiedName, edge, isSource);
                            foreach (var rxSeg in rootCall.Prototype.RXs.OfType<Segment>())
                                yield return new TagGenInfo(TagType.End, rxSeg, rootCall, rootCall.QualifiedName, edge, isSource);
                            break;

                        case Segment rootSeg:
                            var fqdn = rootSeg.QualifiedName;
                            if (edge != null)
                            {
                                var type = isSource ? TagType.End : TagType.Start;
                                yield return new TagGenInfo(type, rootSeg, rootSeg, fqdn, edge, isSource);

                            }

                            var children = rootSeg.ChildVertices.OfType<Child>();
                            foreach (var child in children)
                            {
                                verifyExternalSegmentCallChild_SingleCommandType(child);

                                // - 모든 경우(SubCall or ExSegmentCall )에 End Tag 는 무조건 생성
                                // - SubCall 인 경우, reset edge 무시
                                // - incoming edge 가 reset 인 경우 : Reset Tag 생성
                                // - incoming edge 가 reset 이 아닌 경우 (없는 경우 포함) : Start Tag 생성

                                foreach (var tgi in createEndTagGenInfos(child, fqdn, edge, isSource))
                                    yield return tgi;

                                var hasReset =
                                    GraphUtil.getIncomingEdges(rootSeg.GraphInfo.Graph, child)
                                    .Select(qge => qge.OriginalEdge)
                                    .Any(e => e is IResetEdge)
                                    ;


                                switch (child.Coin)
                                {
                                    case SubCall call:
                                        if (!hasReset) // reset 이 없으면...  (set edge 가 있거나, 없거나..)
                                        {
                                            var segStart = call.Prototype.TXs.OfType<Segment>();
                                            foreach (var s in segStart)
                                                yield return new TagGenInfo(TagType.Start, s, child, fqdn, edge, isSource);
                                        }
                                        break;

                                    case ExSegmentCall exSeg:
                                        var seg = exSeg.ExternalSegment;
                                        var type = hasReset ? TagType.Reset : TagType.Start;
                                        yield return new TagGenInfo(type, seg, child, fqdn, edge, isSource);
                                        break;

                                    default:
                                        throw new Exception("ERROR");
                                }
                            }
                            break;

                        default:
                            throw new Exception("ERROR");
                    }
                }



                var edges = activeFlows.SelectMany(f => f.Edges);
                foreach ( var tgi in edges.SelectMany(createTagGenInfos4Edge))
                    yield return tgi;


                // edge 연결 없이 root 상에 존재하는 vertices 에 대한 tag 생성
                var roots = activeFlows.SelectMany(f => f.ChildVertices).Distinct();
                var vertices = edges.SelectMany(e => e.Vertices);
                var isolatedVertices = roots.Except(vertices);
                foreach (var tgi in isolatedVertices.SelectMany(v => createTagGenInfos4RootVertex(v, null, false)))
                    yield return tgi;
            }
        }
    }
}
