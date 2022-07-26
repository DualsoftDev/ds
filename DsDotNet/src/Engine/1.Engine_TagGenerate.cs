using Engine.Graph;


namespace Engine;

partial class EngineBuilder
{
    [DebuggerDisplay("{ToText()}")]
    class TagGenInfo
    {
        public TagType Type;
        public Segment TagContainerSegment;
        // Generated tag using this. Will be filled in later.   추후, edge 의 dependency 판정 등에 사용 됨.
        public Tag GeneratedTag { get; set; }

        public ICoin Child;     // Child or RootCall or Segment

        public Edge Edge;       // 사용된 edge
        public bool IsSource;   // edge 의 source 쪽인지 여부
        public bool IsTarget => !IsSource;   // edge 의 target 쪽인지 여부

        public string Context;

        public Cpu OwnerCpu;    // 생성된 tag 의 소유주 CPU
        // Tag 가 null 인 상태에서도 tag name 을 가져 올 수 있어야 함.  Tag 가 non null 이면 Tag.Name 과 동일
        public string TagName
        {
            get
            {
                if (Child == TagContainerSegment)
                    return $"{TagContainerSegment.QualifiedName}_{Type}";

                return $"{Child.GetQualifiedName()}_{TagContainerSegment.QualifiedName}_{Type}";
            }
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="type">생성할 tag type</param>
        /// <param name="tagContainerSegment">tag 가 직접 제어할 segment</param>
        /// <param name="child">segment 를 포함하는 Child.  (Child or RootCall) </param>
        /// <param name="context">child 가 사용된 context</param>
        /// <param name="edge">생성 정보 기준 edge</param>
        /// <param name="isSource">child 가 edge 의 source 쪽인지의 여부.  false 이면 target 쪽</param>
        /// <param name="ownerCpu">생성된 Tag 가 속할 CPU</param>
        public TagGenInfo(TagType type, Segment tagContainerSegment, ICoin child, string context, Edge edge, bool isSource, Cpu ownerCpu)
        {
            Debug.Assert(child is Segment || child is Child || child is RootCall);
            Type = type;
            TagContainerSegment = tagContainerSegment;
            Context = context;
            Child = child;
            Edge = edge;
            IsSource = isSource;
            OwnerCpu = ownerCpu;
            GeneratedTag = null;
        }

        public string ToText() => $"{TagName} : Child={Child.ToString()}";

    }


