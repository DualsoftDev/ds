/*
 * Adapted from Scott Harden's EXCELLENT blog post,
 * "Draw Animated Graphics in the Browser with Blazor WebAssembly"
 * https://swharden.com/blog/2021-01-07-blazor-canvas-animated-graphics/
 */

function removeCanvasEventHandler(name) {
    let events = window.canvasEventMap[name];
    if (events) {
        log.debug(`removing event handlers for [${name}]`);
        window.removeEventListener('click',        events['click']);
        window.removeEventListener('resize',       events['resize']);
        window.removeEventListener('renderjs',     events['renderjs']);
        window.removeEventListener('mouseDown',    events['mouseDown']);
        window.removeEventListener('mouseUp',      events['mouseUp']);
        window.removeEventListener('mouseMove',    events['mouseMove']);
        window.removeEventListener('beforeunload', events['beforeUnload']);
        delete window.canvasEventMap[name];
    }
}

function getCanvasName(instance) { return instance.invokeMethod('GetCanvasHolderName'); }

function initRenderCommon(instance, renderjs) {
    let name = getCanvasName(instance);
    let resize = () => resizeCanvasToFitWindow(instance, name);

    //let mouseDown = e => onMouseDown(instance, e);
    //let mouseUp = e => onMouseUp(instance, e);
    //let mouseMove = e => onMouseMove(instance, e);
    //let beforeUnload = e => onBeforeUnload(instance, e);    // SPA 에서는 동작하지 않는 듯..

    //let map = window.canvasEventMap;
    if (window.canvasEventMap == undefined)
        window.canvasEventMap = {};
    window.canvasEventMap[name] = {
    //    'resize': resize,
    //    'renderjs': renderjs,
    //    'mouseDown': mouseDown,
    //    'mouseUp': mouseUp,
    //    'mouseMove': mouseMove,
    //    'beforeUnload': beforeUnload
    };

    //// tell the window we want to handle the resize event
    //window.addEventListener("resize", resize);

    //// ... and the mouse events
    //window.addEventListener("mousedown", mouseDown);
    //window.addEventListener("mouseup", mouseUp);
    //window.addEventListener("mousemove", mouseMove);

    //window.addEventListener("beforeunload", beforeUnload);      // { "click", "locationchange", "popState", "hashchange" } 어느 것도 잘 동작하지 않음

    // Call resize now
    resize();
}
/*This is called from the Blazor component's Initialize method*/
export function initRenderJS(canvasHelperInstance) {
    // instance is the Blazor component (in C#) dotnet reference
    let instance = canvasHelperInstance;
    let name = getCanvasName(instance);
    let refreshInterval = instance.invokeMethod('GetRefreshInterval');
    log.debug(`initRenderJS(): Found Name = ${name}, Refresh Interval=${refreshInterval}`);
    let renderjs     = timeStamp => renderJS(instance, timeStamp, 1, refreshInterval);

    initRenderCommon(instance, renderjs);
    // request an animation frame, telling window to call renderJS
    // https://developer.mozilla.org/en-US/docs/Web/API/window/requestAnimationFrame
    window.requestAnimationFrame(renderjs);
}

export function initRenderVideoJS(canvasHelperInstance) {
    console.log(`------------------------------------ initRenderVideoJS`)
    let instance = canvasHelperInstance;
    let renderjs = timeStamp => renderJS(instance, timeStamp, 1, refreshInterval);

    initRenderCommon(instance, renderjs);
}

function sleep(ms) { return new Promise((r) => setTimeout(r, ms)); }

export function isCanvasAvailable(canvasHolderName) {
    log.debug(`isCanvasAvailable(${canvasHolderName})`);
    return window.canvasEventMap && window.canvasEventMap[canvasHolderName];
}

