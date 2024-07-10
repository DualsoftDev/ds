
cy.nodes()
cy.edges()
cy.$(':selected')
cy.$(':visible')        // cy.nodes().filter(':visible')
cy.$(':hidden')
cy.$(':parent')
cy.$('node.Flow')
cy.$(':selected').position()
cy.$(':selected').position({ x: 2000, y: 10 })
cy.$('#SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1')
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1') // cy.$('#some-id') 와 동일
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1').data()
cy.$id('SIDE.S200_CARTYPE_MOVE.S204_END__SIDE.S200_CARTYPE_MOVE.S205_RBT1').classes()
cy.edges().filter(e => e.classes().includes("Reset")).length
cy.edges().filter(e => e.classes().includes("Reset"))[0].data()
cy.edges().filter(e => e.classes().includes("Reset"))[0].classes()
cy.edges().filter(e => e.classes().includes("Reset")).json()

cy.edges().filter(e => e.data().source === 'SIDE.MES.S201_RBT1').json()
cy.nodes().map(n => n.data().id)

cy.nodes().filter(n => n.data().id === "SIDE").json()
cy.$id('SIDE').json()
cy.$('#SIDE').json()

cy.$('node.Flow').filter(node => node.visible()).length;
cy.$('#SIDE.MES.MES_1').style('shape') ==> 'diamond'
cy.$('#SIDE.MES.MES_1').style('shape', 'ellipse')
cy.$('#SIDE.MES.MES_1').style('color')
cy.$('#SIDE.MES.MES_1').style('background-color', 'yellow')
cy.$('#SIDE.MES.MES_1').style('border-width', '3px')
cy.$('node.Flow').descendants().style('background-color', 'green')
cy.fit(cy.$(':visible'))

cy.add({ data: { id: 'z', parent: 'b' } })
cy.add({ data: { source: 'z', target: 'c' } })
// elements: {
//   nodes: [
//     { data: { id: 'a', parent: 'b' }, position: { x: 215, y: 85 } },
//     { data: { id: 'b' } },
//     { data: { id: 'c', parent: 'b' }, position: { x: 300, y: 85 } },
//     { data: { id: 'd' }, position: { x: 215, y: 175 } },
//     { data: { id: 'e' } },
//     { data: { id: 'f', parent: 'e' }, position: { x: 300, y: 175 } }
//   ],
//   edges: [
//     { data: { id: 'ad', source: 'a', target: 'd' } },
//     { data: { id: 'eb', source: 'e', target: 'b' } }

//   ]
// },


// elements: {
//     'nodes': [
//         { data: { id: 'a', parent: 'p' } },
//         { data: { id: 'b', parent: 'p' } },
//         { data: { id: 'c', parent: 'p' } },
//         { data: { id: 'd', parent: 'p' } },
//         { data: { id: 'e', parent: 'p' } },
//         { data: { id: 'f', parent: 'p' } },
//         { data: { id: 'g', parent: 'p' } },
//         { data: { id: 'h', parent: 'p' } },
//         { data: { id: 'p' } }
//     ],
//     'edges': [
//         { data: { id: 'e1', source: 'a', target: 'b' } },
//         { data: { id: 'e2', source: 'b', target: 'c' } },
//         { data: { id: 'e3', source: 'b', target: 'd' } },
//         { data: { id: 'e4', source: 'e', target: 'f' } },
//         { data: { id: 'e5', source: 'f', target: 'g' } },
//         { data: { id: 'e6', source: 'f', target: 'h' } }
//     ]
// },


// elements: {
//     nodes: [
//         { data: { id: 'a', parent: 'p' }, position: { x: 215, y: 85 } },
//         { data: { id: 'b', parent: 'p' }, position: { x: 300, y: 85 } },
//         { data: { id: 'p' } }
//     ],
//     edges: [
//         { data: { id: 'e1', source: 'a', target: 'b' } }
//     ]
// },



// // https://stackoverflow.com/questions/27280708/how-do-i-make-classes-work-in-cytoscape-js
// var cy = cytoscape({
//     container: document.getElementById('cy'),
//     style: [
//         {
//             selector: 'node',
//             style: {
//                 'label': 'data(id)'
//             }
//         },

//         {
//             selector: '.ClassName1',
//             style: {
//                 'width': 8,
//                 'height': 8,
//                 'label': ''
//             }
//         }
//     ],
//     elements: {
//         nodes: [
//               { data: { id: 'explore'}, classes: 'ClassName1'},
//               { data: { id: 'discover' } }
//         ],
//         edges: [
//               { data: { source: 'explore', target: 'discover' } }
//         ]
//    },
// });