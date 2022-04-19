import * as path from 'path';
import * as vscode from 'vscode';
import { parseDSDocument } from './clientParser';
import { getWebviewContentD3 } from './webview.d3';
import { getWebviewContentCytoscape } from './webview.cytoscape';

let panel: vscode.WebviewPanel | null = null;
export function initializeWebview(context: vscode.ExtensionContext) {
    console.log('=== initializing webview for ds.');

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
            Array.from(parseDSDocument(text))
                .map(causal => {
                    const edge = causal.op == '>';
                    return {source: causal.left, target: causal.right, solid: edge};
                })
                //.join(',')
            ;
        console.log('finished parseDSDocument on client side.' + connections);

        // And set its HTML content
        // panel.webview.html = getWebviewContentD3(connections);
        panel.webview.html = getWebviewContentCytoscape(connections);

    }

    context.subscriptions.push(
        vscode.commands.registerCommand('ds.dsview', () => { updateDSView(); })
    );
}

