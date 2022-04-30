// import { ParserRuleContext } from "antlr4ts";
// import { ParseTreeWalker } from "antlr4ts/tree";
// import { dsListener } from "../server-bundle/dsListener";
// import { CallContext, CausalPhraseContext, dsParser, FlowContext, ListingContext, ProgramContext, SystemContext, TaskContext } from "../server-bundle/dsParser";


// const elementMap = new Map<string, string>();
// interface NodeInfo {
// 	id:string;			// system.task.segment
// 	label:string;		// segment name
// }
// interface EdgeInfo {
//     flowId:string;		// flow id
//     source:NodeInfo;
//     target:NodeInfo;
//     operator:string;
// }


// class GraphWalker implements dsListener
// {
//     public elements: (NodeInfo|EdgeInfo)[] = [];
//     _system:string = null;
//     _task:string = null;
//     _flowId:string = null;
//     enterEveryRule(ctx: ParserRuleContext)
//     {
//         if (ctx instanceof TaskContext)
//             console.log('every rule');
//     }
//     enterProgram(ctx: ProgramContext)
//     {
//         console.log('program');
//     }
// 	enterSystem(ctx: SystemContext):void {this._system = ctx.children[1].text;}
// 	enterTask(ctx: TaskContext):void {this._task = ctx.children[1].text;}
//     enterListing(ctx: ListingContext) {
//         const name = ctx.children[0].text;
//         const id = `${this._system}.${this._task}.${name}`;
//         const label = name;
//         this.elements.push({id, label});
//     }
//     enterCall(ctx: CallContext) {
//         const name = ctx.children[0].text;
//         const id = `${this._system}.${this._task}.${name}`;
//         const callDetails = ctx.children[3].text;
//         const label = `${name}\n${callDetails}`;
//         this.elements.push({id, label});
//     }
//     enterFlow(ctx: FlowContext) {this._flowId = ctx.children[1].text;}
//     visitCausalPhrase(ctx: CausalPhraseContext)
//     {
//         console.log('object');
//     }
// }


// class GraphWalker extends AbstractParseTreeVisitor<void> implements dsVisitor<void> {
//     protected defaultResult(): void {
//         throw new Error("Method not implemented.");
//     }
//     public elements: (NodeInfo|EdgeInfo)[] = [];

//     _system:string = null;
//     _task:string = null;
//     _flowId:string = null;
//     visitProgram(ctx: ProgramContext) {
//         console.log('Program');
//     }
//     visitSystem(ctx: SystemContext) {this._system = ctx.children[1].text;}
//     visitTask(ctx: TaskContext) {this._task = ctx.children[1].text;}
//     visitListing(ctx: ListingContext) {
//         const name = ctx.children[0].text;
//         const id = `${this._system}.${this._task}.${name}`;
//         const label = name;
//         this.elements.push({id, label});
//     }

//     visitCall(ctx: CallContext) {
//         const name = ctx.children[0].text;
//         const id = `${this._system}.${this._task}.${name}`;
//         const callDetails = ctx.children[3].text;
//         const label = `${name}\n${callDetails}`;
//         this.elements.push({id, label});
//     }

//     visitFlow(ctx: FlowContext) {this._flowId = ctx.children[1].text;}
//     visitCausalPhrase(ctx: CausalPhraseContext)
//     {
//         console.log('object');
//     }
// }


// export function visitGraph(parser:dsParser)
// {
//     parser.reset();

// 	const listener_ = new GraphWalker();
// 	const listener:dsListener = listener_;
// 	parser.removeParseListeners();
// 	ParseTreeWalker.DEFAULT.walk(listener, parser.program());
//     const xxx = listener_.elements;

// 	return listener_.elements;

// }
