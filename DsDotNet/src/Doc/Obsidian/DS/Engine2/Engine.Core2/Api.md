
```cs
DsSystem sys = DsSystem.Create("mysys");
DsFlow flow1 = sys.CreateFlow("myflow");
DsWork work1 = flow1.CreateWork("w1");
DsAction action1 = work1.CreateAction("dev1.ADV", "DSLibrary/DoubleCylinder.ds");
DsAction action2 = work1.CreateAction("dev1.RET", "DSLibrary/DoubleCylinder.ds");
DsAutoPre ap1 = work1.CreateAutoPre("otherflow.dev.api", input:false)
DsAlias alias1 = work1.CreateAlias("dev1.ADV");
// e.g: alias1.Name === "dev1_ADV_1"
DsEdge edge1 = work1.CreateEdge(action1, action2, START);


DsVertex
	DsCircle
		DsDisabled
		DsCoinR
			AutoPre
			Safety
		DsCoin
DsEdge

Ds

```


