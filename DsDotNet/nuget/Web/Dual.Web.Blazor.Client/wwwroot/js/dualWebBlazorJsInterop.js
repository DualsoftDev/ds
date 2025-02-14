// This is a JavaScript module that is loaded on demand. It can export any number of
// functions, and may import other JavaScript modules if required.


const theTooltipId = 'theTooltip';
let functionsMap = {};

function _generatePseudoGuid() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16 | 0,
            v = c == 'x' ? r : (r & 0x3 | 0x8);
        return v.toString(16);
    });
}

export function jwt2json(token) {
    const parts = token.split('.');
    const header = JSON.parse(atob(parts[0]));
    const payload = JSON.parse(atob(parts[1]));
    return JSON.stringify(payload);
}

(function (window) {
    window.browser = {
        ping: () => "pong",
        showPrompt: message => prompt(message, 'Type anything here'),
        showConfirm: (message) => confirm(message),
        showAlert: (message) => alert(message),

        getUserAgent: () => navigator.userAgent,
    };
    window.dimension = {
        getBrowserWindowDimension: function () {
            return {
                width: window.innerWidth,
                height: window.innerHeight
            };
        },

        getImageDimension: function (imageUrl) {
            return new Promise((resolve, reject) => {
                const img = new Image();
                img.onload = () => resolve({ width: img.width, height: img.height });
                img.onerror = () => reject();
                img.src = imageUrl;
            });
        },

        getVideoDimension: function (videoUrl) {
            return new Promise((resolve, reject) => {
                const video = document.createElement('video');
                video.onloadedmetadata = () => resolve({ width: video.videoWidth, height: video.videoHeight });
                video.onerror = () => reject();
                video.src = videoUrl;
                video.load();
            });
        },

        getMediaDimension: function (url) {
            const imageExtensions = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'tiff', 'bmp'];
            const videoExtensions = ['mp4', 'webm', 'ogg', 'mov', 'avi', 'mkv'];
            const extension = url.split('.').pop().toLowerCase();
            if (imageExtensions.includes(extension)) return this.getImageDimension(url);
            if (videoExtensions.includes(extension)) return this.getVideoDimension(url);
            return Promise.reject(new Error('Unsupported media type'));
        },

        // 사용자가 설정한 배율이 적용된 screen 해상도 값
        getScreenDimension: function () {
            return {
                width: window.screen.width,
                height: window.screen.height
            };
        },

        getClientRectById: function (elementId) {
            var img = document.getElementById(elementId);
            var rect = img.getBoundingClientRect();
            return rect;    // rect.left, rect.top, rect.width, rect.height
        },

        // 사용자가 설정한 배율과 상관없는 screen (모니터) 본래의 해상도 값
        getScreenHardwareResolution: function () {
            let w = window.screen.width * window.devicePixelRatio;
            let h = window.screen.height * window.devicePixelRatio;
            return {
                width: parseInt((w * 10 + 1) / 10),
                height: parseInt((h * 10 + 1) / 10)
            };
        },

        isFullScreen: function () { return document.fullscreenElement !== null; },

    };

    window.function = {
        createMemberCallFunction: function (dotnetObj, dotnetObjMethodName, description) {
            let uniqueKey = `func_${description}_${_generatePseudoGuid()}`; // 예시로 현재 시간을 키로 사용
            functionsMap[uniqueKey] = function () {
                dotnetObj.invokeMethodAsync(dotnetObjMethodName);
            };
            return uniqueKey;
        },
        invokeStoredFunction: function (key) {
            if (functionsMap[key]) {
                functionsMap[key]();
            } else {
                console.error("Function not found for key: " + key);
            }
        },
        evalSnippet: (snippet) => eval(snippet),
        isFunctionExists: (functionName) => typeof window[functionName] === 'function',
    };
    window.eventHandler = {

        // eventName: "resize"
        // dotnetEventHandlerName : "OnWindowResize"
        addEventHandler: function (dotnetObj, eventName, dotnetEventHandlerName) {
            console.log(`addEventHandler: ${eventName}`)
            window.addEventListener(eventName, function () {
                console.log(`detected ${eventName}`)
                dotnetObj.invokeMethodAsync(dotnetEventHandlerName);
            });
        },
        addEventHandlerWithFunctionKey: function (eventName, functionMapKey) {
            console.log(`addEventHandlerWithFunctionKey(${eventName}, ${functionMapKey})`)
            window.addEventListener(eventName, functionsMap[functionMapKey]);
        },

        removeEventHandlerWithFunctionKey: function (eventName, functionMapKey) {
            console.log(`removeEventHandlerWithFunctionKey: ${eventName}: ${functionMapKey}`)
            window.removeEventListener(eventName, functionsMap[functionMapKey]);
            functionsMap[functionMapKey] = null;
        },


        addEventHandlers: function (dotnetObj, targetElement, eventNames) {
            eventNames.forEach(eventName => {
                console.log(`addEventHandlers: ${eventName}`);
                targetElement.addEventListener(eventName, (event) => {
                    //console.log(`detected ${eventName} with event=${event}`)
                    var args = jsMouseArgs(eventName, event);
                    dotnetObj.invokeMethodAsync("OnMouse", args);
                });
            });
        },

        addEventHandlersWithElementId: function (dotnetObj, targetElementId, eventNames) {
            let targetElement = document.getElementById(targetElementId);
            addEventHandlers(dotnetObj, targetElement, eventNames)
        },
    };

    window.dom = {
        // https://stackoverflow.com/questions/58280795/how-can-i-change-css-directlywithout-variable-in-blazor
        setStyle: function (element, attributeKey, attributeValue) {
            //console.log('setStyle', element, attributeKey, attributeValue);
            element.style[attributeKey] = attributeValue;
            //console.log('after setStyle', element, attributeKey, attributeValue);
        },

        setStyleWithElementId: (id, attributeKey, attributeValue) => document.getElementById(id).style[attributeKey] = attributeValue,
        getStyle: (element, attributeKey) => element.style[attributeKey],

        getStyleWithElementId: (id, attributeKey) => document.getElementById(id).style[attributeKey],


        setAttribute: function (element, attributeKey, attributeValue) {
            //console.log('setAttribute', element, attributeKey, attributeValue);
            element[attributeKey] = attributeValue;
        },
        setAttributeWithElementId: function (elementId, attributeKey, attributeValue) {
            //console.log('setAttributeWithElementId', elementId, attributeKey, attributeValue);
            var element = document.getElementById(elementId);
            if (element == null)
                console.log(`setAttributeWithElementId: element is null for ${elementId}`);
            element[attributeKey] = attributeValue;
        },


        getAttribute: (element, attributeKey) => element[attributeKey],
        getAttributeWithElementId: function (elementId, attributeKey) {
            let element = document.getElementById(elementId);
            let ret = element[attributeKey];
            //console.log(`getAttributeWithElementId(${elementId}, ${attributeKey}) returns ${ret}`);
            return ret;
        },

        getElementById: id => document.getElementById(id),
        existsElementWithId: function (elementId) {
            var element = document.getElementById(elementId);
            return element !== null;
        },
        getTagName: element => element.tagName,


        setInnerTextById: function (elementId, newText) {
            const element = document.getElementById(elementId);
            if (element)
                element.innerText = newText;
            else
                console.error(`Element with ID ${elementId} not found.`);
        },

        moveToBody: function (elementId) {
            var element = document.getElementById(elementId);
            console.log(`Moving element [${element}] with id=${elementId} to body`);
            if (element)
                document.body.appendChild(element);
        },
        move: function (elementId, newParentId) {
            var element = document.getElementById(elementId);
            var newParent = document.getElementById(newParentId);
            console.log(`Moving element [${element}] with id=${elementId} to parent [${newParent}] with id=${newParentId}`);
            if (element && newParent)
                newParent.appendChild(element);
        },
        addElementClass: function (elementId, className) {
            var element = document.getElementById(elementId);
            if (element) {
                element.classList.add(className);
            };
        },

        removeElementClass: function (elementId, className) {
            var element = document.getElementById(elementId);
            if (element) {
                element.classList.remove(className);
            }
        },
        replaceElementClass: function (elementId, from, to) {
            var element = document.getElementById(elementId);
            if (element) {
                // 클래스를 제거하고 새로운 클래스 추가
                element.classList.remove(from);
                element.classList.add(to);
            }
        },

        // CSS 에서 사용하는 변수의 값을 구한다.  e.g : variableName = '--bs-primary'
        getCssVariableValue: function (variableName) {
            var value = getComputedStyle(document.documentElement).getPropertyValue(variableName).trim();
            // 색상 값을 RGB 또는 RGBA 형식으로 반환
            if (value.startsWith('#')) {
                // #RRGGBB 형식을 rgba() 형식으로 변환
                var r = parseInt(value.substr(1, 2), 16);
                var g = parseInt(value.substr(3, 2), 16);
                var b = parseInt(value.substr(5, 2), 16);
                return `rgba(${r}, ${g}, ${b}, 1)`;
            }
            return value;
        },
        existsElementWithId: function (elementId) {
            var element = document.getElementById(elementId);
            return element !== null;
        },
    };

    window.tooltip = {
        getTheTooltip: function () {
            // ID가 "theTooltipId"인 요소를 찾습니다.
            let tooltip = document.getElementById(theTooltipId);

            // 요소가 없다면 생성합니다.
            if (!tooltip) {
                tooltip = document.createElement('div');
                tooltip.setAttribute('id', theTooltipId);
                document.body.appendChild(tooltip);
            }

            // 최종적으로 요소를 반환합니다.
            return tooltip;
        },
        showTooltip: function (tooltip, x, y, message) {
            console.log(`showTooltip(${tooltip}, ${x}, ${y}, ${message})`);
            if (tooltip == null) {
                console.error(`NULL tooltip on showTooltip()`);
            } else {
                if (message)
                    tooltip.innerHTML = message;
                tooltip.style.position = 'absolute';
                tooltip.style.padding = '10px';
                //tooltip.style.backgroundColor = 'black';
                //tooltip.style.color = 'white';
                tooltip.style.top = (y) + 'px';
                tooltip.style.left = (x) + 'px';
                tooltip.style.display = 'block';
            }
        },
        showTooltipWithElementId: function (tooltipId, x, y, message) {
            console.log(`showTooltipWithElementId(${tooltipId}, ${x}, ${y}, ${message})`);
            let tooltip = document.getElementById(tooltipId)
            showTooltip(tooltip, x, y, message);
        },

        hideTooltipWithElementId: function (tooltipId) {
            let tooltip = document.getElementById(tooltipId)
            hideTooltip(tooltip);
        },

        showTheTooltip: function (x, y, message) {
            let tooltip = getTheTooltip()
            showTooltip(tooltip, x, y, message);
        },

        hideTooltip: function (tooltip) {
            tooltip.style.display = 'none';
        },
        hideTheTooltip: function () {
            let tooltip = getTheTooltip()
            hideTooltip(tooltip);
        },

        // mouse hover 시, tooltip 을 popup 으로 보여준다.
        showPopup: function (elementId, x, y) {
            var popup = document.getElementById(elementId);
            if (popup) {
                popup.style.display = 'block';
                popup.style.left = x + 'px';
                popup.style.top = y + 'px';
            }
        },

        // mouse hover out 시, tooltip 을 숨긴다.
        hidePopup: function (elementId) {
            var popup = document.getElementById(elementId);
            if (popup) {
                popup.style.display = 'none';
            }
        },
        setDivDisplay: function (tooltipDivId, display) {
            var div = document.getElementById(tooltipDivId);
            div.style.display = display;
        },


        attachTooltipEventsWithMessage: function (targetId, tooltip, message) {
            const targetElement = document.getElementById(targetId);

            if (!targetElement) return;

            targetElement.addEventListener('mouseover', (event) => {
                showTooltip(tooltip, event.clientX, event.clientY, message);
            });

            targetElement.addEventListener('mouseout', () => {
                hideTooltip(tooltip);
            });
        },
        attachTheTooltipEvents: function (targetId, message) {
            let tooltip = getTheTooltip()
            attachTooltipEventsWithMessage(targetId, tooltip, message);
        },
        attachTooltipEvents: function (targetId, tooltipId) {
            let tooltip = document.getElementById(tooltipId);
            attachTooltipEventsWithMessage(targetId, tooltip, null);
        }
    };
    window.media = {
        getImageContentOffset: function (imgId) {
            var img = document.getElementById(imgId);
            if (img == null) {
                console.error(`image is null: ${imgId}`);
                return {
                    left: 0,
                    top: 0,
                    width: 0,
                    height: 0
                };
            }

            // 이미지 요소와 실제 이미지의 크기
            var elemWidth = img.clientWidth;
            var elemHeight = img.clientHeight;
            var naturalWidth = img.naturalWidth;
            var naturalHeight = img.naturalHeight;

            // 이미지 내용의 오프셋 계산
            var offsetX = 0, offsetY = 0, contentWidth = 0, contentHeight = 0;
            if (naturalWidth > 0 && naturalHeight > 0) {
                var widthRatio = elemWidth / naturalWidth;
                var heightRatio = elemHeight / naturalHeight;
                var ratio = Math.min(widthRatio, heightRatio);

                contentWidth = naturalWidth * ratio;
                contentHeight = naturalHeight * ratio;

                offsetX = (elemWidth - contentWidth) / 2;
                offsetY = (elemHeight - contentHeight) / 2;
            }

            console.log(`getImageContentOffset: ${elemWidth.toFixed(2)} x ${elemHeight.toFixed(2)}, `
                + `${naturalWidth.toFixed(2)} x ${naturalHeight.toFixed(2)},  returns (${offsetX.toFixed(2)}, ${offsetY.toFixed(2)})`)
            return {
                left: offsetX,
                top: offsetY,
                width: contentWidth,
                height: contentHeight
            };
        },
        startWebcam: function (videoElementId, canvasElementId) {
            const video = document.getElementById(videoElementId);
            const canvas = document.getElementById(canvasElementId);
            const ctx = canvas.getContext('2d');

            navigator.mediaDevices.getUserMedia({ video: true })
                .then(function (stream) {
                    video.srcObject = stream;
                    video.play();

                    const draw = function () {
                        if (video.paused || video.ended) return;
                        ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
                        requestAnimationFrame(draw);
                    }
                    draw();
                })
                .catch(function (error) {
                    console.error("Error accessing webcam:", error);
                });
        },


        startVideo: function (canvasHelperInstance, canvas, video) {
            console.log(`------------------------- startVideo(${canvasHelperInstance}, ${canvas}, ${video})}`);
            const ctx = canvas.getContext('2d');
            // 비디오 재생
            video.play();

            // 비디오 프레임이 업데이트 될 때마다 캔버스에 그림
            const draw = function () {
                if (video.paused || video.ended) return;
                ctx.drawImage(video, 0, 0, canvas.width, canvas.height);

                const dummyFps = -1;
                canvasHelperInstance.invokeMethodAsync('RenderInBlazor', dummyFps);

                ctx.beginPath();
                ctx.arc(100, 100, 100, 0, 2 * Math.PI, false);
                ctx.fillStyle = "rgba(255, 0, 0, 0.3)";

                ctx.fill();
                ctx.stroke();

                requestAnimationFrame(draw);
            }
            draw();
        },
    };
    window.screen = {
        openFullscreen: function (targetElementId) {
            let elem = targetElementId === null ? document.documentElement : document.getElementById(targetElementId);
            if (elem.requestFullscreen)
                elem.requestFullscreen();
            else if (elem.webkitRequestFullscreen)  /* Safari */
                elem.webkitRequestFullscreen();
            else if (elem.msRequestFullscreen) /* IE11 */
                elem.msRequestFullscreen();
            else {
                console.error(`openFullscreen(${targetElementId}) is not supported.`);
                return false;
            }

            return true;
        },

        closeFullscreen: function () {
            if (document.exitFullscreen)
                document.exitFullscreen();
            else if (document.webkitExitFullscreen) /* Safari */
                document.webkitExitFullscreen();
            else if (document.msExitFullscreen) /* IE11 */
                document.msExitFullscreen();
            else {
                console.error('closeFullscreen() is not supported.');
                return false;
            }

            return true;
        },

        goFullScreen: function(toplevelDivId) {
            console.log('entering full scrren mode');
            // Dual.Nuget 에 정의된 full screen mode 진입 함수 호출
            window.screen.openFullscreen(toplevelDivId);
        },
    };

    window.theme = {
        appendLink: function (href, id) {
            const newThemeLink = document.createElement('link');
            newThemeLink.id = id;
            newThemeLink.href = href;
            newThemeLink.rel = 'stylesheet';
            document.head.appendChild(newThemeLink);
        },
        changeTheme: function (themeName) {
    
            // 먼저 현재 테마를 제거합니다.
            const themesToRemove = ['theme-style', 'theme-style2'];
            themesToRemove.forEach(themeId => {
                const existingThemeLink = document.getElementById(themeId);
                if (existingThemeLink)
                    existingThemeLink.remove();
            });
    
            // 선택한 테마를 로드합니다.
            const devExpressThemes = ['blazing-dark', 'blazing-berry', 'purple', 'office-white'];
            // fix me!!!
            if (devExpressThemes.includes(themeName))
            {
                const href = `_content/DevExpress.Blazor.Themes/${themeName}.bs5.css`
                appendLink(href, 'theme-style');
            }
            else
            {
                const href1 = '_content/DevExpress.Blazor.Themes/bootstrap-external.bs5.min.css';
                appendLink(href1, 'theme-style');
    
                const href2 = `css/switcher-resources/themes/${themeName}/bootstrap.min.css`;
                appendLink(href2, 'theme-style2');
            }
        },
    };


    window.loadScript = (url) => {
        return new Promise((resolve, reject) => {
            if (document.querySelector(`script[src='${url}']`)) {
                console.log(`Reusing exsisting script source url: ${url}`)
                resolve();
                return;
            }

            const script = document.createElement('script');
            script.src = url;
            script.onload = () => resolve();
            script.onerror = () => {
                    console.error(`Script load error for ${url}`)
                    reject(new Error(`Script load error for ${url}`));
            }
            document.head.appendChild(script);
        });
    };

    window.xxx = {};

})(window);

