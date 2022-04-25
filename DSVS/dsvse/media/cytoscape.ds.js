// This script will be run within the webview itself
// It cannot access the main VS Code APIs directly.

(function () {
	// In your webview script
	const vscode = acquireVsCodeApi();
	console.log("vscode=", vscode);

	const strElements = document.title;
	const eles = JSON.parse(strElements);
	console.log("type of elements=", typeof eles, eles);

	document.addEventListener("DOMContentLoaded", function () {
		console.log("Document loaded.  elements=", eles);
		console.log("cy div=", document.getElementById("cy"));
		// 강제로 focus 이동
		window.focus();

		let cy = cytoscape({
			container: document.getElementById("cy"),
			wheelSensitivity: 0.1,
			layout: {
				name: "cose", //circle, cose, grid
			},
			elements: eles,
			style: [
				{
					selector: "node",
					style: {
						shape: "round-rectangle",
						// 'width': 'data(width)',
						// 'height': 'data(height)',
						color: "white", // text color

						"border-width": 2,
						"border-color": "white",
						"border-style": "solid", //"dotted",

						// 'background-color': 'data(background_color)',
						//'text-outline-color': 'data(background_color)',

						// 'text-outline-color': 'orange'

						// 'text-outline-width': 2,
						"text-opacity": 0.5,
						label: "data(label)",
						//'font-size' : '25px',

						// todo : 한번 color 정하면, selection color 변경할 수 있는 방법을 찾아야 함.
						"background-color": "green",
					},
				},
				{
					selector: "edge",
					style: {
						"curve-style": "bezier",
						"line-color": "cyan",
						// 'width': 3,
						"target-arrow-shape": "triangle",
						// 'source-arrow-shape': 'circle',
					},
				},
				{
					selector: ":selected",
					style: {
						"border-color": "red",
						// "border-width": 4,
					},
				},
			],
		});

		cy.edges()
			.filter((e, i) => e.isEdge() && e.data("line-style") == "dashed")
			.style("line-style", "dashed");
		//.update()

		var ur = cy.undoRedo({
			isDebug: true,
		});

		cy.on("afterUndo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkred; font-weight: bold'>Undo: </span> " +
				name +
				"</br>";
		});

		cy.on("afterRedo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkblue; font-weight: bold'>Redo: </span>" +
				name +
				"</br>";
		});

		cy.on("afterDo", function (e, name) {
			document.getElementById("undos").innerHTML +=
				"<span style='color: darkmagenta; font-weight: bold'>Do: </span>" +
				name +
				"</br>";
		});

		ur.do("add", {
			group: "nodes",
			data: { weight: 75, name: "New Node", id: "New Node", label: "New Node" },
			position: { x: 50, y: 50 },
			style: {
				"background-color": "darkred",
			},
		});

		cy.on("select", "node", function (e) {
			console.log("node selected");
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
				if (selecteds.length > 0) {
					ur.do("remove", selecteds);
					vscode.postMessage({
						type: "removed",
						args: "Something", //JSON.stringify(selecteds)
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
})();

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
