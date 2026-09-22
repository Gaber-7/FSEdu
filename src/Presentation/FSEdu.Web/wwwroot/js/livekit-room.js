// FSEdu — LiveKit room helper (loaded on-demand from LiveRoom.razor)
// Loads livekit-client UMD from CDN, exposes connect/disconnect + media controls.

(function () {
    if (window.fseduLiveKit) return;

    const SDK_URL = "https://cdn.jsdelivr.net/npm/livekit-client@2.5.7/dist/livekit-client.umd.min.js";

    let sdkPromise = null;
    function loadSdk() {
        if (window.LivekitClient) return Promise.resolve(window.LivekitClient);
        if (sdkPromise) return sdkPromise;
        sdkPromise = new Promise((resolve, reject) => {
            const s = document.createElement("script");
            s.src = SDK_URL;
            s.async = true;
            s.onload = () => resolve(window.LivekitClient);
            s.onerror = () => reject(new Error("Failed to load LiveKit SDK"));
            document.head.appendChild(s);
        });
        return sdkPromise;
    }

    const state = {
        room: null,
        videoContainer: null,
        elementsByTrackSid: new Map(),
        // Recording state (browser MediaRecorder → multipart upload to API)
        sessionId: null,
        appAuthToken: null,
        apiBase: null,
        recorder: null,
        recordedChunks: [],
        whiteboardCanvasId: null  // currently-active whiteboard surface, if any
    };

    function attachTrack(track, participant) {
        if (!state.videoContainer) return;
        const wrapper = document.createElement("div");
        wrapper.className = "lk-tile";
        wrapper.dataset.identity = participant.identity;
        const label = document.createElement("div");
        label.className = "lk-label";
        label.textContent = participant.name || participant.identity || "";
        const el = track.attach();
        el.classList.add(track.kind === "video" ? "lk-video" : "lk-audio");
        if (track.kind === "video") el.setAttribute("playsinline", "");
        wrapper.appendChild(el);
        wrapper.appendChild(label);
        state.videoContainer.appendChild(wrapper);
        state.elementsByTrackSid.set(track.sid, { el, wrapper });
    }

    function detachTrack(track) {
        const entry = state.elementsByTrackSid.get(track.sid);
        if (!entry) return;
        try {
            // track.detach(element) returns a single HTMLMediaElement;
            // track.detach() with no args returns an array. Handle both shapes.
            const result = track.detach(entry.el);
            const els = Array.isArray(result) ? result : (result ? [result] : []);
            els.forEach(e => { try { e.remove(); } catch { /* ignore */ } });
        } catch (e) {
            console.warn("[LiveKit] detach failed:", e);
        }
        try { entry.wrapper.remove(); } catch { /* ignore */ }
        state.elementsByTrackSid.delete(track.sid);
    }

    async function connect(opts) {
        await loadSdk();
        const Lk = window.LivekitClient;
        if (state.room) await disconnect();

        state.videoContainer = document.getElementById(opts.containerId);
        if (!state.videoContainer) throw new Error("Video container not found: " + opts.containerId);

        const room = new Lk.Room({
            adaptiveStream: true,
            dynacast: true,
            videoCaptureDefaults: { resolution: Lk.VideoPresets.h720.resolution }
        });

        room.on(Lk.RoomEvent.TrackSubscribed, (track, _pub, participant) => {
            attachTrack(track, participant);
        });
        room.on(Lk.RoomEvent.TrackUnsubscribed, (track) => detachTrack(track));
        room.on(Lk.RoomEvent.LocalTrackPublished, (pub) => {
            if (pub.track) attachTrack(pub.track, room.localParticipant);
        });
        room.on(Lk.RoomEvent.LocalTrackUnpublished, (pub) => {
            if (pub.track) detachTrack(pub.track);
        });
        room.on(Lk.RoomEvent.Disconnected, () => {
            state.elementsByTrackSid.forEach(({ wrapper }) => wrapper.remove());
            state.elementsByTrackSid.clear();
        });

        await room.connect(opts.url, opts.token);

        if (opts.publish) {
            try { await room.localParticipant.enableCameraAndMicrophone(); }
            catch (e) { console.warn("[LiveKit] camera/mic denied:", e); }
        }

        state.room = room;
        state.sessionId = opts.sessionId || null;
        state.appAuthToken = opts.appAuthToken || null;
        state.apiBase = opts.apiBase || "/";
        return { identity: room.localParticipant.identity };
    }

    async function setMic(enabled) {
        if (!state.room) return;
        await state.room.localParticipant.setMicrophoneEnabled(enabled);
    }

    async function setCam(enabled) {
        if (!state.room) return;
        await state.room.localParticipant.setCameraEnabled(enabled);
    }

    async function setScreen(enabled) {
        if (!state.room) return;
        await state.room.localParticipant.setScreenShareEnabled(enabled);
    }

    async function disconnect() {
        try { if (state.recorder && state.recorder.state !== "inactive") state.recorder.stop(); } catch { /* ignore */ }
        if (state.composer) { try { state.composer.stop(); } catch { /* ignore */ } state.composer = null; }
        state.recorder = null;
        state.recordedChunks = [];
        state.recordingListeners = null;
        if (!state.room) return;
        try { await state.room.disconnect(); } catch { /* ignore */ }
        state.room = null;
        state.elementsByTrackSid.forEach(({ wrapper }) => wrapper.remove());
        state.elementsByTrackSid.clear();
    }

    // ─── Canvas composer ────────────────────────────────────
    // Composites the local participant's video tracks (camera + screen) onto a canvas
    // and mixes audio tracks via AudioContext, producing a single MediaStream that
    // remains valid even when individual tracks are added/removed during recording
    // (which raw MediaRecorder doesn't support).

    class RecordingComposer {
        constructor(width, height, fps) {
            this.canvas = document.createElement("canvas");
            this.canvas.width = width || 1280;
            this.canvas.height = height || 720;
            this.ctx = this.canvas.getContext("2d");
            this.fps = fps || 30;
            this.videos = new Map();   // sid -> { videoEl, source: "camera"|"screen" }
            this.audioCtx = null;
            this.audioDest = null;
            this.audioSources = new Map(); // sid -> MediaStreamAudioSourceNode
            this.rafId = null;
            this.running = false;
            this.whiteboardCanvas = null; // optional secondary surface
        }

        setWhiteboard(canvasId) {
            this.whiteboardCanvas = canvasId ? document.getElementById(canvasId) : null;
        }

        addVideoTrack(lkTrack) {
            if (!lkTrack || !lkTrack.mediaStreamTrack) return;
            const v = document.createElement("video");
            v.muted = true;
            v.playsInline = true;
            v.autoplay = true;
            v.srcObject = new MediaStream([lkTrack.mediaStreamTrack]);
            const playPromise = v.play();
            if (playPromise) playPromise.catch(() => { /* ignore */ });
            const source = (lkTrack.source === "screen_share" || lkTrack.source === "screen-share") ? "screen" : "camera";
            this.videos.set(lkTrack.sid, { videoEl: v, source });
        }

        removeVideoTrack(sid) {
            const entry = this.videos.get(sid);
            if (entry) {
                try { entry.videoEl.srcObject = null; } catch { /* ignore */ }
                this.videos.delete(sid);
            }
        }

        addAudioTrack(lkTrack) {
            if (!lkTrack || !lkTrack.mediaStreamTrack) return;
            if (!this.audioCtx) {
                this.audioCtx = new (window.AudioContext || window.webkitAudioContext)();
                this.audioDest = this.audioCtx.createMediaStreamDestination();
            }
            try {
                const node = this.audioCtx.createMediaStreamSource(
                    new MediaStream([lkTrack.mediaStreamTrack]));
                node.connect(this.audioDest);
                this.audioSources.set(lkTrack.sid, node);
            } catch (e) { console.warn("[Composer] audio source failed:", e); }
        }

        removeAudioTrack(sid) {
            const node = this.audioSources.get(sid);
            if (node) {
                try { node.disconnect(); } catch { /* ignore */ }
                this.audioSources.delete(sid);
            }
        }

        _drawFit(videoEl, x, y, w, h) {
            const vw = videoEl.videoWidth, vh = videoEl.videoHeight;
            if (!vw || !vh) return;
            const scale = Math.min(w / vw, h / vh);
            const dw = vw * scale, dh = vh * scale;
            const dx = x + (w - dw) / 2, dy = y + (h - dh) / 2;
            try { this.ctx.drawImage(videoEl, dx, dy, dw, dh); } catch { /* ignore */ }
        }

        _frame() {
            if (!this.running) return;
            const W = this.canvas.width, H = this.canvas.height;
            this.ctx.fillStyle = "#FFFFFF";
            this.ctx.fillRect(0, 0, W, H);

            const all = [...this.videos.values()];
            const screen = all.find(v => v.source === "screen");
            const camera = all.find(v => v.source === "camera");

            // Priority: whiteboard > screen > camera. Camera always shown as PiP if available.
            if (this.whiteboardCanvas && this.whiteboardCanvas.width > 0) {
                try { this.ctx.drawImage(this.whiteboardCanvas, 0, 0, W, H); } catch { /* ignore */ }
                if (camera) {
                    const cw = Math.floor(W * 0.20), ch = Math.floor(cw * 9 / 16);
                    this._drawFit(camera.videoEl, W - cw - 24, H - ch - 24, cw, ch);
                }
            } else if (screen) {
                this.ctx.fillStyle = "#000";
                this.ctx.fillRect(0, 0, W, H);
                this._drawFit(screen.videoEl, 0, 0, W, H);
                if (camera) {
                    const cw = Math.floor(W * 0.20), ch = Math.floor(cw * 9 / 16);
                    this._drawFit(camera.videoEl, W - cw - 24, H - ch - 24, cw, ch);
                }
            } else if (camera) {
                this.ctx.fillStyle = "#000";
                this.ctx.fillRect(0, 0, W, H);
                this._drawFit(camera.videoEl, 0, 0, W, H);
            } else {
                this.ctx.fillStyle = "#222";
                this.ctx.font = "32px sans-serif";
                this.ctx.textAlign = "center";
                this.ctx.fillText("…", W / 2, H / 2);
            }

            this.rafId = requestAnimationFrame(() => this._frame());
        }

        start() {
            this.running = true;
            this._frame();
            const stream = this.canvas.captureStream(this.fps);
            // Append mixed audio if any
            if (this.audioDest) {
                this.audioDest.stream.getAudioTracks().forEach(t => stream.addTrack(t));
            }
            return stream;
        }

        stop() {
            this.running = false;
            if (this.rafId) cancelAnimationFrame(this.rafId);
            this.rafId = null;
            this.videos.forEach(v => { try { v.videoEl.srcObject = null; } catch { /* ignore */ } });
            this.videos.clear();
            this.audioSources.forEach(n => { try { n.disconnect(); } catch { /* ignore */ } });
            this.audioSources.clear();
            if (this.audioCtx) {
                try { this.audioCtx.close(); } catch { /* ignore */ }
                this.audioCtx = null;
                this.audioDest = null;
            }
        }
    }

    // ─── Client-side recording ─────────────────────────────
    // Records the local participant's published tracks (camera + mic + screen if active)
    // to a webm Blob via MediaRecorder, then uploads to the API on stop.

    function pickMimeType() {
        const candidates = [
            "video/webm;codecs=vp9,opus",
            "video/webm;codecs=vp8,opus",
            "video/webm"
        ];
        for (const t of candidates) {
            if (typeof MediaRecorder !== "undefined" && MediaRecorder.isTypeSupported(t)) return t;
        }
        return null;
    }

    function _addPubToComposer(composer, pub) {
        if (!pub || !pub.track) return;
        if (pub.kind === "video" || (pub.track && pub.track.kind === "video"))
            composer.addVideoTrack(pub.track);
        else if (pub.kind === "audio" || (pub.track && pub.track.kind === "audio"))
            composer.addAudioTrack(pub.track);
    }

    async function startRecording() {
        if (!state.room) throw new Error("غير متصل بغرفة البث");
        if (state.recorder) throw new Error("التسجيل بالفعل قيد التشغيل");

        const Lk = window.LivekitClient;
        const lp = state.room.localParticipant;

        const composer = new RecordingComposer(1280, 720, 30);
        composer.setWhiteboard(state.whiteboardCanvasId);
        state.composer = composer;

        // Snapshot currently-published tracks
        let trackCount = 0;
        lp.trackPublications.forEach(pub => {
            _addPubToComposer(composer, pub);
            trackCount++;
        });
        if (trackCount === 0) {
            composer.stop();
            state.composer = null;
            throw new Error("لا توجد كاميرا/ميكروفون منشورة — فعّل المايك والكاميرا أولاً");
        }

        // Watch for tracks added/removed during recording (e.g. screen share toggled)
        const onPub = (pub) => _addPubToComposer(composer, pub);
        const onUnpub = (pub) => {
            const sid = pub.trackSid || (pub.track && pub.track.sid);
            if (sid) {
                composer.removeVideoTrack(sid);
                composer.removeAudioTrack(sid);
            }
        };
        lp.on(Lk.ParticipantEvent.LocalTrackPublished, onPub);
        lp.on(Lk.ParticipantEvent.LocalTrackUnpublished, onUnpub);
        state.recordingListeners = { onPub, onUnpub };

        const recordingStream = composer.start();

        const mimeType = pickMimeType();
        if (!mimeType) {
            composer.stop();
            state.composer = null;
            throw new Error("المتصفح لا يدعم MediaRecorder");
        }

        state.recordedChunks = [];
        state.recorder = new MediaRecorder(recordingStream, {
            mimeType, videoBitsPerSecond: 2_000_000
        });
        state.recorder.ondataavailable = e => {
            if (e.data && e.data.size > 0) state.recordedChunks.push(e.data);
        };
        state.recorder.start(2000);
        return { mimeType, trackCount };
    }

    async function stopRecording() {
        if (!state.recorder) throw new Error("لا يوجد تسجيل قيد التشغيل");
        if (!state.sessionId || !state.appAuthToken) throw new Error("معلومات الجلسة غير متاحة");

        const recorder = state.recorder;
        const chunks = state.recordedChunks;

        const stopped = new Promise(resolve => { recorder.onstop = resolve; });
        recorder.stop();
        await stopped;

        // Clean up composer + listeners
        if (state.recordingListeners && state.room) {
            const Lk = window.LivekitClient;
            const lp = state.room.localParticipant;
            try { lp.off(Lk.ParticipantEvent.LocalTrackPublished, state.recordingListeners.onPub); } catch { /* ignore */ }
            try { lp.off(Lk.ParticipantEvent.LocalTrackUnpublished, state.recordingListeners.onUnpub); } catch { /* ignore */ }
            state.recordingListeners = null;
        }
        if (state.composer) {
            try { state.composer.stop(); } catch { /* ignore */ }
            state.composer = null;
        }
        state.recorder = null;
        state.recordedChunks = [];

        const blob = new Blob(chunks, { type: recorder.mimeType || "video/webm" });
        if (blob.size === 0) throw new Error("التسجيل فارغ — جرّب مرة أخرى");

        const fd = new FormData();
        const ts = new Date().toISOString().replace(/[:.]/g, "-");
        fd.append("file", blob, `lecture-${ts}.webm`);

        const apiBase = (state.apiBase || "/").replace(/\/+$/, "");
        const url = apiBase + "/api/v1/teacher/live/" + encodeURIComponent(state.sessionId) + "/record/upload";

        const resp = await fetch(url, {
            method: "POST",
            headers: { "Authorization": "Bearer " + state.appAuthToken },
            body: fd
        });
        if (!resp.ok) {
            const body = await resp.text().catch(() => "");
            throw new Error("فشل رفع التسجيل (" + resp.status + "): " + body);
        }
        return await resp.json();
    }

    function setWhiteboardCanvas(canvasId) {
        state.whiteboardCanvasId = canvasId || null;
        if (state.composer) state.composer.setWhiteboard(state.whiteboardCanvasId);
    }

    window.fseduLiveKit = {
        connect, disconnect,
        setMic, setCam, setScreen,
        startRecording, stopRecording,
        setWhiteboardCanvas
    };
})();