/*This is called whenever we have requested an animation frame*/
function renderJS(instance, timeStamp, numRendered, refreshInterval) {
    let chk = () => window.canvasEventMap[getCanvasName(instance)];
    let renderjs = (timeStamp) => {
        if (chk())
            renderJS(instance, timeStamp, numRendered + 1, refreshInterval);
    }

    if (!chk())
        return;
    // Call the blazor component's [JSInvokable] RenderInBlazor method
    instance.invokeMethodAsync('RenderInBlazor', timeStamp)
        .then(x => {
            log.debug('renderJS()');
            let chk = () => window.canvasEventMap[getCanvasName(instance)];
            if (chk()) {
                // request another animation frame
                if (refreshInterval > 0) {
                    sleep(refreshInterval)
                        .then(x => {
                            if (chk())
                                window.requestAnimationFrame(renderjs);
                        });
                } else if (refreshInterval == 0 || numRendered < 3) {
                    window.requestAnimationFrame(renderjs);
                }
            }
        });
}

/*This is called whenever the browser (and therefore the canvas) is resized*/
function resizeCanvasToFitWindow(instance, name) {
    log.debug(`On resizeCanvasToFitWindow(${name})`);
    // canvasHolder is the ID of the div that wraps the renderfragment
    // content(Canvas) in the blazor app
    let holder = document.getElementById(name);
    if (!holder) {
        log.warn(`resizeCanvasToFitWindow(${name}) failed to find holder ===> Removing handlers`);
        removeCanvasEventHandler(name);
        return;
    }
    // find the canvas within the renderfragment
    let canvas = holder.querySelector('canvas');
    if (canvas) {
        /* kwak */
        // resize the canvas to fit the holder window
        const [ow, oh] = [canvas.width, canvas.height];
        const nw = holder.clientWidth;
        const nh = parseInt((1.0 * nw * oh / ow) + 0.5, 10);
        [canvas.width, canvas.height] = [nw, nh];

        //canvas.width = holder.clientWidth;
        //canvas.height = holder.clientHeight;

        if (canvas.width == 0 || canvas.height == 0)
            console.error(`resizeCanvasToFitWindow(${name}) failed to resize canvas to ${canvas.width} x ${canvas.height}`);


        console.log(`canvas.width x height = ${canvas.width} x ${canvas.height}`);

        // 배경 이미지 설정
        ////canvas.style = "background: url('https://www.google.com/images/branding/googlelogo/2x/googlelogo_light_color_272x92dp.png')" >
        //canvas.style.background = "url('https://www.google.com/images/branding/googlelogo/2x/googlelogo_light_color_272x92dp.png')";
        //canvas.style.backgroundSize = "cover"; // 배경 이미지를 캔버스 크기에 맞게 늘립니다.



        // resize the canvas to fit the whole window area
        //canvas.width = window.innerWidth;
        //canvas.height = window.innerHeight;

        // Call the blazor component's [JSInvokable] ResizeInBlazor method
        instance.invokeMethodAsync('ResizeInBlazor', canvas.width, canvas.height);
    }
}


////Handle the window.mouse{down, up, move} event
//function onMouseEvent(instance, e, eventName) { // eventName = {'OnMouseDown', 'OnMouseUp', 'OnMouseMove'}
//    let name = getCanvasName(instance);
//    if (name == e.srcElement.parentElement.id) {
//        var args = canvasMouseEventArgs(eventName, e);
//        instance.invokeMethodAsync(eventName, args);
//    }
//}
//function onMouseDown(instance, e) { onMouseEvent(instance, e, 'OnMouseDown'); }
//function onMouseUp  (instance, e) { onMouseEvent(instance, e, 'OnMouseUp'); }
//function onMouseMove(instance, e) { onMouseEvent(instance, e, 'OnMouseMove'); }

//function onBeforeUnload(instance, e) {
//    log.debug("onBeforeUnload() for ", getCanvasName(instance));
//}

export function getCanvasDimension(holderName) {
    log.info(`-------------------- getCanvasDimension(${holderName})`);
    let holder = document.getElementById(holderName);
    if (!holder) {
        log.warn(`Holder is null for ${holderName}`);
        return { width: 0, height: 0}
    }
    // find the canvas within the renderfragment
    let canvas = holder.querySelector('canvas');
    if (canvas) {
        return {
            width: holder.clientWidth,
            height: holder.clientHeight
        };
    }

    return null;
}

export function disposeCanvas(canvasHolderName) {
    log.debug(`~ disposeCanvas(${canvasHolderName})`);
    removeCanvasEventHandler(canvasHolderName);
}