export const ping = () => browser.ping();
export const showPrompt   = browser.showPrompt;
export const showConfirm  = browser.showConfirm;
export const showAlert    = browser.showAlert;
export const getUserAgent = browser.getUserAgent;


export const getBrowserWindowDimension   = dimension.getBrowserWindowDimension;
export const getImageDimension           = dimension.getImageDimension;
export const getClientRectById           = dimension.getClientRectById;
export const getVideoDimension           = dimension.getVideoDimension;
export const getMediaDimension           = dimension.getMediaDimension;
export const getScreenDimension          = dimension.getScreenDimension;
export const getScreenHardwareResolution = dimension.getScreenHardwareResolution;
export const isFullScreen                = dimension.isFullScreen;

export const createMemberCallFunction = window.function.createMemberCallFunction;
export const invokeStoredFunction = window.function.invokeStoredFunction;
export const evalSnippet = window.function.evalSnippet;
export const isFunctionExists = window.function.isFunctionExists;

export const addEventHandler = eventHandler.addEventHandler;
export const addEventHandlers = eventHandler.addEventHandlers;
export const addEventHandlersWithElementId = eventHandler.addEventHandlersWithElementId;
export const addEventHandlerWithFunctionKey = eventHandler.addEventHandlerWithFunctionKey;
export const removeEventHandlerWithFunctionKey = eventHandler.removeEventHandlerWithFunctionKey;

