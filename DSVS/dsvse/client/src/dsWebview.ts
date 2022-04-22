import * as path from 'path';
import * as vscode from 'vscode';
import { parseDSDocument } from './clientParser';
import { visitDSDocument } from './clientVisitor';
import { getWebviewContentD3 } from './webview.d3';
import { getWebviewContentCytoscape } from './webview.cytoscape';
import { assert } from 'console';

let panel: vscode.WebviewPanel | null = null;
export function initializeWebview(context: vscode.ExtensionContext) {
    console.log('=== initializing webview for ds.');

    const text = vscode.window.activeTextEditor.document.getText();
    visitDSDocument(text);

    context.subscriptions.push(
        vscode.workspace.onDidChangeTextDocument((event) => {
            console.log('Modification detected.' + event);
            updateDSView();
        }),
    );

    function updateDSView() {

        if (panel == null) {
            // Create and show panel
            panel = vscode.window.createWebviewPanel(
                'dsview',
                'DS view',
                vscode.ViewColumn.Two,
                { enableScripts: true }  //  because the document's frame is sandboxed and the 'allow-scripts' permission is not set
            );
        }

        const text = vscode.window.activeTextEditor.document.getText();

        // {source: "Microsoft", target: "Amazon", type: "licensing"},
        const connections =
            Array.from(visitDSDocument(text))
                .flatMap(causal => {
                    const c = causal;
                    switch(causal.op)
                    {
                        case '>': return {source: c.l, target: c.r, solid: true};
                        case '<': return {source: c.r, target: c.l, solid: true};
                        case '|>': return {source: c.l, target: c.r, solid: false};
                        case '<|': return {source: c.r, target: c.l, solid: false};
                        default:
                            assert(false, `invalid operator: ${causal.op}`);
                            break;
                    }
                    const edge = causal.op == '>';
                    return {source: causal.l, target: causal.r, solid: edge};
                })
                //.join(',')
            ;
        console.log('finished parseDSDocument on client side.' + connections);

        // And set its HTML content
        // const html = getWebviewContentD3(connections);
        const html = getWebviewContentCytoscape(connections);
        panel.webview.html = html;

    }

    context.subscriptions.push(
        vscode.commands.registerCommand('ds.dsview', () => { updateDSView(); })
    );
}

