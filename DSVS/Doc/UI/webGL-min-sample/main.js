// https://codepen.io/gmem/pen/qBOrKeB


// The goal of this is to create JSON that is completely compatible with CircleCI's
// YAML parser.

let baseConfig = {version:2.1, workflows:{}, jobs:{}};

// Workflow node.
function WorkflowNode() {
  this.addInput("Job 1", "job");
  this.addOutput("Workflow", "workflow")
  this.addProperty("Name", "build");
  this.widget = this.addWidget("text", "Name", "build", "Name");
}
WorkflowNode.title = "Workflow";
WorkflowNode.prototype.onExecute = function() {
  let inputs = this.inputs;
  if (this.getInputData(inputs.length - 1) != undefined) {
    this.addInput(`Job ${inputs.length + 1}`, "job")
  }
  let thisWorkflowConfig = baseConfig;
  thisWorkflowConfig["workflows"][this.getInputOrProperty("Name")] = {};
  for (let i = 0; i < inputs.length; i++) {
    let val = this.getInputData(i);
    for (var y in val) {
      thisWorkflowConfig["jobs"][y] = val[y];
      if (!thisWorkflowConfig["workflows"][this.getInputOrProperty("Name")]["jobs"]) {
        thisWorkflowConfig["workflows"][this.getInputOrProperty("Name")]["jobs"] = [];
      }
      thisWorkflowConfig["workflows"][this.getInputOrProperty("Name")]["jobs"].push(y);
    }
  }
  this.setOutputData(0, thisWorkflowConfig);
} 
LiteGraph.registerNodeType("circleci/workflow", WorkflowNode);

// Job node.
function JobNode() {
  this.addInput("Executor", "executor");
  this.addProperty("Name", "build");
  this.widget = this.addWidget("text", "Name", "build", "Name");
  this.addInput("Step 1", "step");
  this.addOutput("Job", "job");
}
JobNode.title = "Job";
JobNode.prototype.onExecute = function() {
  let inputs = this.inputs;
  // Step inputs start @ 1.
  if (this.getInputData(inputs.length - 1) != undefined) {
    this.addInput(`Step ${inputs.length}`, "step")
  }
  let exec = this.getInputData(0);
  let jobStruct = {[this.getInputOrProperty("Name")]: {"steps": []}};
  for (var v in exec) {
    jobStruct[this.getInputOrProperty("Name")][v] = exec[v];
  }
  if (this.getInputData(1)) {
    for (let i = 1; i < inputs.length - 1; i++) {
      debugger;
      let val = this.getInputData(i);
      if (typeof val == "object") {
        for (var v in val) {
          debugger;
         jobStruct[this.getInputOrProperty("Name")]["steps"].push({[v]: val[v]});
        }
      }
      else { jobStruct[this.getInputOrProperty("Name")]["steps"].push(val); }
    }
  }
  this.setOutputData(0, jobStruct);
} 
LiteGraph.registerNodeType("circleci/job", JobNode);

// Checkout node.
function CheckoutNode() {
  this.addProperty("Command", "checkout");
  this.addOutput("Step", "step");
}
CheckoutNode.title = "Checkout";
CheckoutNode.prototype.onExecute = function() {
  this.setOutputData(0, this.getInputOrProperty("Command"));
} 
LiteGraph.registerNodeType("circleci/checkout", CheckoutNode);
  
// Step node.
function StepNode() {
  this.addProperty("Run Command", "string");
  this.widget = this.addWidget("text", "Run Command", "", "Run Command");
  this.addOutput("Step", "step");
}
StepNode.title = "Run Command";
StepNode.prototype.onExecute = function() {
  this.setOutputData(0, {run: this.getInputOrProperty("Run Command")});
}

LiteGraph.registerNodeType("circleci/run", StepNode);

// Executor node.
function ExecutorNode() {
  this.addProperty("Type", "executorType");
  this.widget = this.addWidget("text", "Type", "", "Type");
  this.addProperty("Image", "executorImagemage");
  this.widget = this.addWidget("text", "Image", "", "Image");
  this.addOutput("Executor", "executor");
}
ExecutorNode.title = "Executor";
ExecutorNode.prototype.onExecute = function() {
  let execImg = this.getInputOrProperty("Image");
  let execType = this.getInputOrProperty("Type");
  
  let execObj = {[execType]: [{image: execImg}]};
  this.setOutputData(0, execObj);
}

LiteGraph.registerNodeType("circleci/executor", ExecutorNode);
  
// Actual graph.
var graph= new LGraph();

var canvas = new LGraphCanvas("#mycanvas", graph);
graph.start()
ctx = document.getElementById("mycanvas").getContext("2d");

// disable firefox browser default context menu



// document.getElementById("mycanvas").addEventListener("contextmenu", e => e.preventDefault());
// document.getElementById("mycanvas").removeEventListener("contextmenu", e => e.preventDefault());
// document.oncontextmenu = () => false;
// $('#mycanvas').unbind('click');

ctx.canvas.width  = (window.innerWidth * 0.6);
ctx.canvas.height = (window.innerHeight * 0.8);
function process() {
  let workflows = graph.findNodesByType("circleci/workflow");
  let config = "";
  workflows.forEach(workflow => {
    config += JSON.stringify(workflow.getOutputData(0), null, 2) + "\n";
  });
  document.getElementById("config").innerHTML = config;
}