    /// <summary>
    /// Root flow 에 존재하는
    /// <para/> - root call 및
    /// <para/> - root segment 의 하부 call 및 external segment call 의
    /// <para/>   호출을 위한 start/reset/end tag 를 생성하기 위한 정보를 생성
    /// <para/> *** 호출 site 와 피호출 site 모두 동일 이름의 tag 를 생성할 수 있도록 CPU 별 pair 로 중복 정보를 생성한다.
    /// </summary>
    TagGenInfo[] CreateTags4Child(RootFlow[] rootFlows)
    {
        Debug.Assert(rootFlows.Select(f => f.Cpu).Distinct().Count() == 1);
        Cpu cpu = rootFlows[0].Cpu;

        var tagGenInfos = collectTagGenInfo(rootFlows).ToArray();

        // tag 가 사용된 위치(child) 및 tag type 으로 grouping
        var tgiGroups = tagGenInfos.GroupByToDictionary(tgi => (tgi.Child, tgi.Type));
        foreach (var kv in tgiGroups)
        {
            (var location, var type) = kv.Key;
            var tgis = kv.Value;
            var tags =
                tgis
                .Select(tgi => createTag(tgi, location, type))
                .Where(tgi => tgi.OwnerCpu == cpu)
                .ToArray();

            var addTagsFunc = location switch
            {
                ITagSREContainer sreContainer => sreContainer.AddTagsFunc,  // sreContainer = {Child or Segment}
                RootCall rootCall => type switch
                {
                    TagType.Start => new Action<IEnumerable<Tag>>(tags => rootCall.AddTxTags(tags)),
                    TagType.End   => new Action<IEnumerable<Tag>>(tags => rootCall.AddRxTags(tags)),
                    _             => throw new Exception("ERROR")
                },
                _ => throw new Exception("ERROR"),
            };

            addTagsFunc(tags);


            var tagNames = String.Join(", ", tgis.Select(tgi => tgi.TagName));
            Global.Logger.Debug($"Adding Child Tags {tagNames} to child [{location.GetQualifiedName()}]");

            // apply to segment
            // 생성 정보 중에 적용할 segment 의 CPU 에 해당하는 것들에 대해서만...
            foreach (var tgi in tgis.Where(tgi => tgi.OwnerCpu == tgi.TagContainerSegment.OwnerCpu))
            {
                var seg = tgi.TagContainerSegment;
                Global.Logger.Debug($"Adding Export {tgi.Type} Tag [{tgi.TagName}] to segment [{seg.QualifiedName}]");
                Debug.Assert(seg.OwnerCpu == tgi.OwnerCpu);
                var tag = createTag(tgi, seg, type);
                seg.AddTagsFunc(new[] { tag });
            }
        }

        return tagGenInfos.ToArray();


        Tag createTag(TagGenInfo tgi, ICoin owner, TagType tagType)
        {
            if (tgi.GeneratedTag != null)
            {
                Debug.Assert(tgi.GeneratedTag.OwnerCpu == owner.OwnerCpu);
                Debug.Assert(tgi.GeneratedTag.Type == tagType);
                return tgi.GeneratedTag;
            }
            if (tgi.OwnerCpu == owner.OwnerCpu && tgi.OwnerCpu.TagsMap.ContainsKey(tgi.TagName))
            {
                Global.Logger.Warn($"Tag [{tgi.TagName} already created.  using it instead creating new one.");
                var existing = tgi.OwnerCpu.TagsMap[tgi.TagName];
                tgi.GeneratedTag = existing;
                return existing;
            }

            var tag = new Tag(owner, tgi.TagName, tagType, tgi.OwnerCpu);
            tgi.GeneratedTag = tag;
            return tag;
        }



        /// Root flow 에 존재하는
        /// - root call
        /// - root segment 의 하부 call 및 external segment call
        /// 의 호출을 위한 start/reset/end tag 를 생성하기 위한 정보를 생성
        IEnumerable<TagGenInfo> collectTagGenInfo(RootFlow[] rootFlows)
        {
            var edges = rootFlows.SelectMany(f => f.Edges);
            foreach (var tgi in edges.SelectMany(edge => createTagGenInfos4Edge(edge, null)))
                yield return tgi;


            // edge 연결 없이 root 상에 존재하는 vertices 에 대한 tag 생성
            var roots = rootFlows.SelectMany(f => f.ChildVertices).Distinct();
            var vertices = edges.SelectMany(e => e.Vertices);
            var isolatedVertices = roots.Except(vertices).ToArray();
            var isolatedVertiecsTags =
                isolatedVertices
                .SelectMany(v => createTagGenInfos4RootVertex(v, null, false))
                .ToArray()
                ;
            foreach (var tgi in isolatedVertiecsTags)
                yield return tgi;


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
                        var segsEnd = call.Prototype.RXs.OfType<Segment>();
                        foreach (var segE in segsEnd)
                        {
                            // 호출측 CPU: edge.OwnerCpu
                            // 피호출측 CPU: segE.OwnerCpu
                            yield return new TagGenInfo(TagType.End, segE, child, context, edge, isSource, segE.OwnerCpu);
                            yield return new TagGenInfo(TagType.End, segE, child, context, edge, isSource, edge.OwnerCpu);
                        }
                        break;

                    case ExSegmentCall exSeg:
                        var seg = exSeg.ExternalSegment;
                        yield return new TagGenInfo(TagType.End, seg, child, context, edge, isSource, seg.OwnerCpu);
                        yield return new TagGenInfo(TagType.End, seg, child, context, edge, isSource, edge.OwnerCpu);
                        break;
                    default:
                        throw new Exception("ERROR");
                }

            }


