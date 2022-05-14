import * as path from 'path';
import * as vscode from 'vscode';
import * as fs from "fs";
import { parserFromDocument } from '@dualsoft/parser';
import { getWebviewContentD3 } from './webview.d3';
import { getWebviewContentCytoscape } from './webview.cytoscape';
import { getElements } from '@dualsoft/parser/cytoscapeVisitor';

// let myTextEditor:vscode.TextEditor | null = null;

const panelMap = new Map<string, vscode.WebviewPanel>();

function createPanel(filePath: string, context: vscode.ExtensionContext)
{
    if (panelMap.get(filePath))
        return panelMap.get(filePath);

    const panel = vscode.window.createWebviewPanel(
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
    panelMap.set(filePath, panel);

    panel.onDidDispose(() => {
        // When the panel is closed, cancel any future updates to the webview content
        panelMap.delete(filePath);
        panel.dispose();
    }, null, context.subscriptions);

    context.subscriptions.push(
        // cytoscape.ds.js 에서 처리된 message 를 받음.
        panel.webview.onDidReceiveMessage(msg => {
            console.log('Got message ', msg);
            const replaceContent = `${msg.type}: ${msg.args}\n`;
            vscode.window.showInformationMessage(replaceContent);
            // const lastLine = doc.lineAt(doc.lineCount - 1);
            // de.window.activeTextEditor.edit((editBuilder) => {
            // editBuilder.replace(lastLine.range.end, replaceContent);
        })
    );

    return panel;
}

export function initializeWebview(textEditor:vscode.TextEditor, context: vscode.ExtensionContext) {
    console.log('=== initializing webview for ds.');

    const key = textEditor.document.fileName;
    let panel = panelMap.get(key);
    if (! panel) {
        // Create and show panel
        panel = createPanel(key, context);



        panelMap.set(key, panel);
    }

    context.subscriptions.push(
        vscode.workspace.onDidChangeTextDocument((event) => {
            console.log('Modification detected.' + event);
            const key = vscode.window.activeTextEditor.document.fileName;
            const panel = panelMap.get(key);
            if (panel)
            {
                updateDSView(panel);                
                // panel.reveal();
            }
        }),
    );

    function updateDSView(panel) {
        const key = vscode.window.activeTextEditor.document.fileName;
        console.log('PATH=', key);

        const text = vscode.window.activeTextEditor.document.getText();
        const parser = parserFromDocument(text);

        const elements = getElements(parser);

        // // {source: "Microsoft", target: "Amazon", type: "licensing"},
        // const links = visitLinks(parser);
        // const connections:CausalLink[] =
        //     links
        //     .flatMap(causal => {
        //         const c = causal;
        //         const op = causal.op;
        //         switch(op)
        //         {
        //             case '|>':
        //             case '>': return [{l: c.l, r: c.r, op}];

        //             case '<|':
        //             case '<': return [{l: c.r, r: c.l, op}];

        //             case '<||>':
        //             case '|><|':
        //                 return [ {l: c.l, r: c.r, op:'|>'},
        //                          {l: c.r, r: c.l, op:'|>'}];

        //             case '><|':
        //                 return [ {l: c.l, r: c.r, op:'>'},
        //                          {l: c.r, r: c.l, op:'|>'}];
        //             case '|><':
        //                 return [ {l: c.l, r: c.r, op:'|>'},
        //                          {l: c.r, r: c.l, op:'>'}];


        //             default:
        //                 assert(false, `invalid operator: ${op}`);
        //                 break;
        //         }
        //         return [{l: causal.l, r: causal.r, op}];
        //     })
        //     //.join(',')
        //     ;

        // parser.reset();
        // const systemInfos:SystemGraphInfo[] = enumerateSystemInfos(parser);
        
        // console.log('finished enumerateSystemInfos on client side.' + connections);

        // console.log('webview=', panel.webview);

        // And set its HTML content
        // const html = getWebviewContentD3(connections);
        const html = getWebviewContentCytoscape(key, context.extensionUri, panel.webview, elements);
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
        vscode.commands.registerCommand('ds.dsview', () => {
            // 아이콘 누를 때 수행 됨.
            const key = vscode.window.activeTextEditor.document.fileName;
            panel = createPanel(key, context);
            updateDSView(panel);
            panel.reveal();
        })
    );
}