export const changeTheme = theme.changeTheme;
export const appendLink = theme.appendLink;


function setLogFunctions() {
    if (window.disableDebugLog) {
        window.log = {
            debug: function () { }, // 빈 함수로 오버라이드하여 무시 : log.debug("blah blah blah");
            info: console.log,
            warn: function (...args) { console.log('%c' + args.join(' '), 'background: yellow; color: black;'); },
            error: console.error,
        };
    } else {
        window.log = {
            debug: function (...args) { console.log('%c' + args.join(' '), 'color: gray;'); },
            info: console.log,
            warn: function (...args) { console.log('%c' + args.join(' '), 'background: yellow; color: black;'); },
            error: console.error,
        };
    }
}

// can't : export const debug = log.debug;  setLogFunctions() 호출 전까지는 아직 log.debug 등이 만들어 지지 않은 상태
export function debug(message) { window.log.debug(message); }
export function info(message) { window.log.info(message); }
export function warn(message) { window.log.warn(message); }
export function error(message) { window.log.error(message); }



// https://stackoverflow.com/questions/58280795/how-can-i-change-css-directlywithout-variable-in-blazor
export const setStyle                  = dom.setStyle;
export const setStyleWithElementId     = dom.setStyleWithElementId;
export const getStyle                  = dom.getStyle;
export const getStyleWithElementId     = dom.getStyleWithElementId;
export const setAttribute              = dom.setAttribute;
export const setAttributeWithElementId = dom.setAttributeWithElementId;
export const getAttribute              = dom.getAttribute;
export const getAttributeWithElementId = dom.getAttributeWithElementId;
export const getElementById            = dom.getElementById;
export const getTagName                = dom.getTagName;
export const setInnerTextById          = dom.setInnerTextById;
export const moveToBody                = dom.moveToBody;
export const move                      = dom.move;

