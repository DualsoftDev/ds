import * as path from 'path';
import * as vscode from 'vscode';
import * as fs from "fs";
import { parseDSDocument } from './clientParser';
import { visitDSDocument } from './clientVisitor';
import { getWebviewContentD3 } from './webview.d3';
import { getWebviewContentCytoscape } from './webview.cytoscape';
import { assert } from 'console';

let panel: vscode.WebviewPanel | null = null;
let myTextEditor:vscode.TextEditor | null = null;

export function initializeWebview(textEditor:vscode.TextEditor, context: vscode.ExtensionContext) {
    console.log('=== initializing webview for ds.');
    myTextEditor = textEditor;

    context.subscriptions.push(
        vscode.workspace.onDidChangeTextDocument((event) => {
            console.log('Modification detected.' + event);
            updateDSView();
    }),
    );

    function updateDSView() {
        console.log('PATH=', context.extensionPath);

        if (panel == null) {
            // Create and show panel
            panel = vscode.window.createWebviewPanel(
                'dsview',
                'DS view',
                {
                    viewColumn: vscode.ViewColumn.Two,
                    preserveFocus: false,   // If preserveFocus is set, the new webview will not take focus.
                },
                {
                    enableScripts: true,   //  because the document's frame is sandboxed and the 'allow-scripts' permission is not set
                    retainContextWhenHidden: true,

                    // Only allow the webview to access resources in our extension's media directory
                    localResourceRoots: [
                        vscode.Uri.file(path.join(context.extensionPath, 'media')),
                    ]
                }
            );

            const doc = myTextEditor.document;
            context.subscriptions.push(
                // cytoscape.ds.js 에서 처리된 message 를 받음.
                panel.webview.onDidReceiveMessage(msg => {
                    console.log('Got message ', msg);
                    const replaceContent = `${msg.type}: ${msg.args}\n`;
                    vscode.window.showInformationMessage(replaceContent);
                    // const lastLine = doc.lineAt(doc.lineCount - 1);
                    // myTextEditor.edit((editBuilder) => {
                    // editBuilder.replace(lastLine.range.end, replaceContent);
                })
            );
        }

        const text = myTextEditor.document.getText();

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

        console.log('webview=', panel.webview);

        // And set its HTML content
        // const html = getWebviewContentD3(connections);
        const html = getWebviewContentCytoscape(context.extensionUri, panel.webview, connections);
        panel.webview.html = html;

        // write html string to file 'hello.html'
        const test = context.asAbsolutePath(
            path.join('media', 'test.html')
        );    
        fs.writeFileSync(test, html);

        // // write html string to file abc.txt in the workspace root
        //  vscode.workspace.openTextDocument({ content: html, language: 'html' })
        //  .then(doc => {
        //      vscode.window.showTextDocument(doc); });

    }

    context.subscriptions.push(
        vscode.commands.registerCommand('ds.dsview', () => { updateDSView(); })
    );
}
