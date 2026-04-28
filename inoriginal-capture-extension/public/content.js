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
    lastText: "",
    clipMode: null
  };

  chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message?.type === "start-subtitle-capture") {
      startCapture(message.startedAt);
      sendResponse({ ok: true });
      return;
    }

    if (message?.type === "stop-subtitle-capture") {
      stopCapture(message.stoppedAt);
      sendResponse({
        entries: state.entries,
        ...getSubtitleContext()
      });
      return;
    }

    if (message?.type === "get-current-subtitle") {
      sendResponse({ currentSubtitle: getCurrentSubtitle() });
      return;
    }

    if (message?.type === "get-current-subtitle-context") {
      sendResponse(getSubtitleContext());
      return;
    }

    if (message?.type === "start-subtitle-clip") {
      startSubtitleClip(message.startedAt, message.targetSubtitle, message.rewindMs || 0);
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

  function getSubtitleContext() {
    const currentSubtitle = getCurrentSubtitle();
    const currentIndex = state.entries.findIndex((entry) => entry.text === currentSubtitle);
    const fallbackIndex = state.entries.length - 1;
    const resolvedIndex = currentIndex >= 0 ? currentIndex : fallbackIndex;

    return {
      currentSubtitle,
      previousSubtitle: resolvedIndex > 0 ? state.entries[resolvedIndex - 1]?.text || "" : "",
      nextSubtitle: resolvedIndex >= 0 && resolvedIndex < state.entries.length - 1
        ? state.entries[resolvedIndex + 1]?.text || ""
        : ""
    };
  }

  function startSubtitleClip(startedAt, targetSubtitle, rewindMs) {
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

      completeClip(getCurrentSubtitle());
    }, 12000);
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
      || state.clipMode.recordingRequested
      || (!force && text !== state.clipMode.targetSubtitle)
    ) {
      return;
    }

    state.clipMode.recordingRequested = true;
    chrome.runtime.sendMessage({
      type: "subtitle-clip-start-recording"
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
      || !state.clipMode.recordingStarted
      || (!state.clipMode.forcedStart && previousText !== state.clipMode.targetSubtitle)
      || text === state.clipMode.targetSubtitle
    ) {
      return;
    }

    completeClip(text);
  }

  function completeClip(nextSubtitle) {
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
      nextSubtitle: nextSubtitle && nextSubtitle !== state.clipMode.targetSubtitle ? nextSubtitle : ""
    });
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