            IEnumerable<TagGenInfo> createTagGenInfos4Edge(Edge edge, string fqdn)
            {
                var sourceTgis =
                    edge.Sources
                    .SelectMany(s => s switch {
                        Child child => createTagGenInfos4Child(child, edge, true, fqdn),
                        Segment segment => createTagGenInfos4RootVertex(s, edge, true),
                        RootCall rootCall => createTagGenInfos4RootVertex(s, edge, true),
                        _ => throw new Exception("ERROR"),
                    })
                    .ToArray()
                    ;

                foreach (var tgi in sourceTgis)
                    yield return tgi;

                var tagetTgis = createTagGenInfos4RootVertex(edge.Target, edge, false).ToArray();
                foreach (var tgi in tagetTgis)
                    yield return tgi;

            }


            IEnumerable<TagGenInfo> createTagGenInfos4Child(Child child, Edge edge, bool isSource, string fqdn)
            {
                verifyExternalSegmentCallChild_SingleCommandType(child);
                var rootSeg = child.Parent;

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
                            foreach (var segS in segStart)
                            {
                                yield return new TagGenInfo(TagType.Start, segS, child, fqdn, edge, isSource, segS.OwnerCpu);
                                yield return new TagGenInfo(TagType.Start, segS, child, fqdn, edge, isSource, edge.OwnerCpu);
                            }
                        }
                        break;

                    case ExSegmentCall exSeg:
                        var seg = exSeg.ExternalSegment;
                        var type = hasReset ? TagType.Reset : TagType.Start;
                        yield return new TagGenInfo(type, seg, child, fqdn, edge, isSource, seg.OwnerCpu);
                        yield return new TagGenInfo(type, seg, child, fqdn, edge, isSource, edge.OwnerCpu);
                        break;

                    default:
                        throw new Exception("ERROR");
                }
            }


            IEnumerable<TagGenInfo> createTagGenInfos4RootVertex(IVertex root, Edge edge, bool isSource)
            {
                switch (root)
                {
                    case RootCall rootCall:
                        foreach (var txSeg in rootCall.Prototype.TXs.OfType<Segment>())
                        {
                            yield return new TagGenInfo(TagType.Start, txSeg, rootCall, rootCall.QualifiedName, edge, isSource, txSeg.OwnerCpu);
                            yield return new TagGenInfo(TagType.Start, txSeg, rootCall, rootCall.QualifiedName, edge, isSource, edge.OwnerCpu);
                        }
                        foreach (var rxSeg in rootCall.Prototype.RXs.OfType<Segment>())
                        {
                            yield return new TagGenInfo(TagType.End, rxSeg, rootCall, rootCall.QualifiedName, edge, isSource, rxSeg.OwnerCpu);
                            yield return new TagGenInfo(TagType.End, rxSeg, rootCall, rootCall.QualifiedName, edge, isSource, edge.OwnerCpu);
                        }
                        break;

                    case Segment rootSeg:
                        var fqdn = rootSeg.QualifiedName;
                        if (edge != null)
                        {
                            var type = isSource ? TagType.End : (edge is IResetEdge ? TagType.Reset : TagType.Start);
                            yield return new TagGenInfo(type, rootSeg, rootSeg, fqdn, edge, isSource, root.OwnerCpu);
                        }

                        var subEdges = rootSeg.Edges.ToArray();
                        foreach (var subEdge in subEdges)
                        {
                            var tgisSource =
                                subEdge.Sources
                                    .OfType<Child>()
                                    .SelectMany(ch => createTagGenInfos4Child(ch, subEdge, true, fqdn))
                                    .ToArray()
                                    ;
                            foreach (var tgi in tgisSource)
                                yield return tgi;

                            var target = subEdge.Target as Child;
                            if (target != null)
                            {
                                var targetTags = createTagGenInfos4Child(target, subEdge, false, fqdn).ToArray();
                                foreach (var tgi in targetTags)
                                    yield return tgi;
                            }
                        }

                        // Todo : isolated vertex 처리

                        break;

                    default:
                        throw new Exception("ERROR");
                }
            }

        }
    }
}
