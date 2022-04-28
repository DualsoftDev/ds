// This script will be run within the webview itself
// It cannot access the main VS Code APIs directly.

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
		cy.bind('click', 'node', function(evt) {
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

function layout(name) {
	console.log(name);
	cy.layout({
	  name
	}).run();
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
