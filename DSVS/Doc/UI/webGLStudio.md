# litegraph.js
- Author: [jagenjo - Overview](https://github.com/jagenjo)


[GitHub - jagenjo/litegraph.js: A graph node engine and editor written in Javascript similar to PD or UDK Blueprints, comes with its own editor in HTML5 Canvas2D. The engine can run client side or server side using Node. It allows to export graphs as JSONs to be included in applications independently.](https://github.com/jagenjo/litegraph.js)
$ git clone https://github.com/jagenjo/litegraph.js.git
$ cd litegraph.js
$ npm install
$ node utils/server.js
Example app listening on port 80!




[GitHub - jagenjo/webglstudio.js: A full open source 3D graphics editor in the browser, with scene editor, coding pad, graph editor, virtual file system, and many features more.](https://github.com/jagenjo/webglstudio.js)

[WebGLStudio - WOW!! Stunning Game Engine &amp; Editor (that&#39;s Free &amp; Open Source)](https://www.youtube.com/watch?v=EwyVjq5stI4)


[Elephant Syst&egrave;me Nodal](https://moiscript.weebly.com/elephant-systegraveme-nodal.html?ref=morioh.com&utm_source=morioh.com)


## minimal sample
[Attention Required! | Cloudflare](https://codepen.io/gmem/pen/qBOrKeB)

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <title>CircleCI Configuration Creator</title>
	<link rel="stylesheet" type="text/css" href="https://tamats.com/projects/litegraph/css/litegraph.css">
	<link rel="stylesheet" type="text/css" href="test.css">
	<script type="text/javascript" src="https://unpkg.com/litegraph.js@0.7.5/build/litegraph.js"></script>
</head>
<body>
<h1>CircleCI Configuration Creator</h1>
<p>right click for context menu</p>
  <div class="controls">
<button onclick="graph.add(LiteGraph.createNode('circleci/workflow'));">Workflow</button>
<button onclick="graph.add(LiteGraph.createNode('circleci/job'));">Job</button>
<button onclick="graph.add(LiteGraph.createNode('circleci/executor'));">Executor</button>
<button onclick="graph.add(LiteGraph.createNode('circleci/checkout'));">Checkout</button>
<button onclick="graph.add(LiteGraph.createNode('circleci/run'));">Run</button>
<button onclick="process()"">Generate</button>
    
  </div>
<div class="app">
<canvas id="mycanvas" height=500 width=1000></canvas>
<pre><code id="config">{
  version:2.1, 
  workflows:{}, 
  jobs:{}
}</code></pre>
  </div>

  <script type="text/javascript" src="main.js"></script>

</body>
</html>
```


[Designing a Dataflow Editor With TypeScript and React | Protocol Labs Research](https://research.protocol.ai/blog/2021/designing-a-dataflow-editor-with-typescript-and-react/)


