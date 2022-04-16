import * as path from 'path';
import * as vscode from 'vscode';

export function initializeWebview(context: vscode.ExtensionContext)
{
    console.log('initializing webview for ds.');

    // const panel = vscode.window.createWebviewPanel(
    //   'dsview',
    //   'DS view',
    //   vscode.ViewColumn.One,
    //   {}
    // );

    // // And set its HTML content
    // panel.webview.html = getWebviewContent();


    context.subscriptions.push(
        vscode.commands.registerCommand('ds.dsview', () => {
          // Create and show panel
          const panel = vscode.window.createWebviewPanel(
            'dsview',
            'DS view',
            vscode.ViewColumn.One,
            {}
          );

          const text = vscode.window.activeTextEditor.document.getText();
    
          // And set its HTML content
          panel.webview.html = getWebviewContent(text);
        })
    );
}

function getWebviewContent(text:string) {
    return `<!DOCTYPE html>
  <html lang="en">
  <head>
      <meta charset="UTF-8">
      <meta name="viewport" content="width=device-width, initial-scale=1.0">
      <title>Cat Coding</title>
  </head>
  <body>
      <p>${text}</p>
  </body>
  </html>`;
}