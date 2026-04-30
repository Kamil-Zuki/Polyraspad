(function initContentScript() {
  if (window.__inoriginalCaptureInstalled) {
    return;
  }

  window.__inoriginalCaptureInstalled = true;

  const state = {
    entries: [],
    isRecording: false,
    observer: null,
    subtitleElement: null,
    startedAt: 0,
    pollTimer: null,
    clipPollTimer: null,
    clipFallbackTimer: null,
    clipMaxTimer: null,
    rangeTimer: null,
    lastText: "",
    clipMode: null,
    timeline: null,
    timelinePromise: null
  };

  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message?.type === "start-subtitle-capture") {
      startCapture(message.startedAt);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === "stop-subtitle-capture") {
      stopCapture(message.stoppedAt);
      void getSubtitleContext()
        .then((context) => sendResponse({
          entries: state.entries,
          ...context
        }))
        .catch(() => sendResponse({
          entries: state.entries,
          ...getDomSubtitleContext()
        }));
      return true;
    }

    if (message?.type === "get-current-subtitle") {
      sendResponse({ currentSubtitle: getCurrentSubtitle() });
      return;
    }

    if (message?.type === "get-current-subtitle-context") {
      void getSubtitleContext()
        .then((context) => sendResponse(context))
        .catch(() => sendResponse(getDomSubtitleContext()));
      return true;
    }

    if (message?.type === "start-subtitle-clip") {
      startSubtitleClip(message.startedAt, message.targetSubtitle, message.rewindMs || 0, message.maxClipMs || 8000, message.cue || null, message.captureMode || "auto-vtt");
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === "select-subtitle-cue") {
      void selectSubtitleCue(message.index)
        .then((context) => sendResponse({ ok: true, ...context }))
        .catch((error) => sendResponse({ ok: false, error: error.message }));
      return true;
    }

    if (message?.type === "prepare-audio-range") {
      void prepareAudioRange(message.startSeconds, message.endSeconds)
        .then((result) => sendResponse({ ok: true, ...result }))
        .catch((error) => sendResponse({ ok: false, error: error.message }));
      return true;
    }

    if (message?.type === "play-prepared-audio-range") {
      playPreparedAudioRange(message.endSeconds);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === "pause-video-playback") {
      pauseVideoPlayback();
      sendResponse({ ok: true });
    }
  });

  function startCapture(startedAt) {
    stopCapture(startedAt);

    state.isRecording = true;
    state.entries = [];
    state.startedAt = startedAt;
    state.lastText = "";
    state.subtitleElement = findSubtitleSpan();

    observeSubtitle();
    state.pollTimer = window.setInterval(refreshSubtitleTarget, 1000);
    captureCurrentText();
  }

  function stopCapture(stoppedAt) {
    state.isRecording = false;
    state.clipMode = null;

    if (state.observer) {
      state.observer.disconnect();
      state.observer = null;
    }

    if (state.pollTimer) {
      clearInterval(state.pollTimer);
      state.pollTimer = null;
    }

    if (state.clipPollTimer) {
      clearInterval(state.clipPollTimer);
      state.clipPollTimer = null;
    }

    if (state.clipFallbackTimer) {
      clearTimeout(state.clipFallbackTimer);
      state.clipFallbackTimer = null;
    }

    if (state.clipMaxTimer) {
      clearTimeout(state.clipMaxTimer);
      state.clipMaxTimer = null;
    }

    if (state.rangeTimer) {
      clearTimeout(state.rangeTimer);
      state.rangeTimer = null;
    }

    if (state.entries.length > 0) {
      state.entries[state.entries.length - 1].endMs = stoppedAt - state.startedAt;
    }
  }

  function refreshSubtitleTarget() {
    const nextElement = findSubtitleSpan();
    if (nextElement === state.subtitleElement) {
      captureCurrentText();
      return;
    }

    state.subtitleElement = nextElement;
    observeSubtitle();
    captureCurrentText();
  }

  function observeSubtitle() {
    if (state.observer) {
      state.observer.disconnect();
      state.observer = null;
    }

    if (!state.subtitleElement) {
      return;
    }

    state.observer = new MutationObserver(() => captureCurrentText());
    state.observer.observe(state.subtitleElement, {
      childList: true,
      characterData: true,
      subtree: true
    });
  }

  function captureCurrentText() {
    if (!state.isRecording || !state.subtitleElement) {
      return;
    }

    const text = normalizeText(state.subtitleElement.textContent || "");
    if (!text || text === state.lastText) {
      return;
    }

    const previousText = state.lastText;
    const now = Date.now();
    const atMs = now - state.startedAt;

    if (state.entries.length > 0) {
      state.entries[state.entries.length - 1].endMs = atMs;
    }

    state.entries.push({
      atMs,
      endMs: atMs + 1500,
      sessionStartedAt: state.startedAt,
      text
    });
    state.lastText = text;

    maybeRequestClipRecording(text);

    maybeCompleteClip(text, previousText);
  }

  function findSubtitleSpan() {
    return document.querySelector("#pjs_playerjs_subtitle > span");
  }

  function normalizeText(value) {
    return value.replace(/\s+/g, " ").trim();
  }

  function getCurrentSubtitle() {
    const element = findSubtitleSpan();
    return normalizeText(element?.textContent || "") || state.lastText || "";
  }

  async function getSubtitleContext() {
    const timeline = await getSubtitleTimeline().catch(() => null);
    const cueContext = timeline ? getCueContext(timeline) : null;
    return cueContext || getDomSubtitleContext();
  }

  function getDomSubtitleContext() {
    const currentSubtitle = getCurrentSubtitle();
    const currentIndex = state.entries.findIndex((entry) => entry.text === currentSubtitle);
    const fallbackIndex = state.entries.length - 1;
    const resolvedIndex = currentIndex >= 0 ? currentIndex : fallbackIndex;

    return {
      currentSubtitle,
      previousSubtitle: resolvedIndex > 0 ? state.entries[resolvedIndex - 1]?.text || "" : "",
      nextSubtitle: resolvedIndex >= 0 && resolvedIndex < state.entries.length - 1
        ? state.entries[resolvedIndex + 1]?.text || ""
        : "",
      videoTime: getVideoTime()
    };
  }

  function startSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs, cue, captureMode) {
    if (captureMode === "dom-fallback") {
      startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs);
      return;
    }

    if (cue && Number.isFinite(cue.start) && Number.isFinite(cue.end) && cue.end > cue.start) {
      void startCueAwareSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs, cue)
        .catch(() => {
          if (captureMode === "manual-range") {
            chrome.runtime.sendMessage({
              type: "recording-error",
              error: "Manual range capture could not seek to the selected VTT cue."
            });
            return;
          }

          startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs);
        });
      return;
    }

    if (captureMode === "manual-range") {
      chrome.runtime.sendMessage({
        type: "recording-error",
        error: "Manual range mode needs a selected VTT cue."
      });
      return;
    }

    startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs);
  }

  async function startCueAwareSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs, cue) {
    startCapture(startedAt);
    state.clipMode = {
      active: true,
      cue,
      cueAware: true,
      plannedVideoEndTime: 0,
      plannedVideoStartTime: 0,
      targetSubtitle,
      recordingRequested: true,
      recordingStarted: false,
      forcedStart: true
    };

    const mediaElement = getVideoElement();
    if (!mediaElement) {
      throw new Error("No video element found for cue-aware capture.");
    }

    const rewindSeconds = Math.max(0, Number(rewindMs) || 0) / 1000;
    const startSeconds = Math.max(0, cue.start - rewindSeconds);
    const recorderWarmupMs = 300;
    const minCueClipMs = 2500;
    const tailPaddingSeconds = 0.85;
    const naturalEndSeconds = Math.max(cue.end + tailPaddingSeconds, startSeconds + minCueClipMs / 1000);
    const maxEndSeconds = startSeconds + Math.max(0.5, Number(maxClipMs) || 8000) / 1000;
    const endSeconds = Math.min(naturalEndSeconds, maxEndSeconds);
    const plannedDurationMs = Math.max(minCueClipMs, Math.round((endSeconds - startSeconds) * 1000));
    state.clipMode.plannedVideoStartTime = startSeconds;
    state.clipMode.plannedVideoEndTime = endSeconds;
    mediaElement.pause();
    await seekVideo(mediaElement, startSeconds);

    chrome.runtime.sendMessage({
      type: "subtitle-clip-start-recording",
      plannedDurationMs,
      videoEndTime: endSeconds,
      videoStartTime: startSeconds,
      videoTime: startSeconds
    }, (response) => {
      if (chrome.runtime.lastError || !response?.ok || !state.clipMode?.active) {
        startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs);
        return;
      }

      state.clipMode.recordingStarted = true;
      state.startedAt = Date.now();
      state.entries = [{
        atMs: 0,
        endMs: plannedDurationMs,
        sessionStartedAt: state.startedAt,
        text: targetSubtitle
      }];
      state.lastText = targetSubtitle;

      state.clipFallbackTimer = window.setTimeout(() => {
        if (!state.clipMode?.active) {
          return;
        }

        void mediaElement.play().catch(() => {});
        state.clipMaxTimer = window.setTimeout(() => {
          completeClipByCue(cue, "cue-end");
        }, plannedDurationMs);
      }, recorderWarmupMs);
    });
  }

  function startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs) {
    startCapture(startedAt);
    const rewound = rewindVideoPlayback(rewindMs);
    state.clipMode = {
      active: true,
      targetSubtitle,
      recordingRequested: false,
      recordingStarted: false,
      forcedStart: false
    };

    maybeRequestClipRecording(getCurrentSubtitle());

    state.clipPollTimer = window.setInterval(() => {
      const currentText = getCurrentSubtitle();
      if (currentText) {
        maybeRequestClipRecording(currentText);
      }
      maybeCompleteClip(currentText, state.lastText);
    }, 100);

    state.clipFallbackTimer = window.setTimeout(() => {
      if (!state.clipMode?.active || state.clipMode.recordingStarted) {
        return;
      }

      state.clipMode.forcedStart = true;
      maybeRequestClipRecording(state.clipMode.targetSubtitle, true);
    }, rewound ? 1800 : 350);

    state.clipMaxTimer = window.setTimeout(() => {
      if (!state.clipMode?.active || !state.clipMode.recordingStarted) {
        return;
      }

      completeClip(getCurrentSubtitle(), "max-duration");
    }, Math.max(3000, Number(maxClipMs) || 8000));
  }

  function completeClipByCue(cue, stopReason) {
    if (!state.clipMode?.active) {
      return;
    }

    const timeline = state.timeline;
    state.clipMode.active = false;

    if (state.clipPollTimer) {
      clearInterval(state.clipPollTimer);
      state.clipPollTimer = null;
    }

    if (state.clipFallbackTimer) {
      clearTimeout(state.clipFallbackTimer);
      state.clipFallbackTimer = null;
    }

    if (state.clipMaxTimer) {
      clearTimeout(state.clipMaxTimer);
      state.clipMaxTimer = null;
    }

    pauseVideoPlayback();
    void chrome.runtime.sendMessage({
      type: "subtitle-clip-complete",
      subtitle: cue.text,
      previousSubtitle: timeline?.cues?.[cue.index - 1]?.text || "",
      nextSubtitle: timeline?.cues?.[cue.index + 1]?.text || "",
      stopReason,
      cue,
      videoEndTime: state.clipMode.plannedVideoEndTime || cue.end,
      videoStartTime: state.clipMode.plannedVideoStartTime
    });
  }

  function getPreviousSubtitleFor(text) {
    const index = state.entries.findIndex((entry) => entry.text === text);
    return index > 0 ? state.entries[index - 1]?.text || "" : "";
  }

  function pauseVideoPlayback() {
    const mediaElement = document.querySelector("video");
    if (mediaElement && !mediaElement.paused) {
      mediaElement.pause();
      return;
    }

    const control = findPlaybackToggle();
    if (control instanceof HTMLElement) {
      control.click();
    }
  }

  function getVideoElement() {
    return document.querySelector("#playerjs video") || document.querySelector("video");
  }

  function getVideoTime() {
    const mediaElement = getVideoElement();
    return mediaElement && Number.isFinite(mediaElement.currentTime)
      ? mediaElement.currentTime
      : undefined;
  }

  function seekVideo(mediaElement, targetSeconds) {
    return new Promise((resolve, reject) => {
      if (!mediaElement || !Number.isFinite(mediaElement.duration)) {
        reject(new Error("No seekable video element found."));
        return;
      }

      const duration = mediaElement.duration || targetSeconds;
      const nextTime = Math.max(0, Math.min(targetSeconds, duration));
      const finish = () => resolve({ currentTime: mediaElement.currentTime, duration });
      const timeout = window.setTimeout(finish, 1200);

      mediaElement.addEventListener("seeked", () => {
        clearTimeout(timeout);
        finish();
      }, { once: true });
      mediaElement.currentTime = nextTime;
    });
  }

  async function prepareAudioRange(startSeconds, endSeconds) {
    const mediaElement = getVideoElement();
    if (!mediaElement) {
      throw new Error("No video element found in #playerjs.");
    }

    if (!Number.isFinite(startSeconds) || !Number.isFinite(endSeconds) || endSeconds <= startSeconds) {
      throw new Error("Invalid audio range.");
    }

    mediaElement.pause();
    const result = await seekVideo(mediaElement, startSeconds);
    return {
      currentTime: result.currentTime,
      duration: result.duration
    };
  }

  function playPreparedAudioRange(endSeconds) {
    const mediaElement = getVideoElement();
    if (!mediaElement) {
      return;
    }

    if (state.rangeTimer) {
      clearTimeout(state.rangeTimer);
      state.rangeTimer = null;
    }

    const durationMs = Math.max(250, (endSeconds - mediaElement.currentTime) * 1000);
    void mediaElement.play().catch(() => {});
    state.rangeTimer = window.setTimeout(() => {
      pauseVideoPlayback();
      void chrome.runtime.sendMessage({
        type: "audio-range-complete"
      });
    }, durationMs);
  }

  function rewindVideoPlayback(rewindMs) {
    const mediaElement = document.querySelector("video");
    if (!mediaElement || !Number.isFinite(mediaElement.currentTime)) {
      return false;
    }

    const rewindSeconds = Math.max(0, rewindMs) / 1000;
    if (rewindSeconds <= 0) {
      return false;
    }

    mediaElement.currentTime = Math.max(0, mediaElement.currentTime - rewindSeconds);
    mediaElement.addEventListener("seeked", () => {
      state.lastText = "";
      maybeRequestClipRecording(getCurrentSubtitle());
    }, { once: true });

    if (mediaElement.paused) {
      void mediaElement.play().catch(() => {});
    }

    return true;
  }

  function maybeRequestClipRecording(text, force = false) {
    if (
      !state.clipMode?.active
      || state.clipMode.cueAware
      || state.clipMode.recordingRequested
      || (!force && text !== state.clipMode.targetSubtitle)
    ) {
      return;
    }

    state.clipMode.recordingRequested = true;
    chrome.runtime.sendMessage({
      type: "subtitle-clip-start-recording",
      videoTime: getVideoTime()
    }, (response) => {
      if (chrome.runtime.lastError || !response?.ok) {
        state.clipMode.recordingRequested = false;
        return;
      }

      state.clipMode.recordingStarted = true;
      state.startedAt = Date.now();
      state.entries = [{
        atMs: 0,
        endMs: 1500,
        sessionStartedAt: state.startedAt,
        text: state.clipMode.targetSubtitle
      }];
      state.lastText = state.clipMode.targetSubtitle;
    });
  }

  function maybeCompleteClip(text, previousText) {
    if (
      !state.clipMode?.active
      || state.clipMode.cueAware
      || !state.clipMode.recordingStarted
      || (!state.clipMode.forcedStart && previousText !== state.clipMode.targetSubtitle)
      || text === state.clipMode.targetSubtitle
    ) {
      return;
    }

    completeClip(text);
  }

  function completeClip(nextSubtitle, stopReason = "subtitle-change") {
    if (!state.clipMode?.active) {
      return;
    }

    state.clipMode.active = false;

    if (state.clipPollTimer) {
      clearInterval(state.clipPollTimer);
      state.clipPollTimer = null;
    }

    if (state.clipFallbackTimer) {
      clearTimeout(state.clipFallbackTimer);
      state.clipFallbackTimer = null;
    }

    if (state.clipMaxTimer) {
      clearTimeout(state.clipMaxTimer);
      state.clipMaxTimer = null;
    }

    pauseVideoPlayback();
    void chrome.runtime.sendMessage({
      type: "subtitle-clip-complete",
      subtitle: state.clipMode.targetSubtitle,
      previousSubtitle: getPreviousSubtitleFor(state.clipMode.targetSubtitle),
      nextSubtitle: nextSubtitle && nextSubtitle !== state.clipMode.targetSubtitle ? nextSubtitle : "",
      stopReason,
      cue: state.clipMode.cue || null,
      videoEndTime: getVideoTime()
    });
  }

  async function selectSubtitleCue(index) {
    const timeline = await getSubtitleTimeline();
    const cue = timeline.cues.find((item) => item.index === index);
    if (!cue) {
      throw new Error("Subtitle cue was not found.");
    }

    const mediaElement = getVideoElement();
    if (mediaElement) {
      await seekVideo(mediaElement, cue.start);
      mediaElement.pause();
    }

    return getCueContext(timeline, cue);
  }

  async function getSubtitleTimeline() {
    if (state.timeline?.cues?.length) {
      return state.timeline;
    }

    if (state.timelinePromise) {
      return state.timelinePromise;
    }

    state.timelinePromise = loadSubtitleTimeline()
      .then((timeline) => {
        state.timeline = timeline;
        return timeline;
      })
      .finally(() => {
        state.timelinePromise = null;
      });

    return state.timelinePromise;
  }

  async function loadSubtitleTimeline() {
    const source = findSubtitleSource();
    if (!source?.url) {
      throw new Error("No VTT subtitle URL found in Playerjs config.");
    }

    const response = await fetch(source.url, { credentials: "include" });
    if (!response.ok) {
      throw new Error(`Could not load subtitles: ${response.status}`);
    }

    const cues = parseVtt(await response.text());
    if (cues.length === 0) {
      throw new Error("Subtitle file has no cues.");
    }

    return {
      cues,
      sourceLabel: source.label,
      sourceUrl: source.url
    };
  }

  function findSubtitleSource() {
    const scripts = Array.from(document.scripts).map((script) => script.textContent || "").join("\n");
    const subtitleConfig = scripts.match(/["']subtitle["']\s*:\s*["']([^"']+)["']/)?.[1]
      || scripts.match(/subtitle\s*:\s*["']([^"']+)["']/)?.[1]
      || "";
    const sources = [];
    const pattern = /\[([^\]]+)\]([^,\s]+?\.vtt(?:\?[^,\s]*)?)/gi;
    let match = pattern.exec(subtitleConfig);
    while (match) {
      sources.push({
        label: match[1],
        url: new URL(match[2], window.location.href).href
      });
      match = pattern.exec(subtitleConfig);
    }

    if (sources.length === 0) {
      const direct = subtitleConfig.match(/https?:\/\/[^,\s"']+?\.vtt(?:\?[^,\s"']*)?/i)?.[0]
        || subtitleConfig.match(/\/[^,\s"']+?\.vtt(?:\?[^,\s"']*)?/i)?.[0];
      if (direct) {
        sources.push({
          label: "Subtitles",
          url: new URL(direct, window.location.href).href
        });
      }
    }

    return sources.find((source) => /english|eng|англ/i.test(source.label)) || sources[0] || null;
  }

  function parseVtt(value) {
    const blocks = value.replace(/\r/g, "").split(/\n{2,}/);
    const cues = [];

    for (const block of blocks) {
      const lines = block.split("\n").map((line) => line.trim()).filter(Boolean);
      if (lines.length === 0 || lines[0] === "WEBVTT") {
        continue;
      }

      const timeLineIndex = lines.findIndex((line) => line.includes("-->"));
      if (timeLineIndex < 0) {
        continue;
      }

      const [rawStart, rawEnd] = lines[timeLineIndex].split("-->").map((part) => part.trim().split(/\s+/)[0]);
      const start = parseVttTime(rawStart);
      const end = parseVttTime(rawEnd);
      const text = normalizeText(lines.slice(timeLineIndex + 1).join(" ").replace(/<[^>]+>/g, ""));
      if (!Number.isFinite(start) || !Number.isFinite(end) || end <= start || !text) {
        continue;
      }

      cues.push({
        end,
        index: cues.length,
        start,
        text
      });
    }

    return cues;
  }

  function parseVttTime(value) {
    const parts = value.split(":");
    const secondsPart = parts.pop() || "0";
    const seconds = Number(secondsPart.replace(",", "."));
    const minutes = Number(parts.pop() || 0);
    const hours = Number(parts.pop() || 0);
    return hours * 3600 + minutes * 60 + seconds;
  }

  function getCueContext(timeline, selectedCue) {
    const videoTime = getVideoTime();
    const cue = selectedCue || timeline.cues.find((item) => videoTime !== undefined && videoTime >= item.start && videoTime <= item.end)
      || findNearestCue(timeline.cues, videoTime);
    if (!cue) {
      return null;
    }

    return {
      currentSubtitle: cue.text,
      previousSubtitle: timeline.cues[cue.index - 1]?.text || "",
      nextSubtitle: timeline.cues[cue.index + 1]?.text || "",
      cue,
      timeline: {
        cues: timeline.cues.slice(Math.max(0, cue.index - 4), Math.min(timeline.cues.length, cue.index + 5)),
        sourceLabel: timeline.sourceLabel,
        sourceUrl: timeline.sourceUrl
      },
      videoTime
    };
  }

  function findNearestCue(cues, videoTime) {
    if (!Number.isFinite(videoTime)) {
      return null;
    }

    return cues.find((cue) => videoTime < cue.start) || cues[cues.length - 1] || null;
  }

  function findPlaybackToggle() {
    const candidates = Array.from(document.querySelectorAll("div"));
    return candidates.find((element) => {
      const style = element.getAttribute("style") || "";
      return style.includes("cursor: pointer")
        && style.includes("pointer-events: auto")
        && style.includes("20px")
        && style.includes("transform: scale(1.8)");
    }) || null;
  }
})();
