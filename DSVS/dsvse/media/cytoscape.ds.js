!(function () {
	// In your webview script
	const vscode = acquireVsCodeApi();
	console.log("vscode=", vscode);

	document.addEventListener("DOMContentLoaded", function () {
		console.log("cy div=", document.getElementById("cy"));
		// // 강제로 focus 이동
		// window.focus();


		cy.edges()
			.filter((e, i) => e.isEdge() && e.data("line-style") == "dashed")
			.style("line-style", "dashed");
		//.update()

		//cy.layout().run();

		var ur = cy.undoRedo({
			isDebug: true,
		});

		cy.on("afterUndo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkred; font-weight: bold'>Undo: </span> " +
				name +
				"</br>";
			vscode.postMessage({
				type: "undo",
				args: name, //JSON.stringify(selecteds)
			});
		});

		cy.on("afterRedo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkblue; font-weight: bold'>Redo: </span>" +
				name +
				"</br>";
			vscode.postMessage({
				type: "redo",
				args: name, //JSON.stringify(selecteds)
			});
		});

		cy.on("afterDo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkmagenta; font-weight: bold'>Do: </span>" +
				name +
				"</br>";
		});

		/*
		 * 임의 노드 추가
		 */
		// ur.do("add", {
		// 	group: "nodes",
		// 	data: { weight: 75, name: "New Node", id: "New Node", label: "New Node" },
		// 	position: { x: 50, y: 50 },
		// 	style: {
		// 		"background-color": "darkred",
		// 	},
		// });

		cy.on("select", "node", function (e) {
			console.log("node selected:", e.target.id());
		});
		cy.bind('click', 'node', function (evt) {
			console.log('node clicked:(by bind) ', evt.target.id());
		});

		document.addEventListener("click", function (e) {
			console.log("clicked");

			// 강제로 focus 이동
			window.focus();
		});

		document.addEventListener("keydown", function (e) {
			console.log(
				"keydown event detected.",
				e.which,
				"targetnode=",
				e.target.nodeName
			);
			if (e.which === 46) {
				// DEL key
				var selecteds = cy.$(":selected");
				const delNodeName = selecteds[0]._private.data.name;
				console.log('selected:', delNodeName);

				if (selecteds.length > 0) {
					ur.do("remove", selecteds);
					vscode.postMessage({
						type: "removed",
						args: delNodeName,	//e.target.nodeName, //JSON.stringify(selecteds)
					});
				}
			} else if (e.ctrlKey && e.target.nodeName === "BODY")
				if (e.which === 90) ur.undo();
				else if (e.which === 89) ur.redo();
		});

		document
			.getElementById("batchButton")
			.addEventListener("click", function (e) {
				console.log("Button clicked...");
				actions = [];
				actions.push({
					name: "add",
					param: {
						group: "nodes",
						data: { weight: 75, name: "Bob", id: "Bob", label: "Bob" },
						position: { x: 50, y: 50 },
						style: { "background-color": "darkgreen" },
					},
				});
				actions.push({
					name: "remove",
					param: cy.$(":selected"),
				});
				ur.do("batch", actions);
			});
	});

	return true;
})();

/**
 * 동적으로 cytoscape 의 layout 수행
 * @param {*} name Layout 이름
 */
function layout(name) {
	//import ext from 'cytoscape-cise';
	//cytoscape.use( ext );
	// cytoscape.use( require('cytoscape-cise') );

	console.log('applying layout:', name);
	// if (name === "cise") {
    //     import ext as cise from 'cytoscape-cise';
    //     cytoscape.use( cise );
	// }
	cy.layout({
		name
	}).run();
}

/**
 * cytoscape 의 노드/엣지 추가
 * @param elements - cytoscape 용 elements 집합 : object
 * @returns 
 */
function fillCytoscape(elements) {
	let cy = cytoscape({
		container: document.getElementById("cy"),
		wheelSensitivity: 0.1,

		layout: {
			name: "dagre", //cose, grid, dagre, circle, random, arbor, cose-bilkent, cola, constraint
			/*
			spacingFactor: 120,		// https://stackoverflow.com/questions/54015729/cytoscape-js-spacing-between-nodes
			idealEdgeLength: 100,
			fit: false, // dangerous
			*/
			gravity: -100,
			/*
			gravityCompound: -10,
			animate: false,
			*/
		},
		elements: elements,
		style: [
			{
				selector: "node",
				style: {
					shape: "circle", //"round-rectangle",
					/*
					'width': 'data(width)',
					'height': 'data(height)',
					*/
					color: "white", /* text color */
					"text-wrap": "wrap",  /* multiline text : use '\n' */

					"border-width": 2,
					"border-color": "white",
					"border-style": "solid", //"dotted",

					'background-color': 'data(background_color)',
					/*
					'text-outline-color': 'data(background_color)',    
					'text-outline-width': 2,
					'text-outline-color': 'orange'
					*/

					"text-opacity": 0.5,
					label: "data(label)",
					/* 'font-size' : '25px', */

					/* 
					// todo : 한번 color 정하면, selection color 변경할 수 있는 방법을 찾아야 함.
					"background-color": "green",
					*/
				},
			},
			{
				selector: "edge",
				style: {
					"curve-style": "bezier",
					"line-color": "cyan",
					/* 'width': 1, */
					"target-arrow-shape": "triangle",
					'target-arrow-color': 'orange',
					/* 'source-arrow-shape': 'circle', */
				},
			},
			{
				selector: ":selected",
				style: {
					"border-color": "red",
					/* "border-width": 4,*/
				},
			},
		],

		/* initial viewport state: Not working
		zoom: 0.8,
		pan: { x: 0, y: 0 },
		*/

	});

	return cy;
}



/* vscode 에서 dropdown 은 지원안되는 듯 함.. */
function dropdown() {
	document.getElementById("myDropdown").classList.toggle("show");

	var dropdowns = document.getElementsByClassName("dropdown-content");
	console.log(dropdowns);
	var i;
	for (i = 0; i < dropdowns.length; i++) {
		var openDropdown = dropdowns[i];
		if (openDropdown.classList.contains('show'))
			openDropdown.classList.remove('show');
	}
}


// const vscode = acquireVsCodeApi();

// const oldState = /** @type {{ count: number} | undefined} */ (vscode.getState());

// const counter = /** @type {HTMLElement} */ (document.getElementById('lines-of-code-counter'));
// console.log('Initial state', oldState);

// let currentCount = (oldState && oldState.count) || 0;
// counter.textContent = `${currentCount}`;

// setInterval(() => {
//     counter.textContent = `${currentCount++} `;

//     // Update state
//     vscode.setState({ count: currentCount });

//     // Alert the extension when the cat introduces a bug
//     if (Math.random() < Math.min(0.001 * currentCount, 0.05)) {
//         // Send a message back to the extension
//         vscode.postMessage({
//             command: 'alert',
//             text: '🐛  on line ' + currentCount
//         });
//     }
// }, 100);

// // Handle messages sent from the extension to the webview
// window.addEventListener('message', event => {
//     const message = event.data; // The json data that the extension sent
//     switch (message.command) {
//         case 'refactor':
//             currentCount = Math.ceil(currentCount * 0.5);
//             counter.textContent = `${currentCount}`;
//             break;
//     }
// });
