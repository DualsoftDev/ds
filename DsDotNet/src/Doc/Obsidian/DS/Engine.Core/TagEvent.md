- Event{System, Flow, Vertex, ApiItem, TaskDev, Variable, HwSys} of IStorage * Target * TagKind
	- e.g
		- Event*System* of tag:IStorage * target:Ds*System* * tagKind:*System*Tag
		- Event*Flow* of tag:IStorage * target:*Flow* * tagKind:*Flow*Tag

- SystemTag
	- on, off
	- button, lamp, monitor of PREDOMICATH (I, O (Idle, Origin)는 monitor 에만 포함됨)
- FlowTag
	- button, lamp, state of PREDOMICATH (I, O (Idle, Origin) 제외)
	- mode (auto, manual, idle)
- VertexTag
	- RGHF, (force) {start, reset, end}
	- call or real 의 성격
		- Real
			- Token, data,
			- {Script, Motion} {Start, End} ..
		- Call
			- callCommandPulse, dummyCoinSTs, ...
- ApiItemTag
- TaskDevTag
	- Plan {Start, End, Output}, 
	- Action{In, Out, Memory}

 
- TagEventSubject : `Subject<TagEvent>`
	- ScanOnce -> changedTags -> notifyPreExecute -> TagEventSubject.OnNext(...)