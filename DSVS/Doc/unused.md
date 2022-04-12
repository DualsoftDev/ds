				{
					"name": "constant.language.unit.ds",
					"match": "\\(\\)"
				},
				{
					"match": "\\b([[:alpha:]_]\\w*)\\s*(<|>|~)([[:alpha:]_]\\w*))",
					"captures": {
						"1": {
							"name": "variable.parameter.ds"
						},
						"2": {
							"name": "keyword.operator.ds"
						},
						"3": {
							"name": "variable.parameter.ds"
						}
					}
				},




		"segmentName":{
			"patterns": [
				{
					"name": "constant.character.escape.ds",
					"match": "[:alnum]*"
				}
			]
		},
