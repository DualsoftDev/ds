/* Javascript module for CompKonvaJs.razor */

// The one and only way of getting global scope in all environments
// https://stackoverflow.com/q/3277182/1008999
var _global = typeof window === 'object' && window.window === window
    ? window : typeof self === 'object' && self.self === self
        ? self : typeof global === 'object' && global.global === global
            ? global
            : this


// Konva stage, layer, shapes, tooltip 등을 관리하는 객체
class KonvaObject {
    constructor(stage, layer, konvaShapeDict) {
        this.stage = stage;
        this.layer = layer;
        this.konvaShapeDict = konvaShapeDict;
        this.aspectRatioH = 1;
        this.aspectRatioV = 1;
    }
    resizedX(x) { return x / this.aspectRatioH; }
    resizedY(y) { return y / this.aspectRatioV; }
};

window.konvaObjects = {};

var isDebugKonva = false;
var setKonvaDebug = function (enable) { isDebugKonva = enable; }
var isKonvaDebug = function () { return isDebugKonva; }

var refreshKonvaCanvas = function (divId) {
    const k = window.konvaObjects[divId];
    if (k == null)
        return;
    const w = k.stage.width();
    k.stage.width(w+1);
    k.layer.draw();
    k.stage.width(w);
    k.layer.draw();
}

var resizeKonvaCanvas = function (divId, rect, aspectRatioH, aspectRatioV) {
    let { x, y, w, h } = rect;
    console.log(`JS: Resizing konva canvas (x, y)=(${x.toFixed(2)}, ${y.toFixed(2)}), ` +
        `w x h: ${w.toFixed(2)} x ${h.toFixed(2)}, arh=${aspectRatioH.toFixed(2)}, arv=${aspectRatioV.toFixed(2)}`);
    const k = window.konvaObjects[divId];
    if (k == null)
        return;

    k.stage.width(w);
    k.stage.height(h);

    // 모든 자식 요소에 대해 위치와 크기 재설정
    k.layer.children.forEach(function (child) {
        // 위치 조정:
        let { ox, oy } = {        // origin x, y,
            ox: child.origX,
            oy: child.origY
        };
        let { nx, ny } = {        // new x, y
            nx: ox * aspectRatioH,  // + x,
            ny: oy * aspectRatioV,  // + y,
        };
        if (isDebugKonva)
            console.log(`  child: ${child.className}, x, y: (${ox.toFixed(2)}, ${oy.toFixed(2)}) => (${nx.toFixed(2)}, ${ny.toFixed(2)})`);

        child.x(nx);
        child.y(ny);

        // 요소 유형에 따른 크기 조정
        switch (child.className) {
            case 'Image':
            case 'Rect':
                let ow = child.origWidth;
                let oh = child.origHeight;

                // 새로운 width와 height 계산
                let nw = ow * aspectRatioH;
                let nh = oh * aspectRatioV;

                if (isDebugKonva)
                    console.log(`    w, h: : ${ow.toFixed(2)} x ${oh.toFixed(2)} => (${nw.toFixed(2)}, ${nh.toFixed(2)})`);

                child.width(nw);
                child.height(nh);
                break;
            case 'RegularPolygon':
            case 'Wedge':
            case 'Circle':
                child.radius(child.origRadius * Math.min(aspectRatioH, aspectRatioV));
                break;
            case 'Star':
                // child.outerRadius(child.origOuterRadius * Math.min(aspectRatioH, aspectRatioV));
                // child.innerRadius(child.origInnerRadius * Math.min(aspectRatioH, aspectRatioV));
                break;
            case 'Text':
                child.fontSize(child.fontSize() * Math.min(aspectRatioH, aspectRatioV));
                break;
            default:
                console.error(`1Unknown shape type: ${child.className}`);
                // console.log(`1Unknown shape type: ${JSON.stringify(child, null, 2)}`);

                break;
        }
    });

    k.aspectRatioH = aspectRatioH;
    k.aspectRatioV = aspectRatioV;

    if (k.layer.renderAll == null)
        if (k.layer.draw == null)
            console.error('k.layer.renderAll is null');
        else
            k.layer.draw();
    else
        k.layer.renderAll();
};

