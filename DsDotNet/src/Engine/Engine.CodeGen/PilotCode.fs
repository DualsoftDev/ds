namespace Engine.CodeGen

open Engine.Core
open Newtonsoft.Json

[<AutoOpen>]
module PilotGenModule =
    let GenPilotCode(model:Model) = 
        let pilotCode = 
            """
            {
				"mode": "initialize",
				"from": "info_server",
				"reals": [
					{
						"name": "ctrl_sys.main_flow.R1",
						"type": "real",
						"parent": "ctrl_sys.main_flow",
						"indices": {
							"ctrl_sys.main_flow.R1.tsk.P1": 1,
							"ctrl_sys.main_flow.R1.tsk.M1": 2,
							"ctrl_sys.main_flow.R1.tsk.P2": 3,
							"ctrl_sys.main_flow.R1.tsk.M2": 4
						},
						"targets": {
							"ctrl_sys.main_flow.R1.tsk.M1": {
								"ctrl_sys.main_flow.R1.tsk.P1": false,
								"ctrl_sys.main_flow.R1.tsk.M1": true,
								"ctrl_sys.main_flow.R1.tsk.P2": false,
								"ctrl_sys.main_flow.R1.tsk.M2": true
							},
							"ctrl_sys.main_flow.R1.tsk.M2": {
								"ctrl_sys.main_flow.R1.tsk.P1": true,
								"ctrl_sys.main_flow.R1.tsk.M1": false,
								"ctrl_sys.main_flow.R1.tsk.P2": false,
								"ctrl_sys.main_flow.R1.tsk.M2": true
							},
							"ctrl_sys.main_flow.R1.tsk.P1": {
								"ctrl_sys.main_flow.R1.tsk.P1": true,
								"ctrl_sys.main_flow.R1.tsk.M1": false,
								"ctrl_sys.main_flow.R1.tsk.P2": false,
								"ctrl_sys.main_flow.R1.tsk.M2": true
							},
							"ctrl_sys.main_flow.R1.tsk.P2": {
								"ctrl_sys.main_flow.R1.tsk.P1": true,
								"ctrl_sys.main_flow.R1.tsk.M1": false,
								"ctrl_sys.main_flow.R1.tsk.P2": true,
								"ctrl_sys.main_flow.R1.tsk.M2": false
							}
						}
					},
					{
						"name": "ctrl_sys.main_flow.R2",
						"type": "real",
						"parent": "ctrl_sys.main_flow",
						"indices": {
							"ctrl_sys.main_flow.R2.tsk.AP": 1,
							"ctrl_sys.main_flow.R2.tsk.AM": 2,
							"ctrl_sys.main_flow.R2.tsk.BP": 3,
							"ctrl_sys.main_flow.R2.tsk.BM": 4
						},
						"targets": {
							"ctrl_sys.main_flow.R2.tsk.AM": {
								"ctrl_sys.main_flow.R2.tsk.AM": true,
								"ctrl_sys.main_flow.R2.tsk.AP": false
							},
							"ctrl_sys.main_flow.R2.tsk.AP": {
								"ctrl_sys.main_flow.R2.tsk.AM": false,
								"ctrl_sys.main_flow.R2.tsk.AP": true,
								"ctrl_sys.main_flow.R2.tsk.BM": true,
								"ctrl_sys.main_flow.R2.tsk.BP": false
							},
							"ctrl_sys.main_flow.R2.tsk.BM": {
								"ctrl_sys.main_flow.R2.tsk.AM": true,
								"ctrl_sys.main_flow.R2.tsk.AP": false,
								"ctrl_sys.main_flow.R2.tsk.BM": true,
								"ctrl_sys.main_flow.R2.tsk.BP": false
							},
							"ctrl_sys.main_flow.R2.tsk.BP": {
								"ctrl_sys.main_flow.R2.tsk.BM": false,
								"ctrl_sys.main_flow.R2.tsk.BP": true
							}
						}
					},
					{
						"name": "ctrl_sys.main_flow.R3",
						"type": "real",
						"parent": "ctrl_sys.main_flow",
						"indices": {
							"ctrl_sys.main_flow.R3.tsk.CP": 1,
							"ctrl_sys.main_flow.R3.tsk.CM": 2,
							"ctrl_sys.main_flow.R3.tsk.DP": 3,
							"ctrl_sys.main_flow.R3.tsk.DM": 4
						},
						"targets": {
							"ctrl_sys.main_flow.R3.tsk.CM": {
								"ctrl_sys.main_flow.R3.tsk.CP": false,
								"ctrl_sys.main_flow.R3.tsk.CM": true
							},
							"ctrl_sys.main_flow.R3.tsk.CP": {
								"ctrl_sys.main_flow.R3.tsk.CP": true,
								"ctrl_sys.main_flow.R3.tsk.CM": false
							},
							"ctrl_sys.main_flow.R3.tsk.DM": {
								"ctrl_sys.main_flow.R3.tsk.CP": false,
								"ctrl_sys.main_flow.R3.tsk.CM": true,
								"ctrl_sys.main_flow.R3.tsk.DM": true
							},
							"ctrl_sys.main_flow.R3.tsk.DP": {
								"ctrl_sys.main_flow.R3.tsk.DP": true,
								"ctrl_sys.main_flow.R3.tsk.DM": false
							}
						}
					}
				],
				"calls": [
					{
						"name": "ctrl_sys.main_flow.R1.tsk.P1",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R1",
						"position": {
							"x": 0.7744791666666667,
							"y": 0.6185185185185185
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R1.tsk.M1",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R1",
						"position": {
							"x": 0.7755208333333333,
							"y": 0.7314814814814815
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R1.tsk.P2",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R1",
						"position": {
							"x": 0.7651041666666667,
							"y": 0.4212962962962963
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R1.tsk.M2",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R1",
						"position": {
							"x": 0.7651041666666667,
							"y": 0.31296296296296297
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R2.tsk.AP",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R2",
						"position": {
							"x": 0.6229166666666667,
							"y": 0.6287037037037037
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R2.tsk.AM",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R2",
						"position": {
							"x": 0.6229166666666667,
							"y": 0.737037037037037
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R2.tsk.BP",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R2",
						"position": {
							"x": 0.6151041666666667,
							"y": 0.42592592592592595
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R2.tsk.BM",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R2",
						"position": {
							"x": 0.6151041666666667,
							"y": 0.31203703703703708
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R3.tsk.CP",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R3",
						"position": {
							"x": 0.46770833333333336,
							"y": 0.6435185185185185
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R3.tsk.CM",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R3",
						"position": {
							"x": 0.4708333333333333,
							"y": 0.7537037037037037
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R3.tsk.DP",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R3",
						"position": {
							"x": 0.453125,
							"y": 0.4527777777777778
						},
						"size": {
							"w": 105,
							"h": 75
						}
					},
					{
						"name": "ctrl_sys.main_flow.R3.tsk.DM",
						"type": "call",
						"parent": "ctrl_sys.main_flow.R3",
						"position": {
							"x": 0.453125,
							"y": 0.3287037037037037
						},
						"size": {
							"w": 105,
							"h": 75
						}
					}
				],
				"timestamp": "1667183446.5441985"
			}
            """

        { from = "pilot"; succeed = true; body = pilotCode; error = ""; }