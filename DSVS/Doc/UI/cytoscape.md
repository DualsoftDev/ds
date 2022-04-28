~/tmp/cytoscape.js/documentation/demos/compound-nodes

- [Attention Required! | Cloudflare](https://codepen.io/yeoupooh/pen/BjWvRa)

- 삭제, UNDO/REDO 샘플 [cytoscape-undo-redo.js demo](https://ivis-at-bilkent.github.io/cytoscape.js-undo-redo/demo.html)

[생활코딩 마인드맵 라이브러리 cytoscape 사용법](https://velog.io/@takeknowledge/%EC%83%9D%ED%99%9C%EC%BD%94%EB%94%A9-%EB%A7%88%EC%9D%B8%EB%93%9C%EB%A7%B5-cytoscape-%ED%99%9C%EC%9A%A9-%ED%94%84%EB%A1%9C%EC%A0%9D%ED%8A%B8-56k4in7315)

- [Getting started with Cytoscape.js &middot; Cytoscape.js](https://blog.js.cytoscape.org/2016/05/24/getting-started/)


- [Cytoscape.js](https://js.cytoscape.org/#getting-started/including-cytoscape.js)
    License: Cytoscape.js is an open-source graph theory (a.k.a. network) library written in JS


- line style : [How can i customize edges in cytoscape.js so that lines are dashed and animated/flashing?](https://stackoverflow.com/questions/56001559/how-can-i-customize-edges-in-cytoscape-js-so-that-lines-are-dashed-and-animated)
    	'line-style': 'dashed',




#### selection
- class 이용 : [How to select nodes by class in cytoscape.js?](https://stackoverflow.com/questions/45572034/how-to-select-nodes-by-class-in-cytoscape-js)



// 'a' 는 특정 classes 이름
cy.$('.a').layout({name:'circle'}).run();
cy.elements().not(cy.$('.a')).layout({name:'circle'}).run();
cy.nodes(':parent');        // https://stackoverflow.com/questions/52200858/cytoscape-js-multiple-layouts-different-layout-within-compound-nodes
cy.nodes().not(':parent').length;
cy.elements().not(':parent').length;
cy.nodes(':parent').layout({name:'circle'}).run();

cy.elements('node[parent="C"],node[parent="B"]').layout({name:'circle'}).run();