// 깜박임 예제 용..
var replaceKonvaImage = function (divId, id, imageUrl) {
    if (konvaObjects == null || konvaObjects[divId] == null) {
        console.error(`Konva not initialized for ${divId}`);
        return;
    }
    console.log(`Updating konva shape with id=${id}`);

    const k = window.konvaObjects[divId];
    const konvaShape = k.konvaShapeDict[id];

    const imageObj = new Image();
    imageObj.onload = function () {
        konvaShape.image(imageObj);
    }
    imageObj.src = imageUrl;
};

var changeKonvaShapeAttribute = function (divId, shapeId, attributeName, attributeValue) {
    // konvaObjects가 초기화되지 않았거나, 지정된 divId에 해당하는 객체가 없는 경우 에러 로그 출력
    if (window.konvaObjects == null || window.konvaObjects[divId] == null) {
        console.error(`Konva not initialized for ${divId}`);
        return;
    }

    // divId에 해당하는 Konva 객체 접근
    const k = window.konvaObjects[divId];
    // shapeId를 사용하여 konvaShapeDict에서 해당하는 Konva Shape 객체 찾기
    const konvaShape = k.konvaShapeDict[shapeId];

    if (konvaShape) {
        // attributeName에 해당하는 속성을 attributeValue로 변경
        konvaShape.setAttr(attributeName, attributeValue);
        // 변경 사항을 적용하기 위해 레이어를 다시 그림
        k.layer.draw();
        console.log(`Attribute ${attributeName} of konva shape with id=${shapeId} updated to ${attributeValue}`);
    } else {
        console.error(`Shape with id=${shapeId} not found in ${divId}`);
    }
};

var jsonShapeToKonvaShape = function (shapeJson) {
    const s = shapeJson;
    let ks;
    switch (s.type) {
        case 'KonvaImage':
            ks = new Konva.Image({
                x: s.x,
                y: s.y,
                width: s.width,
                height: s.height,
            });
            ks.origWidth = s.width;
            ks.origHeight = s.height;
            let imageObj = new Image();
            imageObj.onload = function () {
                //console.log(`KonvaImage info: ${JSON.stringify(s, null, 2)}`);
                //console.log(`Konva.Image info: ${JSON.stringify(ks, null, 2)}`);
                ks.image(imageObj);
                //ks.width(s.width);
                //ks.height(s.height);
            };
            imageObj.src = s.imageUrl;
            break;

        case 'KonvaFigureGroup':
            ks = new Konva.Rect(s);
            ks.origWidth = s.width;
            ks.origHeight = s.height;
            if (isDebugKonva)
                console.log(`Creating Konva.Group: ${ks.className} = ${JSON.stringify(s, null, 2)}`);
            break;
        case 'KonvaFigureRect':
            ks = new Konva.Rect(s);
            ks.origWidth = s.width;
            ks.origHeight = s.height;
            break;
        case 'KonvaFigureCircle':
            ks = new Konva.Circle(s);
            ks.origRadius = s.radius;
            break;
        case 'KonvaFigureRegularPolygon':
            ks = new Konva.RegularPolygon(s);
            ks.origRadius = s.radius;
            break;
        case 'KonvaFigureWedge':
            ks = new Konva.Wedge(s);
            ks.origRadius = s.radius;
            break;
        case 'KonvaFigureStar':
            ks = new Konva.Star(s);
            break;
        case 'KonvaFigureText':
            ks = new Konva.Text(s);
            break;


        default:
            console.error(`Unknown shape type: ${s.type}`);
            break;
    }
    ks.id = s.id;     // 동적으로 추가한 속성
    ks.origX = s.x;
    ks.origY = s.y;

    if (isDebugKonva)
        console.log(`window.jsonShapeToKonvaShape:: Returning ${ks.className} = ${s.type}, for id=${s.id}`);

    return ks;
};