export const addElementClass = dom.addElementClass;
export const removeElementClass = dom.removeElementClass;
export const replaceElementClass = dom.replaceElementClass;
export const getCssVariableValue = dom.getCssVariableValue;
export const existsElementWithId = dom.existsElementWithId;

export const getTheTooltip  = tooltip.getTheTooltip;
export const showTooltip    = tooltip.showTooltip;
export const showTooltipWithElementId = tooltip.showTooltipWithElementId;
export const hideTooltipWithElementId = tooltip.hideTooltipWithElementId;
export const showTheTooltip = tooltip.showTheTooltip;
export const hideTooltip    = tooltip.hideTooltip;
export const hideTheTooltip = tooltip.hideTheTooltip;
export const showPopup = tooltip.showPopup;
export const hidePopup = tooltip.hidePopup;
export const attachTooltipEventsWithMessage = tooltip.attachTooltipEventsWithMessage;
export const attachTheTooltipEvents =         tooltip.attachTheTooltipEvents;
export const attachTooltipEvents =            tooltip.attachTooltipEvents;
export const setDivDisplay = tooltip.setDivDisplay;

export const getImageContentOffset = media.getImageContentOffset;
export const startWebcam = media.startWebcam;
export const startVideo = media.startVideo;

export const openFullscreen = screen.openFullscreen;
export const closeFullscreen = screen.closeFullscreen;
export const goFullScreen = screen.goFullScreen;


export function enableDebugLog(enable) {
    window.disableDebugLog = !enable;
    setLogFunctions();
}

export function openWindow(url) {   // url: "http://dualsoft.com"
    window.open(url, "_blank")
}


setLogFunctions();
window.openWindow = openWindow


// Extend the CanvasMouseArgs.cs class (and this) as necessary
function jsMouseArgs(eventName, e) {
    return {
        EventName: eventName,
        ScreenX: e.screenX,
        ScreenY: e.screenY,
        ClientX: e.clientX,
        ClientY: e.clientY,
        MovementX: e.movementX,
        MovementY: e.movementY,
        OffsetX: e.offsetX,
        OffsetY: e.offsetY,
        AltKey: e.altKey,
        CtrlKey: e.ctrlKey,
        Button: e.button,
        Buttons: e.button,
        Bubbles: e.bubbles
    };
}

console.log('==[dualWebBlazorJsInterop.js loaded]');
