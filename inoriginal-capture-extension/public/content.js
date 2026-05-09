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
    timelinePromise: null,
    // Только URL страницы (path/query/hash). Не включать video.currentSrc: у HLS это часто blob:, меняется во время воспроизведения и ломает загрузку VTT и запись аудио.
    timelinePageSignature: null
  };

  /** Сброс кэша таймлайна при навигации без полной перезагрузки (другой сериал/эпизод). */
  function clearSubtitleTimelineCache() {
    state.timeline = null;
    state.timelinePageSignature = null;
  }

  if (typeof window !== "undefined") {
    window.addEventListener("popstate", clearSubtitleTimelineCache);
    window.addEventListener("hashchange", clearSubtitleTimelineCache);
  }

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
      if (message.captureMode === "dom-fallback") {
        sendResponse(getDomSubtitleContext());
        return;
      }

      void getSubtitleContext()
        .then((context) => sendResponse(context))
        .catch(() => sendResponse(getDomSubtitleContext()));
      return true;
    }

    if (message?.type === "start-subtitle-clip") {
      startSubtitleClip(message.startedAt, message.targetSubtitle, message.rewindMs || 0, message.maxClipMs || 8000, message.cue || null, message.captureMode || "dom-fallback");
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
    if (state.clipMode?.retryRecordingTimer) {
      clearTimeout(state.clipMode.retryRecordingTimer);
    }
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
    // Прямой потомок или вложенный span (у плеера часто обёртка + стилизованный span с текстом)
    return (
      document.querySelector("#pjs_playerjs_subtitle span")
      || document.querySelector("#pjs_playerjs_subtitle > span")
    );
  }

  function normalizeText(value) {
    return String(value || "")
      .normalize("NFKC")
      .replace(/\s+/g, " ")
      .trim();
  }

  function getCurrentSubtitle() {
    const element = findSubtitleSpan();
    return normalizeText(element?.textContent || "") || state.lastText || "";
  }

  function getDisplayedSubtitle() {
    const element = findSubtitleSpan();
    return normalizeText(element?.textContent || "");
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
    const timeline = state.timeline;
    const nextCue = timeline?.cues?.[cue.index + 1];
    const nextCueStart = nextCue && Number.isFinite(nextCue.start) ? nextCue.start : null;

    state.clipMode = {
      active: true,
      cue,
      cueAware: true,
      plannedVideoEndTime: 0,
      plannedVideoStartTime: 0,
      targetSubtitle,
      recordingRequested: true,
      recordingStarted: false,
      forcedStart: true,
      nextCueStart,
      targetNormalized: normalizeText(targetSubtitle)
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
    const safetyCapMs = Math.max(3000, Number(maxClipMs) || 8000);

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

        // Остановка при начале следующей реплики:
        // 1) по факту смены активного VTT cue (более надежно при дрейфе таймингов),
        // 2) по времени nextCueStart как быстрый резерв.
        state.clipPollTimer = window.setInterval(() => {
          if (!state.clipMode?.active || !state.clipMode.cueAware) {
            return;
          }
          const media = getVideoElement();
          if (!media) {
            return;
          }
          const t = media.currentTime;
          const nextAt = state.clipMode.nextCueStart;
          const activeCue = timeline?.cues?.find((item) => t >= item.start && t <= item.end + 0.03) || null;
          if (activeCue && activeCue.index > cue.index) {
            completeClipByCue(cue, "next-cue-start", getVideoTime());
            return;
          }
          if (nextAt != null && t >= nextAt - 0.03) {
            completeClipByCue(cue, "next-cue-start", getVideoTime());
            return;
          }
          // Резерв по DOM только если в VTT нет следующего куя: иначе текст в DOM может быть многострочным блоком,
          // а targetSubtitle — одна строка, и запись оборвётся сразу.
          if (nextAt == null) {
            const domNorm = normalizeText(findSubtitleSpan()?.textContent || "");
            if (domNorm && domNorm !== state.clipMode.targetNormalized) {
              completeClipByCue(cue, "subtitle-change", getVideoTime());
            }
          }
        }, 85);

        // Нет следующего куя — как раньше: обрезка по рассчитанному концу реплики
        // Есть следующий — верхняя граница только safetyCap (если не сработала смена реплики)
        const timerMs = nextCueStart != null ? safetyCapMs : plannedDurationMs;
        const stopReason = nextCueStart != null ? "max-duration" : "cue-end";

        state.clipMaxTimer = window.setTimeout(() => {
          if (!state.clipMode?.active) {
            return;
          }
          completeClipByCue(cue, stopReason);
        }, timerMs);
      }, recorderWarmupMs);
    });
  }

  function startDomSubtitleClip(startedAt, targetSubtitle, rewindMs, maxClipMs) {
    startCapture(startedAt);
    const rewound = rewindVideoPlayback(rewindMs);
    const normalizedTarget = normalizeText(targetSubtitle);
    state.clipMode = {
      active: true,
      targetSubtitle,
      targetNormalized: normalizedTarget,
      targetWasVisible: false,
      recordingRequested: false,
      recordingStarted: false,
      forcedStart: false,
      recordingStartAttempts: 0
    };

    maybeRequestClipRecording(getDisplayedSubtitle());

    state.clipPollTimer = window.setInterval(() => {
      const displayedText = getDisplayedSubtitle();
      const displayedNormalized = normalizeText(displayedText);

      if (displayedNormalized === state.clipMode?.targetNormalized) {
        state.clipMode.targetWasVisible = true;
        maybeRequestClipRecording(displayedText);
      }

      // Для аудио важен не "последний известный текст", а реальный конец реплики на экране:
      // как только текущий DOM-субтитр исчез или сменился после старта записи — останавливаем запись и player.
      if (
        state.clipMode?.recordingStarted
        && state.clipMode.targetWasVisible
        && displayedNormalized !== state.clipMode.targetNormalized
      ) {
        completeClip(displayedText, displayedNormalized ? "subtitle-change" : "subtitle-ended");
        return;
      }
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

  /**
   * @param {number} [videoEndTimeOverride] — фактическое время остановки (следующая реплика / смена DOM)
   */
  function completeClipByCue(cue, stopReason, videoEndTimeOverride) {
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

    if (state.clipMode?.retryRecordingTimer) {
      clearTimeout(state.clipMode.retryRecordingTimer);
      state.clipMode.retryRecordingTimer = null;
    }

    pauseVideoPlayback();
    const endT =
      Number.isFinite(videoEndTimeOverride)
        ? videoEndTimeOverride
        : state.clipMode.plannedVideoEndTime || cue.end;

    void chrome.runtime.sendMessage({
      type: "subtitle-clip-complete",
      subtitle: cue.text,
      previousSubtitle: timeline?.cues?.[cue.index - 1]?.text || "",
      nextSubtitle: timeline?.cues?.[cue.index + 1]?.text || "",
      stopReason,
      cue,
      videoEndTime: endT,
      videoStartTime: state.clipMode.plannedVideoStartTime
    });
  }

  function getPreviousSubtitleFor(text) {
    const index = state.entries.findIndex((entry) => entry.text === text);
    return index > 0 ? state.entries[index - 1]?.text || "" : "";
  }

  function pauseVideoPlayback() {
    const mediaElement = getVideoElement();
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
    const mediaElement = getVideoElement();
    if (!mediaElement || !Number.isFinite(mediaElement.currentTime)) {
      return false;
    }

    const rewindSeconds = Math.max(0, rewindMs) / 1000;
    if (rewindSeconds <= 0) {
      return false;
    }

    mediaElement.currentTime = Math.max(0, mediaElement.currentTime - rewindSeconds);

    // Некоторые HLS/PlayerJS не всегда шлют seeked — дублируем запуск опроса по таймауту
    let seekHandled = false;
    const afterSeek = () => {
      if (seekHandled) {
        return;
      }
      seekHandled = true;
      clearTimeout(seekFallbackTimer);
      state.lastText = "";
      maybeRequestClipRecording(getDisplayedSubtitle());
    };
    const seekFallbackTimer = window.setTimeout(afterSeek, 700);
    mediaElement.addEventListener("seeked", afterSeek, { once: true });

    if (mediaElement.paused) {
      void mediaElement.play().catch(() => {});
    }

    return true;
  }

  function maybeRequestClipRecording(text, force = false) {
    const displayedNorm = normalizeText(text || "");
    if (
      !state.clipMode?.active
      || state.clipMode.cueAware
      || state.clipMode.recordingRequested
      || (!force && displayedNorm !== state.clipMode.targetNormalized)
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
        // Повтор через 2 с при временном сбое tabCapture (не более 4 попыток)
        const attempts = (state.clipMode.recordingStartAttempts || 0) + 1;
        if (state.clipMode) {
          state.clipMode.recordingStartAttempts = attempts;
        }
        if (
          state.clipMode?.active
          && !state.clipMode.recordingStarted
          && !state.clipMode.retryRecordingTimer
          && attempts <= 4
        ) {
          state.clipMode.retryRecordingTimer = window.setTimeout(() => {
            if (!state.clipMode?.active) {
              return;
            }
            state.clipMode.retryRecordingTimer = null;
            maybeRequestClipRecording(state.clipMode.targetSubtitle, true);
          }, 2000);
        }
        return;
      }

      if (state.clipMode.retryRecordingTimer) {
        clearTimeout(state.clipMode.retryRecordingTimer);
        state.clipMode.retryRecordingTimer = null;
      }
      state.clipMode.recordingStarted = true;
      state.clipMode.targetWasVisible = true;
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
      || (!state.clipMode.forcedStart && normalizeText(previousText) !== state.clipMode.targetNormalized)
      || normalizeText(text) === state.clipMode.targetNormalized
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
    const videoEndTime = getVideoTime();

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

    if (state.clipMode?.retryRecordingTimer) {
      clearTimeout(state.clipMode.retryRecordingTimer);
      state.clipMode.retryRecordingTimer = null;
    }

    pauseVideoPlayback();
    void chrome.runtime.sendMessage({
      type: "subtitle-clip-complete",
      subtitle: state.clipMode.targetSubtitle,
      previousSubtitle: getPreviousSubtitleFor(state.clipMode.targetSubtitle),
      nextSubtitle: nextSubtitle && nextSubtitle !== state.clipMode.targetSubtitle ? nextSubtitle : "",
      stopReason,
      cue: state.clipMode.cue || null,
      videoEndTime
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

  /** Смена эпизода на SPA обычно меняет path/search/hash; этого достаточно, чтобы сбросить кэш VTT. */
  function getPageLocationSignature() {
    return `${location.pathname}${location.search}${location.hash}`;
  }

  async function getSubtitleTimeline() {
    const pageSig = getPageLocationSignature();
    if (state.timeline?.cues?.length && state.timelinePageSignature === pageSig) {
      return state.timeline;
    }

    state.timeline = null;

    if (state.timelinePromise) {
      return state.timelinePromise;
    }

    const loadStartedPage = pageSig;
    state.timelinePromise = loadSubtitleTimeline()
      .then((timeline) => {
        if (getPageLocationSignature() !== loadStartedPage) {
          clearSubtitleTimelineCache();
          throw new Error("Страница сменилась во время загрузки субтитров.");
        }
        state.timeline = timeline;
        state.timelinePageSignature = loadStartedPage;
        return timeline;
      })
      .finally(() => {
        state.timelinePromise = null;
      });

    return state.timelinePromise;
  }

  async function loadSubtitleTimeline() {
    const candidates = collectVttCandidatesSync();
    let source = pickPreferredVttSource(candidates);
    if (!source) {
      source = await tryPickVttFromHlsManifest();
    }
    if (!source?.url) {
      throw new Error(
        "No VTT URL on the page: checked inline scripts, #playerjs markup, <track>, bare .vtt URLs, and HLS master playlist."
      );
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

  /** Собираем текст страницы, где часто лежит конфиг Playerjs / ссылки на VTT (внешние .js без доступа к телу не сканируем). */
  function getSubtitleScanText() {
    const parts = [];
    for (const script of document.scripts) {
      parts.push(script.textContent || "");
    }
    // Разметка и data-* контейнеров плеера (субтитры могут быть только в innerHTML, не в inline script).
    const roots = document.querySelectorAll(
      "#playerjs, #pjs_playerjs, [id*='playerjs' i], [id^='pjs_'], [class*='playerjs' i]"
    );
    for (const el of roots) {
      parts.push(el.innerHTML);
      for (const attr of el.attributes) {
        parts.push(attr.value);
      }
    }
    return parts.join("\n");
  }

  /**
   * Все обнаруженные пары label + url (без приоритета). Разные режимы: VTT с таймлайном vs DOM — по-прежнему
   * раздельно; здесь только то, откуда реально можно скачать .vtt для режима auto-vtt.
   */
  function collectVttCandidatesSync() {
    const seen = new Set();
    /** @type {{ label: string, url: string }[]} */
    const list = [];

    function add(label, urlRaw) {
      if (!urlRaw || typeof urlRaw !== "string") {
        return;
      }
      const trimmed = urlRaw.trim();
      if (!/\.vtt(\?|#|$)/i.test(trimmed)) {
        return;
      }
      try {
        const url = new URL(trimmed, window.location.href).href;
        if (seen.has(url)) {
          return;
        }
        seen.add(url);
        list.push({ label: label || "Subtitles", url });
      } catch (_) {
        /* ignore */
      }
    }

    const fullText = getSubtitleScanText();

    // Нативные дорожки у <video> — то, что плеер мог подставить в разметку.
    for (const track of document.querySelectorAll("video track, track")) {
      const kind = (track.getAttribute("kind") || "").toLowerCase();
      if (kind && kind !== "subtitles" && kind !== "captions") {
        continue;
      }
      add(
        track.getAttribute("label") || track.getAttribute("srclang") || "track",
        track.getAttribute("src")
      );
    }

    // Playerjs: subtitle: "[Label]url.vtt, ..." или отдельная строка только с квадратными скобками.
    const subtitleBlock =
      fullText.match(/["']subtitle["']\s*:\s*["']([^"']+)["']/i)?.[1]
      || fullText.match(/\bsubtitle\s*:\s*["']([^"']+)["']/i)?.[1]
      || "";
    const textsToScan = [subtitleBlock, fullText];

    for (const text of textsToScan) {
      if (!text) {
        continue;
      }
      const bracket = /\[([^\]]+)\]\s*([^\s,]+\.vtt[^\s,]*)/gi;
      let m = bracket.exec(text);
      while (m) {
        add(m[1], m[2]);
        m = bracket.exec(text);
      }
    }

    // JSON-подобные поля в скриптах: "file"|"src"|"url" -> *.vtt
    const jsonUrl = /"(?:file|src|url)"\s*:\s*"([^"]+\.vtt[^"]*)"/gi;
    for (let jm = jsonUrl.exec(fullText); jm; jm = jsonUrl.exec(fullText)) {
      add("Subtitles", jm[1]);
    }

    // Первый попавшийся абсолютный или относительный .vtt в том же тексте (запасной путь).
    const bare =
      fullText.match(/https?:\/\/[^\s"'<>]+\.vtt(?:\?[^\s"'<>]*)?/i)?.[0]
      || fullText.match(/\/[^\s"'<>]+\.vtt(?:\?[^\s"'<>]*)?/i)?.[0];
    if (bare) {
      add("Subtitles", bare);
    }

    return list;
  }

  /** Чем ближе путь .vtt к пути текущего медиа, тем вероятнее это дорожка именно этого эпизода, а не чужой .vtt со страницы. */
  function scoreVttUrlAgainstVideo(vttUrl, videoSrc) {
    if (!videoSrc || !vttUrl) {
      return 0;
    }
    try {
      const v = new URL(videoSrc, window.location.href);
      const t = new URL(vttUrl, window.location.href);
      if (v.hostname !== t.hostname) {
        return 0;
      }
      const vParts = v.pathname.split("/").filter(Boolean);
      const tParts = t.pathname.split("/").filter(Boolean);
      let score = 0;
      for (let i = 0; i < Math.min(vParts.length, tParts.length); i++) {
        if (vParts[i] === tParts[i]) {
          score += 20;
        } else {
          break;
        }
      }
      return score;
    } catch (_) {
      return 0;
    }
  }

  function pickPreferredVttSource(candidates) {
    if (!candidates?.length) {
      return null;
    }
    const video = getVideoElement();
    const videoSrc = (video && (video.currentSrc || video.src)) || "";
    const ranked = candidates
      .map((c, index) => {
        let score = scoreVttUrlAgainstVideo(c.url, videoSrc);
        if (/english|eng|англ/i.test(c.label)) {
          score += 15;
        }
        return { label: c.label, url: c.url, score, _order: index };
      })
      .sort((a, b) => b.score - a.score || a._order - b._order);
    const best = ranked[0];
    return { label: best.label, url: best.url };
  }

  function collectM3u8UrlsFromText(text) {
    const urls = [];
    const seen = new Set();
    const abs = /https?:\/\/[^\s"'<>]+\.m3u8(?:\?[^\s"'<>]*)?/gi;
    let m = abs.exec(text);
    while (m) {
      const href = m[0];
      if (!seen.has(href)) {
        seen.add(href);
        urls.push(href);
      }
      m = abs.exec(text);
    }
    const rel = /\/[^\s"'<>]+\.m3u8(?:\?[^\s"'<>]*)?/gi;
    m = rel.exec(text);
    while (m) {
      try {
        const href = new URL(m[0], window.location.href).href;
        if (!seen.has(href)) {
          seen.add(href);
          urls.push(href);
        }
      } catch (_) {
        /* ignore */
      }
      m = rel.exec(text);
    }
    const video = getVideoElement();
    const videoSrc = (video && (video.currentSrc || video.src)) || "";
    return urls
      .map((u) => ({ url: u, score: scoreVttUrlAgainstVideo(u, videoSrc) }))
      .sort((a, b) => b.score - a.score)
      .map((x) => x.url);
  }

  /**
   * Если субтитры приходят из HLS: в master .m3u8 строка #EXT-X-MEDIA:TYPE=SUBTITLES,... URI="....vtt"
   * Несколько master на странице — берём manifest ближе к URL текущего видео.
   */
  async function tryPickVttFromHlsManifest() {
    const text = getSubtitleScanText();
    const orderedM3u8 = collectM3u8UrlsFromText(text);
    for (const m3u8Url of orderedM3u8) {
      try {
        const manifestUrl = new URL(m3u8Url, window.location.href).href;
        const res = await fetch(manifestUrl, { credentials: "include" });
        if (!res.ok) {
          continue;
        }
        const body = await res.text();
        /** @type {{ label: string, url: string }[]} */
        const out = [];
        for (const line of body.split(/\r?\n/)) {
          if (!/TYPE=SUBTITLES/i.test(line)) {
            continue;
          }
          const uri = line.match(/URI="([^"]+)"/i)?.[1];
          if (!uri || !/\.vtt/i.test(uri)) {
            continue;
          }
          const label =
            line.match(/NAME="([^"]+)"/i)?.[1]
            || line.match(/LANGUAGE="([^"]+)"/i)?.[1]
            || "Subtitles";
          out.push({ label, url: new URL(uri, manifestUrl).href });
        }
        const picked = pickPreferredVttSource(out);
        if (picked) {
          return picked;
        }
      } catch (_) {
        /* следующий manifest */
      }
    }
    return null;
  }

  function parseVtt(value) {
    // BOM + CRLF: без нормализации первая строка может быть "\uFEFFWEBVTT"
    const normalized = value.replace(/^\uFEFF/, "").replace(/\r/g, "");
    const blocks = normalized.split(/\n{2,}/);
    const cues = [];

    for (const block of blocks) {
      let lines = block.split("\n").map((line) => line.trim()).filter(Boolean);
      if (lines.length === 0) {
        continue;
      }
      // Не отбрасывать блок целиком из‑за WEBVTT: при одном \n между заголовком и первой репликой
      // (без пустой строки) здесь же окажутся и куи — иначе файл парсился как пустой.
      while (lines.length) {
        const head = lines[0].replace(/^\uFEFF/, "");
        if (head === "WEBVTT") {
          lines.shift();
          continue;
        }
        if (/^(Kind|Language|Region):/i.test(head)) {
          lines.shift();
          continue;
        }
        if (/^X-TIMESTAMP-MAP=/i.test(head)) {
          lines.shift();
          continue;
        }
        break;
      }
      if (lines.length === 0) {
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
    if (!Number.isFinite(videoTime) || !cues?.length) {
      return null;
    }

    // Раньше: cues.find((cue) => videoTime < cue.start) — это ПЕРВЫЙ будущий куй.
    // В паузе между репликами показывался текст следующей фразы (иногда звуковые метки вроде [beeping]),
    // хотя на экране ещё предыдущая или пусто. В промежутке берём последний уже закончившийся куй.
    let lastBeforeOrInside = null;
    for (const cue of cues) {
      if (videoTime < cue.start) {
        return lastBeforeOrInside;
      }
      if (videoTime <= cue.end) {
        return cue;
      }
      lastBeforeOrInside = cue;
    }
    return lastBeforeOrInside;
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
