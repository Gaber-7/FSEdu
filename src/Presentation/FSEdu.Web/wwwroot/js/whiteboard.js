// FSEdu — Interactive whiteboard
// Drawing logic lives in JS. C# side uses SignalR for sync and only relays
// strokes via the methods below. Strokes are stored on the canvas; sync
// state is handled by C# (hub).

(function () {
    if (window.fseduWhiteboard) return;

    const state = {
        canvas: null,
        ctx: null,
        wrap: null,
        isTeacher: false,
        dotnetRef: null,         // .NET object ref to call back when stroke finishes
        currentStroke: null,     // accumulating points while mouse is down
        pen: { color: "#111111", width: 3, mode: "pen" }, // mode: pen | eraser
        bgColor: "#FFFFFF"
    };

    function clientPos(e) {
        const rect = state.canvas.getBoundingClientRect();
        const sx = state.canvas.width / rect.width;
        const sy = state.canvas.height / rect.height;
        const x = (e.clientX - rect.left) * sx;
        const y = (e.clientY - rect.top) * sy;
        return [Math.round(x), Math.round(y)];
    }

    function drawSegment(p1, p2, opts) {
        const ctx = state.ctx;
        ctx.save();
        ctx.lineCap = "round";
        ctx.lineJoin = "round";
        ctx.lineWidth = opts.width;
        if (opts.mode === "eraser") {
            ctx.strokeStyle = state.bgColor;
            ctx.lineWidth = Math.max(opts.width * 4, 16);
        } else {
            ctx.strokeStyle = opts.color;
        }
        ctx.beginPath();
        ctx.moveTo(p1[0], p1[1]);
        ctx.lineTo(p2[0], p2[1]);
        ctx.stroke();
        ctx.restore();
    }

    function drawStrokeFull(stroke) {
        if (!stroke || !stroke.points || stroke.points.length < 2) return;
        for (let i = 1; i < stroke.points.length; i++) {
            drawSegment(stroke.points[i - 1], stroke.points[i], stroke);
        }
    }

    function clearCanvas() {
        const ctx = state.ctx;
        ctx.save();
        ctx.fillStyle = state.bgColor;
        ctx.fillRect(0, 0, state.canvas.width, state.canvas.height);
        ctx.restore();
    }

    function onPointerDown(e) {
        if (!state.isTeacher) return;
        e.preventDefault();
        state.canvas.setPointerCapture(e.pointerId);
        const p = clientPos(e);
        state.currentStroke = {
            color: state.pen.color,
            width: state.pen.width,
            mode: state.pen.mode,
            points: [p]
        };
    }

    function onPointerMove(e) {
        if (!state.isTeacher || !state.currentStroke) return;
        e.preventDefault();
        const p = clientPos(e);
        const last = state.currentStroke.points[state.currentStroke.points.length - 1];
        // Skip points that are too close (reduces stroke size)
        if (Math.abs(p[0] - last[0]) + Math.abs(p[1] - last[1]) < 2) return;
        state.currentStroke.points.push(p);
        drawSegment(last, p, state.currentStroke);
    }

    function onPointerUp(e) {
        if (!state.isTeacher || !state.currentStroke) return;
        e.preventDefault();
        try { state.canvas.releasePointerCapture(e.pointerId); } catch { /* ignore */ }
        const stroke = state.currentStroke;
        state.currentStroke = null;
        if (stroke.points.length < 2) return; // ignore taps
        // Notify C# to broadcast via SignalR
        if (state.dotnetRef) {
            try { state.dotnetRef.invokeMethodAsync("OnStrokeFinished", stroke); } catch (err) { console.warn(err); }
        }
    }

    function attach(opts) {
        const canvas = document.getElementById(opts.canvasId);
        if (!canvas) throw new Error("Whiteboard canvas not found: " + opts.canvasId);

        state.canvas = canvas;
        state.ctx = canvas.getContext("2d");
        state.wrap = canvas.parentElement;
        state.isTeacher = !!opts.isTeacher;
        state.dotnetRef = opts.dotnetRef || null;

        // Resize canvas to its CSS-rendered size, in 16:9 logical resolution
        const rect = canvas.getBoundingClientRect();
        canvas.width = 1280;
        canvas.height = Math.round(1280 * (rect.height / Math.max(rect.width, 1)) || 720);
        if (canvas.height < 200) canvas.height = 720;

        clearCanvas();

        if (state.isTeacher) {
            canvas.style.cursor = "crosshair";
            canvas.addEventListener("pointerdown", onPointerDown);
            canvas.addEventListener("pointermove", onPointerMove);
            canvas.addEventListener("pointerup", onPointerUp);
            canvas.addEventListener("pointercancel", onPointerUp);
        } else {
            canvas.style.cursor = "default";
        }
    }

    function detach() {
        if (state.canvas) {
            state.canvas.removeEventListener("pointerdown", onPointerDown);
            state.canvas.removeEventListener("pointermove", onPointerMove);
            state.canvas.removeEventListener("pointerup", onPointerUp);
            state.canvas.removeEventListener("pointercancel", onPointerUp);
        }
        state.canvas = null; state.ctx = null; state.wrap = null;
        state.isTeacher = false; state.dotnetRef = null;
        state.currentStroke = null;
    }

    function setTool(tool) {
        if (tool.color) state.pen.color = tool.color;
        if (tool.width) state.pen.width = tool.width;
        if (tool.mode) state.pen.mode = tool.mode;
    }

    function applyRemoteStroke(stroke) {
        if (!state.ctx) return;
        drawStrokeFull(stroke);
    }

    function applySnapshot(strokes) {
        if (!state.ctx) return;
        clearCanvas();
        for (const s of strokes || []) drawStrokeFull(s);
    }

    function clearAll(local) {
        if (!state.ctx) return;
        clearCanvas();
    }

    window.fseduWhiteboard = {
        attach, detach, setTool,
        applyRemoteStroke, applySnapshot, clearAll
    };
})();