// jsonData: stringified json of array of Konva.Shape
var initKonva = function (dotNetHelper, jsonData, divId, aspectRatioH, aspectRatioV) {
    console.log(`-------------- initKonva for ${divId} : aspectRatioH=${aspectRatioH.toFixed(2)}, aspectRatioV=${aspectRatioV.toFixed(2)}`)
    if (window.konvaObjects[divId]) {
        console.error(`Konva already initialized for ${divId}`);
        return;
    }

    let stage = new Konva.Stage({
        container: divId,
        width: window.innerWidth,
        height: window.innerHeight,
    });

    let konvaShapeDict = {};
    let layer = new Konva.Layer();
    stage.add(layer);

    const shapes = JSON.parse(jsonData);
    // console.log(`Json data: ${jsonData}`);
    console.log(`Total ${shapes.length} shapes`);

    shapes.forEach(s => {
        // console.log(`Adding shape: ${s.type} : ${JSON.stringify(s, null, 2)}`);
        if (s.id == -1)
            console.error('Shape id must not be -1.  -1 는 tootip 용으로 예약 됨.');

        let shape = jsonShapeToKonvaShape(s);
        konvaShapeDict[s.id] = shape;
        layer.add(shape);
        shape.on('click', function () {
            let { x, y } = stage.getPointerPosition();
            console.log(`(${x.toFixed(2)}, ${y.toFixed(2)})click shape: ${s.id}`);
            dotNetHelper.invokeMethodAsync('ShapeClicked', s.id, x, y);
        });
        shape.on('mouseover', function () {
            let { x, y } = stage.getPointerPosition();
            console.log(`mouseover on shape id=${s.id}, x, y: ${x.toFixed(2)}, ${y.toFixed(2)}`)
            dotNetHelper.invokeMethodAsync('ShapeMouseOver', s.id, x, y);

        });
        shape.on('mouseout', function () {
            let { x, y } = stage.getPointerPosition();
            console.log(`mouseout on shape id=${s.id}, x, y: ${x.toFixed(2)}, ${y.toFixed(2)}`)
            dotNetHelper.invokeMethodAsync('ShapeMouseOut', s.id, x, y);
        });
    });

    const k = new KonvaObject(stage, layer, konvaShapeDict);
    k.aspectRatioH = aspectRatioH;
    k.aspectRatioV = aspectRatioV;

    window.konvaObjects[divId] = k;

    console.log(`-------------- Konva initialized for ${divId}`);

    // 이 시점에서 resize 호출 시.. Unhandled exception rendering component: layer.renderAll is not a function
    //let rect = { x: 0, y: 0, w: window.innerWidth, h: window.innerHeight };
    //resizeKonvaCanvas(divId, rect, aspectRatioH, aspectRatioV);
};


/* JavaScript에서 _global은 전역 객체를 나타냅니다. 브라우저 환경에서는 window 객체가 전역 객체이고, Node.js 환경에서는 global 객체가 전역 객체입니다 */

_global.initKonva = initKonva;
_global.jsonShapeToKonvaShape = jsonShapeToKonvaShape;
_global.changeKonvaShapeAttribute = changeKonvaShapeAttribute;
_global.replaceKonvaImage = replaceKonvaImage;
_global.resizeKonvaCanvas = resizeKonvaCanvas;
_global.refreshKonvaCanvas = refreshKonvaCanvas;
_global.setKonvaDebug = setKonvaDebug;
_global.isKonvaDebug = isKonvaDebug;

if (typeof module !== 'undefined') {
    module.exports = {
        initKonva, jsonShapeToKonvaShape, changeKonvaShapeAttribute,
        replaceKonvaImage, resizeKonvaCanvas, refreshKonvaCanvas,
        setKonvaDebug, isKonvaDebug
    };
}

