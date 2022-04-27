- [Load external js in external html file in Webview [vscode-extensions]](https://stackoverflow.com/questions/63140623/load-external-js-in-external-html-file-in-webview-vscode-extensions)

- [What I've learned so far while bringing VS Code's Webviews to the web](https://blog.mattbierner.com/vscode-webview-web-learnings/)
    - <webview> 태그

## extension <-> webview 통신
#####  extension -> webview
https://stackoverflow.com/questions/61800330/how-can-i-save-the-whole-body-of-webview-in-vscode
```js
function exportToSVG(type, name) {
    // Saving the SVG is delegated to the extension to allow asking the user for a target file.
    const svg = document.querySelectorAll('svg')[0];
    const args = {
        command: "saveSVG",
        name: name,
        type: type,
        svg: svg.outerHTML
    };

    vscode.postMessage(args);
}
```
##### webview -> extension
- [Calling vscode Extension for data from webview](https://stackoverflow.com/questions/56830928/calling-vscode-extension-for-data-from-webview)
```js
(function() {
            const vscode = acquireVsCodeApi();
            const counter = document.getElementById('lines-of-code-counter');

            let count = 0;
            setInterval(() => {
                counter.textContent = count++;

                // Alert the extension when our cat introduces a bug
                if (Math.random() < 0.001 * count) {
                    vscode.postMessage({
                        command: 'alert',
                        text: '🐛  on line ' + count
                    })
                }
            }, 100);
        }())
```

- [#DevHack: Open custom VSCode WebView panel and focus input](https://www.eliostruyf.com/devhack-open-custom-vscode-webview-panel-focus-input/)


##### webview html 에서 로컬 javascript 로딩 ---> getNonce() 이용!!!
- [webview-sample 참고](https://github.com/microsoft/vscode-extension-samples.git)
    - https://github.com/microsoft/vscode-extension-samples/blob/main/webview-sample/src/extension.ts
    - nonce, ...

- [GitHub - SAP/vscode-webview-rpc-lib: Provides a conventient way to communicate between VSCode extension and his Webviews. Use RPC calls to invoke functions on the webview and receive callbacks.](https://github.com/SAP/vscode-webview-rpc-lib)
```html
<head>
    <script>var exports = {};</script>
    <script type="module" src="vscode-resource:/node_modules/@sap-devx/webview-rpc/out.browser/rpc-common.js"></script>
    <script type="module" src="vscode-resource:/node_modules/@sap-devx/webview-rpc/out.browser/rpc-browser.js"></script>
    <script type="module" src="vscode-resource:/out/media/main.js"></script>
</head>
```


##### Context menu
- [Workaround to allow Popups in VSCode WebView Extension](https://stackoverflow.com/questions/56692461/workaround-to-allow-popups-in-vscode-webview-extension)
- xxx [How to add a custom right-click menu to a webpage?](https://stackoverflow.com/questions/4909167/how-to-add-a-custom-right-click-menu-to-a-webpage)
- QuickPick
    - [301 Moved Permanently](https://github.com/microsoft/vscode-extension-samples.git)
## Resources
- [vscode-webview-ui-toolkit/getting-started.md at main · microsoft/vscode-webview-ui-toolkit](https://github.com/microsoft/vscode-webview-ui-toolkit/blob/main/docs/getting-started.md)



- [https://betterprogramming.pub/how-to-add-webviews-to-a-visual-studio-code-extension-69884706f056](https://betterprogramming.pub/how-to-add-webviews-to-a-visual-studio-code-extension-69884706f056)

- [Webview API](https://code.visualstudio.com/api/extension-guides/webview)

- [vscode-extension-samples/webview-sample at main · microsoft/vscode-extension-samples](https://github.com/microsoft/vscode-extension-samples/tree/main/webview-sample)


- [Webview UI Toolkit for Visual Studio Code](https://code.visualstudio.com/blogs/2021/10/11/webview-ui-toolkit)
- [How to Build a Webview-Powered VS Code Extension with LWC](https://developer.salesforce.com/blogs/2021/04/how-to-build-a-webview-powered-vs-code-extension-with-lightning-web-components)

- [Develop a vscode extension based on WebView from scratch](https://chowdera.com/2021/09/20210927231637417q.html)

- CAT view sample
    - [vscode-extension-samples/extension.ts at main · microsoft/vscode-extension-samples](https://github.com/microsoft/vscode-extension-samples/blob/main/webview-sample/src/extension.ts)

- ToDO list webview
    - [GitHub - kiegroup/kogito-tooling-examples at master](https://github.com/kiegroup/kogito-tooling-examples/tree/master)    

- [Building The Visual Studio Code Extension For WebPageTest - WebPageTest Blog](https://blog.webpagetest.org/posts/vscode/)    


- ?? [Simplifying VS Code Webview Development with vscode-page](https://dev.to/foxgem/simplifying-vs-code-webview-development-with-vscode-page-13c3)


## debugging webview
- command palette : Open Webview Developer Tools
