import { useEffect, useRef, useState } from "react";
import { getPopupContext, saveAnkiSettings, sendRuntimeMessage } from "../shared/chromeApi";
import type { CaptureData, FieldMapping, PopupContext, SentenceDraft } from "../shared/types";
import "./styles.css";

type CaptureAppProps = {
  mode: "popup" | "sidepanel";
};

type FlowStatus = "Idle" | "Rewinding" | "Recording subtitle audio" | "Ready to review" | "Sending to Anki" | "Created" | "Failed" | "Cancelled";
type TrimSuggestion = {
  start: number;
  end: number;
};
type SmartAction = "capture" | "stop-recording" | "pick-word" | "define-word" | "translate" | "fix-audio" | "open-settings" | "send";
type QualityTone = "required" | "recommended" | "risk";
type QualityItem = {
  label: string;
  done: boolean;
  detail: string;
  tone: QualityTone;
};
type CardQuality = {
  cta: string;
  disabled: boolean;
  footerCopy: string;
  items: QualityItem[];
  nextAction: SmartAction;
  score: number;
  status: "Blocked" | "Needs review" | "Ready";
};

export function CaptureApp({ mode }: CaptureAppProps) {
  const [context, setContext] = useState<PopupContext | null>(null);
  const [flowStatus, setFlowStatus] = useState<FlowStatus>("Idle");
  const [message, setMessage] = useState("Loading...");
  const [expression, setExpression] = useState("");
  const [word, setWord] = useState("");
  const [transcription, setTranscription] = useState("");
  const [wordTypes, setWordTypes] = useState("");
  const [translation, setTranslation] = useState("");
  const [definition, setDefinition] = useState("");
  const [example, setExample] = useState("");
  const [synonyms, setSynonyms] = useState("");
  const [antonyms, setAntonyms] = useState("");
  const [source, setSource] = useState("");
  const [url, setUrl] = useState("");
  const [isTranslating, setIsTranslating] = useState(false);
  const [isLookingUpWord, setIsLookingUpWord] = useState(false);
  const [duplicateWarning, setDuplicateWarning] = useState("");
  const [audioDuration, setAudioDuration] = useState<number | null>(null);
  const [audioRangeStart, setAudioRangeStart] = useState(0);
  const [audioRangeEnd, setAudioRangeEnd] = useState(0);
  const [waveformPeaks, setWaveformPeaks] = useState<number[]>([]);
  const [trimSuggestion, setTrimSuggestion] = useState<TrimSuggestion | null>(null);
  const [isAnalyzingAudio, setIsAnalyzingAudio] = useState(false);
  const [waveformError, setWaveformError] = useState("");
  const [showAudioAdvanced, setShowAudioAdvanced] = useState(false);
  const lastCaptureSignature = useRef("");
  const lastSavedDraftSignature = useRef("");
  const lastAutoTranslateSignature = useRef("");
  const lastAudioRangeFile = useRef("");
  const lastAutoTrimFile = useRef("");
  const hasHydratedEditor = useRef(false);
  const expressionRef = useRef<HTMLTextAreaElement | null>(null);
  const confirmedDuplicateExpression = useRef("");

  useEffect(() => {
    void refresh();
    const timer = window.setInterval(() => {
      void refresh(false);
    }, 1500);

    return () => window.clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!hasHydratedEditor.current) {
      return;
    }

    const draft = buildSentenceDraft({
      expression,
      word,
      transcription,
      wordTypes,
      translation,
      definition,
      example,
      synonyms,
      antonyms,
      source,
      url
    });
    const signature = buildDraftSignature(draft);
    if (signature === lastSavedDraftSignature.current) {
      return;
    }

    lastSavedDraftSignature.current = signature;
    void sendRuntimeMessage({
      type: "save-sentence-draft",
      draft
    });
  }, [expression, word, transcription, wordTypes, translation, definition, example, synonyms, antonyms, source, url]);

  useEffect(() => {
    if (
      context?.settings.translationMode !== "after-capture"
      || !context.capture?.audio?.dataUrl
      || !expression.trim()
      || translation.trim()
      || isTranslating
    ) {
      return;
    }

    const signature = `${context.capture.audio.filename || ""}|${expression}|${context.settings.translationSourceLang}|${context.settings.translationTargetLang}`;
    if (signature === lastAutoTranslateSignature.current) {
      return;
    }

    lastAutoTranslateSignature.current = signature;
    void translateSubtitle({ silent: true });
  }, [context, expression, translation, isTranslating]);

  useEffect(() => {
    const duration = getEffectiveAudioDuration(context?.capture, audioDuration);
    const filename = context?.capture?.audio?.filename || "";
    if (!duration || !filename || lastAudioRangeFile.current === filename) {
      return;
    }

    lastAudioRangeFile.current = filename;
    setAudioRangeStart(0);
    setAudioRangeEnd(Number(duration.toFixed(2)));
  }, [context?.capture, audioDuration]);

  useEffect(() => {
    const audio = context?.capture?.audio;
    const filename = audio?.filename || "";
    if (!audio?.dataUrl || !filename) {
      setWaveformPeaks([]);
      setTrimSuggestion(null);
      setWaveformError("");
      return;
    }

    let cancelled = false;
    setIsAnalyzingAudio(true);
    setWaveformError("");

    analyzeAudioDataUrl(audio.dataUrl)
      .then((analysis) => {
        if (cancelled) {
          return;
        }

        setWaveformPeaks(analysis.peaks);
        setTrimSuggestion(analysis.trim);
        setAudioDuration((current) => current || analysis.duration);

        if (analysis.trim && lastAutoTrimFile.current !== filename) {
          lastAutoTrimFile.current = filename;
          setAudioRangeStart(analysis.trim.start);
          setAudioRangeEnd(analysis.trim.end);
        }
      })
      .catch((error) => {
        if (!cancelled) {
          setWaveformPeaks([]);
          setTrimSuggestion(null);
          setWaveformError(error instanceof Error ? error.message : "Could not analyze audio waveform.");
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsAnalyzingAudio(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [context?.capture?.audio?.dataUrl, context?.capture?.audio?.filename]);

  async function refresh(showLoading = true) {
    if (showLoading) {
      setMessage("Loading...");
    }

    try {
      const nextContext = await getPopupContext();
      setContext(nextContext);
      hydrateEditor(nextContext.capture, nextContext.sentenceDraft);
      setFlowStatus(deriveFlowStatus(nextContext));
      setMessage(buildMessage(nextContext));
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Failed to load extension UI.");
    }
  }

  function hydrateEditor(capture?: CaptureData, storedDraft?: SentenceDraft) {
    const signature = buildCaptureSignature(capture);
    if (hasHydratedEditor.current && signature === lastCaptureSignature.current) {
      return;
    }

    const nextDraft = buildSentenceDraft({
      ...storedDraft,
      expression: storedDraft?.expression || capture?.subtitle || "",
      example: storedDraft?.example || buildExampleText(capture),
      source: storedDraft?.source || capture?.pageTitle || "",
      url: storedDraft?.url || capture?.pageUrl || ""
    });

    setExpression(nextDraft.expression);
    setWord(nextDraft.word);
    setTranscription(nextDraft.transcription);
    setWordTypes(nextDraft.wordTypes);
    setTranslation(nextDraft.translation);
    setDefinition(nextDraft.definition);
    setExample(nextDraft.example);
    setSynonyms(nextDraft.synonyms);
    setAntonyms(nextDraft.antonyms);
    setSource(nextDraft.source);
    setUrl(nextDraft.url);
    setDuplicateWarning("");
    setAudioDuration(null);
    lastCaptureSignature.current = signature;
    lastSavedDraftSignature.current = buildDraftSignature(nextDraft);
    hasHydratedEditor.current = true;
  }

  async function captureSubtitleClip() {
    setFlowStatus("Rewinding");
    setMessage("Preparing subtitle capture...");
    const response = await sendRuntimeMessage({ type: "capture-subtitle-clip" });
    if (!response.ok) {
      await refresh(false);
      setFlowStatus("Failed");
      setMessage(response.error || "Capture failed.");
      return;
    }

    await refresh(false);
  }

  async function retakeScreenshot() {
    setMessage("Retaking screenshot...");
    const response = await sendRuntimeMessage({ type: "take-screenshot" });
    if (!response.ok) {
      setMessage(response.error || "Screenshot failed.");
      return;
    }

    await refresh();
  }

  async function recaptureAudio() {
    setFlowStatus("Rewinding");
    setMessage("Re-recording subtitle audio...");
    const response = await sendRuntimeMessage({ type: "recapture-subtitle-audio" });
    if (!response.ok) {
      await refresh(false);
      setFlowStatus("Failed");
      setMessage(response.error || "Audio re-record failed.");
      return;
    }

    await refresh(false);
  }

  async function cancelCapture() {
    setMessage("Cancelling capture...");
    const response = await sendRuntimeMessage({ type: "cancel-capture" });
    if (!response.ok) {
      setMessage(response.error || "Could not cancel capture.");
      return;
    }

    await refresh(false);
  }

  async function stopRecording() {
    setMessage("Stopping audio recording...");
    const response = await sendRuntimeMessage({ type: "stop-current-capture" });
    if (!response.ok) {
      await refresh(false);
      setFlowStatus("Failed");
      setMessage(response.error || "Could not stop recording.");
      return;
    }

    await refresh(false);
  }

  async function recaptureSelectedAudioRange() {
    const effectiveDuration = getEffectiveAudioDuration(context?.capture, audioDuration);
    const rangeEnd = effectiveDuration
      ? Math.min(audioRangeEnd || effectiveDuration, effectiveDuration)
      : audioRangeEnd;

    if (!effectiveDuration || rangeEnd <= audioRangeStart) {
      setMessage("Choose a valid audio range first.");
      return;
    }

    setFlowStatus("Recording subtitle audio");
    setMessage("Re-recording selected range...");
    const response = await sendRuntimeMessage({
      type: "record-audio-range",
      payload: {
        startOffsetMs: Math.round(audioRangeStart * 1000),
        endOffsetMs: Math.round(rangeEnd * 1000)
      }
    });

    if (!response.ok) {
      await refresh(false);
      setFlowStatus("Failed");
      setMessage(response.error || "Selected range re-record failed.");
      return;
    }

    await refresh(false);
  }

  function applyTrimSuggestion() {
    if (!trimSuggestion) {
      setMessage("No speech range found to auto-trim.");
      return;
    }

    setAudioRangeStart(trimSuggestion.start);
    setAudioRangeEnd(trimSuggestion.end);
    setMessage(`Auto-trim applied: ${trimSuggestion.start.toFixed(1)}s to ${trimSuggestion.end.toFixed(1)}s.`);
  }

  function resetAudioRange() {
    const effectiveDuration = getEffectiveAudioDuration(context?.capture, audioDuration);
    if (!effectiveDuration) {
      return;
    }

    setAudioRangeStart(0);
    setAudioRangeEnd(Number(effectiveDuration.toFixed(2)));
    setMessage("Audio range reset to the full clip.");
  }

  async function createCard() {
    if (!canSendToAnki(context, expression)) {
      setMessage("Capture subtitle, screenshot, audio, deck, and note type first.");
      return;
    }

    let finalTranslation = translation;
    if (context?.settings.translationMode === "before-send" && !finalTranslation.trim()) {
      const translatedText = await translateSubtitle({ silent: true });
      if (!translatedText) {
        setMessage("Translation failed. You can edit Translation manually or switch translation mode to Manual.");
        return;
      }
      finalTranslation = translatedText;
    }

    const duplicateOk = await warnIfDuplicateExpression();
    if (!duplicateOk) {
      return;
    }

    setFlowStatus("Sending to Anki");
    setMessage("Sending card to Anki...");
    const response = await sendRuntimeMessage<{ noteId: number }>({
      type: "create-anki-card",
      payload: {
        subtitle: expression || context?.capture?.subtitle,
        draft: {
          expression,
          word,
          transcription,
          wordTypes,
          translation: finalTranslation,
          definition,
          example,
          synonyms,
          antonyms,
          source,
          url
        },
        settings: {
          deckName: context?.settings.deckName,
          modelName: context?.settings.modelName
        }
      }
    });

    if (!response.ok) {
      await refresh(false);
      setFlowStatus("Failed");
      setMessage(response.error || "Failed to create card.");
      return;
    }

    await refresh(false);
    setFlowStatus("Created");
    setMessage(`Card created: ${response.result?.noteId}`);
  }

  async function translateSubtitle(options: { silent?: boolean } = {}) {
    const text = expression || context?.capture?.subtitle || "";
    if (!text.trim()) {
      if (!options.silent) {
        setMessage("No subtitle text to translate.");
      }
      return "";
    }

    setIsTranslating(true);
    if (!options.silent) {
      setMessage("Translating subtitle...");
    }
    const response = await sendRuntimeMessage<{ translatedText: string; provider: string }>({
      type: "translate-text",
      text,
      options: {
        sourceLang: context?.settings.translationSourceLang,
        targetLang: context?.settings.translationTargetLang
      }
    });
    setIsTranslating(false);

    if (!response.ok || !response.result?.translatedText) {
      setMessage(response.error || "Translation failed.");
      return "";
    }

    const translatedText = response.result.translatedText;
    setTranslation(translatedText);
    setMessage(options.silent ? "Translation added." : `Translated with ${response.result.provider}.`);
    return translatedText;
  }

  function useSelectedWord() {
    const input = expressionRef.current;
    const selectedText = input
      ? expression.slice(input.selectionStart, input.selectionEnd).trim()
      : "";
    const fallback = selectedText || expression.split(/\s+/).find(Boolean) || "";
    const normalized = normalizeWord(fallback);

    if (!normalized) {
      setMessage("Select a word in Expression first.");
      return;
    }

    setWord(normalized);
    setMessage(`Word set: ${normalized}`);
  }

  async function lookupDefinition() {
    const targetWord = word.trim();
    if (!targetWord) {
      setMessage("Set Word before dictionary lookup.");
      return;
    }

    setIsLookingUpWord(true);
    setMessage("Looking up definition...");
    const response = await sendRuntimeMessage<{
      antonyms?: string;
      definition: string;
      example?: string;
      partOfSpeech?: string;
      phonetic?: string;
      provider: string;
      synonyms?: string;
      wordTypes?: string;
    }>({
      type: "lookup-word",
      word: targetWord
    });
    setIsLookingUpWord(false);

    if (!response.ok || !response.result?.definition) {
      setMessage(response.error || "Dictionary lookup failed.");
      return;
    }

    applyDictionaryResult(response.result);
    setMessage(`Definition added from ${response.result.provider}.`);
  }

  async function chooseWordFromExpression(targetWord: string) {
    const normalized = normalizeWord(targetWord);
    if (!normalized) {
      return;
    }

    setWord(normalized);
    setIsLookingUpWord(true);
    setMessage(`Looking up "${normalized}"...`);
    const response = await sendRuntimeMessage<{
      antonyms?: string;
      definition: string;
      example?: string;
      partOfSpeech?: string;
      phonetic?: string;
      provider: string;
      synonyms?: string;
      wordTypes?: string;
    }>({
      type: "lookup-word",
      word: normalized
    });
    setIsLookingUpWord(false);

    if (!response.ok || !response.result?.definition) {
      setMessage(response.error || `Could not auto-fill "${normalized}".`);
      return;
    }

    applyDictionaryResult(response.result);
    setMessage(`Word picked: ${normalized}. Dictionary fields filled.`);
  }

  function applyDictionaryResult(result: {
    antonyms?: string;
    definition: string;
    example?: string;
    partOfSpeech?: string;
    phonetic?: string;
    synonyms?: string;
    wordTypes?: string;
  }) {
    const parts = [
      result.partOfSpeech ? `(${result.partOfSpeech}) ${result.definition}` : result.definition,
      result.example ? `Example: ${result.example}` : ""
    ].filter(Boolean);

    setDefinition(parts.join("\n"));
    setTranscription(result.phonetic || "");
    setWordTypes(result.wordTypes || result.partOfSpeech || "");
    setSynonyms(result.synonyms || "");
    setAntonyms(result.antonyms || "");
  }

  async function warnIfDuplicateExpression() {
    const value = expression.trim();
    if (!value || confirmedDuplicateExpression.current === value) {
      return true;
    }

    const response = await sendRuntimeMessage<{ count: number; noteIds: number[] }>({
      type: "find-duplicate-expression",
      expression: value
    });

    if (!response.ok || !response.result?.count) {
      setDuplicateWarning("");
      return true;
    }

    setDuplicateWarning(`Possible duplicate: ${response.result.count} note(s) already contain this expression. Click Send to Anki again to send anyway.`);
    confirmedDuplicateExpression.current = value;
    setMessage("Possible duplicate found.");
    return false;
  }

  async function makeAnother() {
    await sendRuntimeMessage({ type: "clear-draft" });
    setExpression("");
    setWord("");
    setTranscription("");
    setWordTypes("");
    setTranslation("");
    setDefinition("");
    setExample("");
    setSynonyms("");
    setAntonyms("");
    setSource("");
    setUrl("");
    setDuplicateWarning("");
    setAudioDuration(null);
    setAudioRangeStart(0);
    setAudioRangeEnd(0);
    setWaveformPeaks([]);
    setTrimSuggestion(null);
    setWaveformError("");
    setShowAudioAdvanced(false);
    lastCaptureSignature.current = "";
    lastSavedDraftSignature.current = "";
    lastAutoTranslateSignature.current = "";
    lastAudioRangeFile.current = "";
    lastAutoTrimFile.current = "";
    hasHydratedEditor.current = false;
    await refresh();
  }

  async function openInAnki() {
    const noteId = context?.capture?.noteId;
    if (!noteId) {
      setMessage("No created Anki note is available.");
      return;
    }

    const response = await sendRuntimeMessage({
      type: "open-anki-note",
      noteId
    });
    setMessage(response.ok ? "Opened in Anki." : response.error || "Could not open Anki.");
  }

  async function openSidePanel() {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab?.windowId) {
      setMessage("No active window available for the side panel.");
      return;
    }

    if (typeof chrome.sidePanel?.open !== "function") {
      setMessage("This Chrome version does not support side panel opening.");
      return;
    }

    await chrome.sidePanel.open({ windowId: tab.windowId });
  }

  async function runSmartAction(action: SmartAction) {
    if (action === "capture") {
      await captureSubtitleClip();
      return;
    }

    if (action === "stop-recording") {
      await stopRecording();
      return;
    }

    if (action === "pick-word") {
      expressionRef.current?.focus();
      setMessage("Click a word under Expression to fill dictionary fields.");
      return;
    }

    if (action === "define-word") {
      await lookupDefinition();
      return;
    }

    if (action === "translate") {
      await translateSubtitle();
      return;
    }

    if (action === "fix-audio") {
      setShowAudioAdvanced(true);
      setMessage("Adjust the audio range or re-record the clean range before sending.");
      return;
    }

    if (action === "open-settings") {
      chrome.runtime.openOptionsPage();
      return;
    }

    await createCard();
  }

  async function updateDeck(deckName: string) {
    if (!context) {
      return;
    }

    const settings = await saveAnkiSettings({ deckName });
    setContext({ ...context, settings });
  }

  async function updateModel(modelName: string) {
    if (!context) {
      return;
    }

    const settings = await saveAnkiSettings({ modelName });
    setContext({ ...context, settings });
    await refresh(false);
  }

  async function updateFieldMapping(key: keyof FieldMapping, value: string) {
    if (!context) {
      return;
    }

    const settings = await saveAnkiSettings({
      fieldMapping: {
        ...context.settings.fieldMapping,
        [key]: value
      }
    });
    setContext({ ...context, settings });
  }

  const capture = context?.capture;
  const draft = { expression, word, transcription, wordTypes, translation, definition, example, synonyms, antonyms, source, url };
  const ready = canSendToAnki(context, expression);
  const isRecording = Boolean(context?.isRecording);
  const canStopRecording = context?.sessionMode === "clip" || context?.sessionMode === "range";
  const effectiveAudioDuration = getEffectiveAudioDuration(capture, audioDuration);
  const cardQuality = buildCardQuality({
    context,
    draft,
    duplicateWarning,
    effectiveAudioDuration,
    isRecording
  });
  const audioRangeStartMin = getAudioRangeStartMin(capture);
  const normalizedAudioRangeEnd = effectiveAudioDuration
    ? Math.min(audioRangeEnd || effectiveAudioDuration, effectiveAudioDuration)
    : audioRangeEnd;

  if (mode === "popup") {
    return (
      <main className="quick-panel">
        <header className="quick-header">
          <div>
            <p className="eyebrow">InOriginal</p>
            <h1>Capture</h1>
          </div>
          <StatusPill status={flowStatus} />
        </header>

        <p className="subtitle subtitle--current quick-subtitle">{capture?.subtitle || "No current subtitle yet."}</p>

        {capture?.audio?.dataUrl && (
          <audio className="media-preview media-preview--audio" controls src={capture.audio.dataUrl} onLoadedMetadata={(event) => {
            const duration = event.currentTarget.duration;
            setAudioDuration(Number.isFinite(duration) ? duration : null);
          }} />
        )}

        <div className="quick-actions">
          <button className="primary-action" disabled={isRecording} onClick={captureSubtitleClip}>Capture current subtitle</button>
          {canStopRecording && <button className="secondary" onClick={stopRecording}>Stop recording</button>}
          {isRecording && <button className="secondary" onClick={cancelCapture}>Cancel capture</button>}
          <button className="secondary" onClick={openSidePanel}>Open review panel</button>
          <button className="secondary" disabled={cardQuality.disabled} onClick={() => runSmartAction(cardQuality.nextAction)}>{cardQuality.cta}</button>
        </div>

        <p className="status">{effectiveAudioDuration ? `Audio: ${effectiveAudioDuration.toFixed(1)}s | ${message}` : message}</p>
      </main>
    );
  }

  return (
    <main className="studio-shell">
      <header className="studio-topbar">
        <div>
          <p className="eyebrow">Subtitle Studio</p>
          <h1>InOriginal Capture</h1>
        </div>
        <div className="hero-actions">
          <StatusPill status={flowStatus} />
          <button className="primary-action" disabled={isRecording} onClick={captureSubtitleClip}>Capture</button>
          {canStopRecording && <button className="secondary ghost-button" onClick={stopRecording}>Stop recording</button>}
          {isRecording && <button className="secondary ghost-button" onClick={cancelCapture}>Cancel</button>}
          <button className="secondary ghost-button" onClick={() => chrome.runtime.openOptionsPage()}>Settings</button>
        </div>
      </header>

      <section className="studio-grid">
        <section className="review-pane">
          <div className="section-head">
            <h2>Review</h2>
            <p className="status">{message}</p>
          </div>
          <p className="subtitle subtitle--muted subtitle--context">{capture?.previousSubtitle || "No previous subtitle."}</p>
          <p className="subtitle subtitle--current subtitle--hero">{capture?.subtitle || "No current subtitle yet."}</p>
          <p className="subtitle subtitle--muted subtitle--context">{capture?.nextSubtitle || "No next subtitle."}</p>
          <CaptureTimeline capture={capture} />
        </section>

        <section className="media-pane">
          <article className="media-tile">
            <div className="section-head">
              <h2>Screenshot</h2>
              <button className="secondary inline-action" disabled={isRecording} onClick={retakeScreenshot}>Retake screenshot</button>
            </div>
            {capture?.screenshot?.dataUrl ? (
              <img className="media-preview media-preview--image" alt="Screenshot preview" src={capture.screenshot.dataUrl} />
            ) : (
              <p className="muted media-empty">No screenshot yet.</p>
            )}
          </article>

          <article className="media-tile">
            <div className="section-head">
              <h2>Audio clip</h2>
              <button className="secondary inline-action" disabled={isRecording} onClick={recaptureAudio}>Re-record from subtitle</button>
            </div>
            {capture?.audio?.dataUrl ? (
              <>
                <audio className="media-preview media-preview--audio" controls src={capture.audio.dataUrl} onLoadedMetadata={(event) => {
                  const duration = event.currentTarget.duration;
                  const nextDuration = Number.isFinite(duration) ? duration : null;
                  setAudioDuration(nextDuration);
                }} />
                <p className="status">{formatAudioStatus(capture, effectiveAudioDuration)}</p>
                {effectiveAudioDuration && (
                  <div className="range-editor">
                    <div>
                      <h3>{trimSuggestion ? "Clean range ready" : isAnalyzingAudio ? "Finding clean range..." : "Clean range"}</h3>
                      <p className="muted">{buildAudioGuidance(trimSuggestion, isAnalyzingAudio)}</p>
                    </div>
                    <WaveformPreview
                      duration={effectiveAudioDuration}
                      peaks={waveformPeaks}
                      rangeEnd={normalizedAudioRangeEnd}
                      rangeStart={audioRangeStart}
                      trimSuggestion={trimSuggestion}
                    />
                    <div className="range-editor__actions">
                      <button
                        className="primary-action inline-action"
                        disabled={isRecording || capture.audio.videoStartTime === undefined}
                        onClick={recaptureSelectedAudioRange}
                      >
                        Re-record clean range
                      </button>
                      <button className="secondary inline-action" onClick={() => setShowAudioAdvanced(!showAudioAdvanced)}>
                        {showAudioAdvanced ? "Hide manual controls" : "Adjust manually"}
                      </button>
                    </div>
                    {waveformError && <p className="muted media-empty">{waveformError}</p>}
                    {showAudioAdvanced && (
                      <div className="range-editor__advanced">
                        <div className="range-editor__labels">
                          <span>Start: {audioRangeStart.toFixed(1)}s</span>
                          <span>End: {normalizedAudioRangeEnd.toFixed(1)}s</span>
                        </div>
                        <input
                          aria-label="Audio range start"
                          max={Math.max(0, normalizedAudioRangeEnd - 0.1)}
                          min={audioRangeStartMin}
                          step={0.1}
                          type="range"
                          value={audioRangeStart}
                          onChange={(event) => {
                            const nextStart = Math.min(Number(event.target.value), normalizedAudioRangeEnd - 0.1);
                            setAudioRangeStart(Math.max(audioRangeStartMin, nextStart));
                          }}
                        />
                        <input
                          aria-label="Audio range end"
                          max={effectiveAudioDuration}
                          min={Math.min(effectiveAudioDuration, audioRangeStart + 0.1)}
                          step={0.1}
                          type="range"
                          value={normalizedAudioRangeEnd}
                          onChange={(event) => setAudioRangeEnd(Math.max(Number(event.target.value), audioRangeStart + 0.1))}
                        />
                        <div className="range-editor__actions">
                          <button className="secondary inline-action" disabled={!trimSuggestion || isAnalyzingAudio} onClick={applyTrimSuggestion}>
                            Apply auto-trim
                          </button>
                          <button className="secondary inline-action" disabled={isRecording} onClick={resetAudioRange}>Reset full clip</button>
                        </div>
                      </div>
                    )}
                    {capture.audio.videoStartTime === undefined && (
                      <p className="muted media-empty">This old clip has no video timestamp. Press Capture again once, then this button will seek the video automatically.</p>
                    )}
                  </div>
                )}
              </>
            ) : (
              <p className="muted media-empty">No audio yet.</p>
            )}
          </article>
        </section>
      </section>

      <section className="anki-pane">
        <div className="section-head">
          <h2>Anki</h2>
          <Checklist context={context} expression={expression} translation={translation} />
        </div>
        <div className="compact-toolbar">
          <label>
            <span>Deck</span>
            <select value={context?.settings.deckName || ""} onChange={(event) => updateDeck(event.target.value)}>
              {renderOptions(context?.choices.deckNames, context?.settings.deckName)}
            </select>
          </label>
          <label>
            <span>Note type</span>
            <select value={context?.settings.modelName || ""} onChange={(event) => updateModel(event.target.value)}>
              {renderOptions(context?.choices.modelNames, context?.settings.modelName)}
            </select>
          </label>
        </div>

        <details className="mapping-panel">
          <summary>Bind Anki template fields</summary>
          <div className="mapping-grid">
            <FieldMappingSelect label="Expression" fieldKey="expression" context={context} allowEmpty={false} onChange={updateFieldMapping} />
            <FieldMappingSelect label="Word" fieldKey="word" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Translation" fieldKey="translation" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Definition" fieldKey="definition" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Transcription" fieldKey="transcription" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Word Types" fieldKey="wordTypes" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Mnemonic" fieldKey="mnemonic" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Example" fieldKey="example" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Synonyms" fieldKey="synonyms" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Antonyms" fieldKey="antonyms" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Source" fieldKey="source" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Url" fieldKey="url" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Image" fieldKey="image" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Audio" fieldKey="audio" context={context} allowEmpty onChange={updateFieldMapping} />
          </div>
          <p className="muted">Fields are loaded from the selected note type.</p>
        </details>

        <section className="editor-grid">
          <label className="editor-card">
            <span>Expression</span>
            <textarea ref={expressionRef} rows={3} value={expression} onChange={(event) => {
              setExpression(event.target.value);
              setDuplicateWarning("");
            }} />
            <WordPicker expression={expression} isLoading={isLookingUpWord} selectedWord={word} onPick={chooseWordFromExpression} />
          </label>
          <label className="editor-card">
            <span>Word</span>
            <input value={word} onChange={(event) => setWord(event.target.value)} placeholder="Optional target word" />
            <div className="field-actions">
              <button type="button" className="secondary inline-action" onClick={useSelectedWord}>Use selected word</button>
              <button type="button" className="secondary inline-action" disabled={isLookingUpWord || !word.trim()} onClick={lookupDefinition}>
                {isLookingUpWord ? "Looking up..." : "Define word"}
              </button>
            </div>
          </label>
          <label className="editor-card">
            <span>Transcription</span>
            <input value={transcription} onChange={(event) => setTranscription(event.target.value)} placeholder="Auto-filled pronunciation" />
          </label>
          <label className="editor-card">
            <span>Word Types</span>
            <input value={wordTypes} onChange={(event) => setWordTypes(event.target.value)} placeholder="noun, verb, adjective..." />
          </label>
          <label className="editor-card">
            <span>Translation</span>
            <textarea rows={3} value={translation} onChange={(event) => setTranslation(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Definition</span>
            <textarea rows={3} value={definition} onChange={(event) => setDefinition(event.target.value)} />
          </label>
          <label className="editor-card editor-card--wide">
            <span>Example / Context</span>
            <textarea rows={5} value={example} onChange={(event) => setExample(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Synonyms</span>
            <input value={synonyms} onChange={(event) => setSynonyms(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Antonyms</span>
            <input value={antonyms} onChange={(event) => setAntonyms(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Source</span>
            <input value={source} onChange={(event) => setSource(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Url</span>
            <input value={url} onChange={(event) => setUrl(event.target.value)} />
          </label>
        </section>

        <section className="translator-panel">
          <div>
            <h2>Translator</h2>
            <p className="muted">
              {formatTranslationMode(context?.settings.translationMode)} | {context?.settings.translationSourceLang || "en"} to {context?.settings.translationTargetLang || "ru"}
            </p>
          </div>
          <button className="secondary inline-action" disabled={isTranslating || !expression.trim()} onClick={() => translateSubtitle()}>
            {isTranslating ? "Translating..." : "Translate subtitle"}
          </button>
          <button className="secondary inline-action" disabled={!translation} onClick={() => setTranslation("")}>Clear translation</button>
          {translation && <p className="translation-preview">{translation}</p>}
        </section>

        <CardQualityPanel quality={cardQuality} />
        <AnkiFieldPreview context={context} draft={draft} />
        {duplicateWarning && <p className="warning-banner">{duplicateWarning}</p>}

        <footer className="footer-bar">
          {flowStatus === "Created" ? (
            <div className="created-actions">
              <span className="created-label">Card created</span>
              <button className="secondary" onClick={makeAnother}>Make another</button>
              <button className="secondary" onClick={openInAnki}>Open in Anki</button>
            </div>
          ) : (
            <>
              <p className="muted footer-copy">{cardQuality.footerCopy}</p>
              <button className="primary-action" disabled={cardQuality.disabled} onClick={() => runSmartAction(cardQuality.nextAction)}>{cardQuality.cta}</button>
            </>
          )}
        </footer>
      </section>

      <section className="history-pane">
        <h2>Recent cards</h2>
        {(context?.cardHistory || []).length === 0 ? (
          <p className="muted">No cards created yet.</p>
        ) : (
          <div className="history-list">
            {(context?.cardHistory || []).map((item) => (
              <button key={`${item.noteId}-${item.createdAt}`} className="history-item" onClick={() => sendRuntimeMessage({ type: "open-anki-note", noteId: item.noteId })}>
                <span>{item.subtitle}</span>
                <small>{new Date(item.createdAt).toLocaleTimeString()}</small>
              </button>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}

function CaptureTimeline({ capture }: { capture?: CaptureData }) {
  const events = capture?.captureEvents || [];
  if (events.length === 0) {
    return null;
  }

  return (
    <details className="capture-debug">
      <summary>Capture details</summary>
      <ol>
        {events.map((event) => (
          <li key={`${event.at}-${event.step}-${event.message}`} className={`capture-debug__event capture-debug__event--${event.level}`}>
            <span>{event.step}</span>
            <p>{event.message}</p>
            <small>{new Date(event.at).toLocaleTimeString()}</small>
          </li>
        ))}
      </ol>
    </details>
  );
}

function StatusPill({ status }: { status: FlowStatus }) {
  return <span className={`status-pill status-pill--${status.toLowerCase().replaceAll(" ", "-")}`}>{status}</span>;
}

function WordPicker({
  expression,
  isLoading,
  onPick,
  selectedWord
}: {
  expression: string;
  isLoading: boolean;
  onPick: (word: string) => void;
  selectedWord: string;
}) {
  const tokens = tokenizeExpression(expression);
  if (tokens.length === 0) {
    return <p className="muted word-picker__hint">Capture or type an expression, then click a word to auto-fill dictionary fields.</p>;
  }

  return (
    <div className="word-picker" aria-label="Clickable words from expression">
      <p className="word-picker__hint">Click a word to fill Word, Definition, Transcription, Word Types, Synonyms, and Antonyms.</p>
      <div className="word-picker__tokens">
        {tokens.map((token, index) => token.isWord ? (
          <button
            className={normalizeWord(token.value).toLowerCase() === selectedWord.toLowerCase() ? "word-token word-token--selected" : "word-token"}
            disabled={isLoading}
            key={`${token.value}-${index}`}
            onClick={() => onPick(token.value)}
            type="button"
          >
            {token.value}
          </button>
        ) : (
          <span className="word-token__punctuation" key={`${token.value}-${index}`}>{token.value}</span>
        ))}
      </div>
    </div>
  );
}

function WaveformPreview({
  duration,
  peaks,
  rangeEnd,
  rangeStart,
  trimSuggestion
}: {
  duration: number;
  peaks: number[];
  rangeEnd: number;
  rangeStart: number;
  trimSuggestion: TrimSuggestion | null;
}) {
  if (peaks.length === 0) {
    return <div className="waveform waveform--empty"><span>Waveform is loading...</span></div>;
  }

  const visibleStart = Math.max(0, rangeStart);
  const visibleEnd = Math.min(duration, rangeEnd);
  return (
    <div className="waveform" aria-label="Audio waveform preview">
      {peaks.map((peak, index) => {
        const pointTime = duration * ((index + 0.5) / peaks.length);
        const isSelected = pointTime >= visibleStart && pointTime <= visibleEnd;
        const isSuggested = Boolean(trimSuggestion && pointTime >= trimSuggestion.start && pointTime <= trimSuggestion.end);
        return (
          <span
            className={[
              "waveform__bar",
              isSuggested ? "waveform__bar--suggested" : "",
              isSelected ? "waveform__bar--selected" : ""
            ].filter(Boolean).join(" ")}
            key={`${index}-${peak.toFixed(3)}`}
            style={{ height: `${Math.max(8, Math.round(peak * 100))}%` }}
          />
        );
      })}
    </div>
  );
}

function Checklist({ context, expression, translation }: { context: PopupContext | null; expression: string; translation: string }) {
  const capture = context?.capture;
  const items = [
    ["Expression", Boolean(capture?.subtitle || expression)],
    ["Translation", Boolean(translation) || context?.settings.translationMode === "manual"],
    ["Screenshot", Boolean(capture?.screenshot?.dataUrl)],
    ["Audio", Boolean(capture?.audio?.dataUrl)],
    ["Deck", Boolean(context?.settings.deckName)],
    ["Note type", Boolean(context?.settings.modelName)],
    ["Mapping", Boolean(context?.settings.fieldMapping.expression)]
  ] as const;

  return (
    <div className="checklist">
      {items.map(([label, done]) => (
        <span key={label} className={done ? "checklist-item checklist-item--done" : "checklist-item"}>{label}</span>
      ))}
    </div>
  );
}

function CardQualityPanel({ quality }: { quality: CardQuality }) {
  return (
    <section className={`quality-panel quality-panel--${quality.status.toLowerCase().replace(" ", "-")}`}>
      <div className="quality-panel__head">
        <div>
          <p className="eyebrow">Smart Send</p>
          <h2>Card quality</h2>
        </div>
        <div className="quality-score" aria-label={`Card quality score ${quality.score}`}>
          <strong>{quality.score}</strong>
          <span>{quality.status}</span>
        </div>
      </div>
      <div className="quality-list">
        {quality.items.map((item) => (
          <div
            className={[
              "quality-item",
              item.done ? "quality-item--done" : "",
              `quality-item--${item.tone}`
            ].filter(Boolean).join(" ")}
            key={`${item.tone}-${item.label}`}
          >
            <span>{item.done ? "Ready" : item.tone === "risk" ? "Check" : "Missing"}</span>
            <strong>{item.label}</strong>
            <p>{item.detail}</p>
          </div>
        ))}
      </div>
    </section>
  );
}

function AnkiFieldPreview({ context, draft }: { context: PopupContext | null; draft: SentenceDraft }) {
  const mapping = context?.settings.fieldMapping;
  if (!mapping) {
    return null;
  }

  const rows = [
    ["Expression", mapping.expression, draft.expression],
    ["Word", mapping.word, draft.word],
    ["Transcription", mapping.transcription, draft.transcription],
    ["Word Types", mapping.wordTypes, draft.wordTypes],
    ["Translation", mapping.translation, draft.translation],
    ["Definition", mapping.definition, draft.definition],
    ["Example", mapping.example, draft.example],
    ["Synonyms", mapping.synonyms, draft.synonyms],
    ["Antonyms", mapping.antonyms, draft.antonyms],
    ["Source", mapping.source, draft.source],
    ["Url", mapping.url, draft.url],
    ["Image", mapping.image, context?.capture?.screenshot?.dataUrl ? "Screenshot media" : ""],
    ["Audio", mapping.audio, context?.capture?.audio?.dataUrl ? "Audio media" : ""]
  ].filter(([, fieldName]) => Boolean(fieldName));

  if (rows.length === 0) {
    return null;
  }

  return (
    <details className="anki-preview">
      <summary>Anki field preview</summary>
      <div className="anki-preview__grid">
        {rows.map(([label, fieldName, value]) => (
          <div className="anki-preview__row" key={`${label}-${fieldName}`}>
            <span>{label} {"->"} {fieldName}</span>
            <p>{value || "Empty"}</p>
          </div>
        ))}
      </div>
    </details>
  );
}

function FieldMappingSelect({
  allowEmpty,
  context,
  fieldKey,
  label,
  onChange
}: {
  allowEmpty?: boolean;
  context: PopupContext | null;
  fieldKey: keyof FieldMapping;
  label: string;
  onChange: (key: keyof FieldMapping, value: string) => void;
}) {
  const value = context?.settings.fieldMapping[fieldKey] || "";
  return (
    <label>
      <span>{label}</span>
      <select value={value} onChange={(event) => onChange(fieldKey, event.target.value)}>
        {allowEmpty && <option value="">Not used</option>}
        {renderOptions(context?.choices.modelFieldNames, value)}
      </select>
    </label>
  );
}

function renderOptions(values: string[] = [], selectedValue = "") {
  const options = values.includes(selectedValue) || !selectedValue
    ? values
    : [...values, selectedValue];

  return options.map((value) => (
    <option key={value} value={value}>{value}</option>
  ));
}

function formatTranslationMode(mode?: string) {
  if (mode === "before-send") {
    return "Auto before Send";
  }
  if (mode === "manual") {
    return "Manual";
  }
  return "Auto after Capture";
}

function buildCardQuality({
  context,
  draft,
  duplicateWarning,
  effectiveAudioDuration,
  isRecording
}: {
  context: PopupContext | null;
  draft: SentenceDraft;
  duplicateWarning: string;
  effectiveAudioDuration: number | null;
  isRecording: boolean;
}): CardQuality {
  const capture = context?.capture;
  const hasExpression = Boolean(draft.expression.trim() || capture?.subtitle);
  const hasScreenshot = Boolean(capture?.screenshot?.dataUrl);
  const hasAudio = Boolean(capture?.audio?.dataUrl);
  const hasDeck = Boolean(context?.settings.deckName);
  const hasModel = Boolean(context?.settings.modelName);
  const hasExpressionMapping = Boolean(context?.settings.fieldMapping.expression);
  const hasTranslation = Boolean(draft.translation.trim()) || context?.settings.translationMode === "manual";
  const hasWord = Boolean(draft.word.trim());
  const hasDefinition = Boolean(draft.definition.trim());
  const audioTooLong = Boolean(effectiveAudioDuration && effectiveAudioDuration > 8.5);
  const hasDuplicateWarning = Boolean(duplicateWarning);

  const required: QualityItem[] = [
    { label: "Expression", done: hasExpression, detail: "Subtitle or edited sentence is required.", tone: "required" },
    { label: "Screenshot", done: hasScreenshot, detail: "Image context should be attached to the card.", tone: "required" },
    { label: "Audio", done: hasAudio, detail: "Audio clip should be attached before sending.", tone: "required" },
    { label: "Deck", done: hasDeck, detail: "Choose where Anki will save the card.", tone: "required" },
    { label: "Note type", done: hasModel, detail: "Choose the template used by Anki.", tone: "required" },
    { label: "Expression field", done: hasExpressionMapping, detail: "Bind the sentence to a real Anki field.", tone: "required" }
  ];

  const recommended: QualityItem[] = [
    { label: "Target word", done: hasWord, detail: "Pick the mined word so the card has a clear learning target.", tone: "recommended" },
    { label: "Definition", done: hasDefinition, detail: "Dictionary data makes the Back side useful for review.", tone: "recommended" },
    { label: "Translation", done: hasTranslation, detail: "Translation helps quick comprehension during reviews.", tone: "recommended" }
  ];

  const risks: QualityItem[] = [
    { label: "Audio length", done: !audioTooLong, detail: audioTooLong ? "Clip is long. Trim or re-record the clean range." : "Clip length looks focused.", tone: "risk" },
    { label: "Duplicate", done: !hasDuplicateWarning, detail: hasDuplicateWarning ? "This expression may already exist in Anki." : "No duplicate warning for this draft.", tone: "risk" }
  ];

  const requiredMissing = required.filter((item) => !item.done);
  const recommendedMissing = recommended.filter((item) => !item.done);
  const activeRisks = risks.filter((item) => !item.done);
  const positiveItems = [...required, ...recommended];
  const baseScore = Math.round((positiveItems.filter((item) => item.done).length / positiveItems.length) * 100);
  const score = Math.max(0, Math.min(100, baseScore - activeRisks.length * 8));
  const status = requiredMissing.length > 0 ? "Blocked" : recommendedMissing.length > 0 || activeRisks.length > 0 ? "Needs review" : "Ready";

  if (isRecording) {
    return {
      cta: "Stop recording",
      disabled: false,
      footerCopy: "Recording is still running. Stop it to review the final clip.",
      items: [...required, ...recommended, ...risks],
      nextAction: "stop-recording",
      score,
      status
    };
  }

  let nextAction: SmartAction = "send";
  let cta = hasDuplicateWarning ? "Send anyway" : "Send to Anki";
  let footerCopy = status === "Ready"
    ? "Looks ready. Send the final card to Anki."
    : "Smart Send found the next best fix before sending.";

  if (!hasExpression || !hasScreenshot || !hasAudio) {
    nextAction = "capture";
    cta = "Capture";
    footerCopy = "Capture will collect subtitle, screenshot, and audio first.";
  } else if (!hasDeck || !hasModel || !hasExpressionMapping) {
    nextAction = "open-settings";
    cta = "Open settings";
    footerCopy = "Finish deck, note type, and field mapping before sending.";
  } else if (!hasWord) {
    nextAction = "pick-word";
    cta = "Pick a word";
    footerCopy = "Choose the target word to turn this into a stronger sentence-mining card.";
  } else if (!hasDefinition) {
    nextAction = "define-word";
    cta = "Define word";
    footerCopy = "Add dictionary data before sending.";
  } else if (!hasTranslation) {
    nextAction = "translate";
    cta = "Translate";
    footerCopy = "Add translation before sending, or switch translation mode to Manual.";
  } else if (audioTooLong) {
    nextAction = "fix-audio";
    cta = "Fix audio";
    footerCopy = "The clip looks long. Trim or re-record the clean range.";
  }

  return {
    cta,
    disabled: false,
    footerCopy,
    items: [...required, ...recommended, ...risks],
    nextAction,
    score,
    status
  };
}

function buildAudioGuidance(trimSuggestion: TrimSuggestion | null, isAnalyzingAudio: boolean) {
  if (isAnalyzingAudio) {
    return "Analyzing silence and speech in the clip.";
  }

  if (trimSuggestion) {
    return `Suggested clean range: ${trimSuggestion.start.toFixed(1)}s to ${trimSuggestion.end.toFixed(1)}s. Preview it, then re-record only that part if the clip needs cleanup.`;
  }

  return "Preview the clip. If it has extra silence or missed speech, adjust manually or re-record from the subtitle.";
}

function getEffectiveAudioDuration(capture: CaptureData | undefined, metadataDuration: number | null) {
  if (metadataDuration && Number.isFinite(metadataDuration)) {
    return metadataDuration;
  }

  const durationFromCapture = capture?.audio?.durationMs ? capture.audio.durationMs / 1000 : null;
  if (durationFromCapture && Number.isFinite(durationFromCapture)) {
    return durationFromCapture;
  }

  const videoStart = capture?.audio?.videoStartTime;
  const videoEnd = capture?.audio?.videoEndTime;
  if (videoStart !== undefined && videoEnd !== undefined && videoEnd > videoStart) {
    return videoEnd - videoStart;
  }

  return null;
}

function getAudioRangeStartMin(capture: CaptureData | undefined) {
  const videoStartTime = capture?.audio?.videoStartTime;
  if (videoStartTime === undefined) {
    return 0;
  }

  return -Math.min(5, videoStartTime);
}

async function analyzeAudioDataUrl(dataUrl: string): Promise<{
  duration: number;
  peaks: number[];
  trim: TrimSuggestion | null;
}> {
  const response = await fetch(dataUrl);
  const audioData = await response.arrayBuffer();
  const AudioContextConstructor = window.AudioContext
    || (window as typeof window & { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
  if (!AudioContextConstructor) {
    throw new Error("This browser cannot analyze audio waveforms.");
  }

  const audioContext = new AudioContextConstructor();
  try {
    const audioBuffer = await audioContext.decodeAudioData(audioData.slice(0));
    const channel = audioBuffer.getChannelData(0);
    const duration = audioBuffer.duration;
    const peaks = buildWaveformPeaks(channel, 96);
    const trim = detectSpeechRange(channel, audioBuffer.sampleRate, duration);
    return { duration, peaks, trim };
  } finally {
    await audioContext.close().catch(() => {});
  }
}

function buildWaveformPeaks(channel: Float32Array, segmentCount: number) {
  const segmentSize = Math.max(1, Math.floor(channel.length / segmentCount));
  const peaks = Array.from({ length: segmentCount }, (_, index) => {
    const start = index * segmentSize;
    const end = Math.min(channel.length, start + segmentSize);
    let max = 0;

    for (let cursor = start; cursor < end; cursor += 1) {
      max = Math.max(max, Math.abs(channel[cursor]));
    }

    return max;
  });
  const maxPeak = Math.max(...peaks, 0.001);
  return peaks.map((peak) => peak / maxPeak);
}

function detectSpeechRange(channel: Float32Array, sampleRate: number, duration: number): TrimSuggestion | null {
  const frameSize = Math.max(1, Math.floor(sampleRate * 0.05));
  const frameCount = Math.ceil(channel.length / frameSize);
  const rmsFrames = Array.from({ length: frameCount }, (_, frameIndex) => {
    const start = frameIndex * frameSize;
    const end = Math.min(channel.length, start + frameSize);
    let sum = 0;

    for (let cursor = start; cursor < end; cursor += 1) {
      sum += channel[cursor] * channel[cursor];
    }

    return Math.sqrt(sum / Math.max(1, end - start));
  });

  const maxRms = Math.max(...rmsFrames, 0);
  if (maxRms <= 0.003) {
    return null;
  }

  const threshold = Math.max(0.01, maxRms * 0.12);
  const firstSpeechFrame = rmsFrames.findIndex((value) => value >= threshold);
  const lastSpeechFrame = rmsFrames.length - 1 - [...rmsFrames].reverse().findIndex((value) => value >= threshold);

  if (firstSpeechFrame < 0 || lastSpeechFrame < firstSpeechFrame) {
    return null;
  }

  const padSeconds = 0.12;
  const start = Math.max(0, firstSpeechFrame * 0.05 - padSeconds);
  const end = Math.min(duration, (lastSpeechFrame + 1) * 0.05 + padSeconds);
  if (end - start < 0.25) {
    return null;
  }

  return {
    start: Number(start.toFixed(2)),
    end: Number(end.toFixed(2))
  };
}

function formatAudioStatus(capture: CaptureData, audioDuration: number | null) {
  const parts = [audioDuration ? `Audio: ${audioDuration.toFixed(1)}s` : "Audio ready"];
  if (capture.audio?.stopReason === "max-duration") {
    parts.push("stopped at max length");
  }
  if (capture.audio?.stopReason === "manual") {
    parts.push("stopped manually");
  }
  if (capture.audio?.stopReason === "range") {
    parts.push("selected range");
  }
  return parts.join(" | ");
}

function canSendToAnki(context: PopupContext | null, expression: string) {
  const capture = context?.capture;
  return Boolean(
    (capture?.subtitle || expression)
    && capture?.screenshot?.dataUrl
    && capture?.audio?.dataUrl
    && context?.settings.deckName
    && context?.settings.modelName
    && context?.settings.fieldMapping.expression
    && !context?.isRecording
  );
}

function buildExampleText(capture?: CaptureData) {
  if (!capture) {
    return "";
  }

  return [
    capture.previousSubtitle ? `Previous: ${capture.previousSubtitle}` : "",
    capture.subtitle ? `Current: ${capture.subtitle}` : "",
    capture.nextSubtitle ? `Next: ${capture.nextSubtitle}` : ""
  ].filter(Boolean).join("\n");
}

function buildSentenceDraft(value: Partial<SentenceDraft> = {}): SentenceDraft {
  return {
    expression: value.expression || "",
    word: value.word || "",
    transcription: value.transcription || "",
    wordTypes: value.wordTypes || "",
    translation: value.translation || "",
    definition: value.definition || "",
    example: value.example || "",
    synonyms: value.synonyms || "",
    antonyms: value.antonyms || "",
    source: value.source || "",
    url: value.url || ""
  };
}

function buildDraftSignature(draft: SentenceDraft) {
  return [
    draft.expression,
    draft.word,
    draft.transcription,
    draft.wordTypes,
    draft.translation,
    draft.definition,
    draft.example,
    draft.synonyms,
    draft.antonyms,
    draft.source,
    draft.url
  ].join("|");
}

function normalizeWord(value: string) {
  return value.replace(/^[^\p{L}\p{N}'-]+|[^\p{L}\p{N}'-]+$/gu, "");
}

function tokenizeExpression(value: string) {
  const matches = value.match(/[\p{L}\p{N}'-]+|[^\p{L}\p{N}'-]+/gu) || [];
  return matches
    .map((token) => ({
      isWord: Boolean(normalizeWord(token)),
      value: token
    }))
    .filter((token) => token.value.trim() || token.isWord);
}

function buildCaptureSignature(capture?: CaptureData) {
  if (!capture) {
    return "";
  }

  return [
    capture.subtitle || "",
    capture.previousSubtitle || "",
    capture.nextSubtitle || "",
    capture.screenshot?.filename || "",
    capture.audio?.filename || "",
    capture.pageUrl || "",
    capture.cardState || "",
    capture.captureStep || "",
    capture.error || "",
    capture.noteId || ""
  ].join("|");
}

function deriveFlowStatus(context: PopupContext): FlowStatus {
  if (context.isRecording && context.sessionMode === "clip-waiting") {
    return "Rewinding";
  }
  if (context.isRecording && context.sessionMode === "clip") {
    return "Recording subtitle audio";
  }
  if (context.capture?.cardState === "created") {
    return "Created";
  }
  if (context.capture?.captureStep === "failed") {
    return "Failed";
  }
  if (context.capture?.captureStep === "cancelled") {
    return "Cancelled";
  }
  if (context.capture?.error) {
    return "Ready to review";
  }
  if (context.capture?.subtitle && context.capture?.screenshot?.dataUrl && context.capture?.audio?.dataUrl) {
    return "Ready to review";
  }
  return "Idle";
}

function buildMessage(context: PopupContext) {
  if (context.isRecording && context.sessionMode === "clip-waiting") {
    return "Rewinding to catch the start of the subtitle.";
  }
  if (context.isRecording && context.sessionMode === "clip") {
    return "Recording subtitle audio.";
  }
  if (context.capture?.cardState === "created") {
    return `Card created${context.capture.noteId ? `: ${context.capture.noteId}` : ""}.`;
  }
  if (context.capture?.captureStep === "failed" && context.capture.error) {
    return context.capture.error;
  }
  if (context.capture?.captureStep === "cancelled") {
    return "Capture cancelled. You can capture again or keep editing the draft.";
  }
  if (context.capture?.error) {
    return context.capture.error;
  }
  if (context.capture?.subtitle && context.capture?.screenshot?.dataUrl && context.capture?.audio?.dataUrl) {
    return "Ready to review.";
  }
  return "Capture a subtitle to start a draft.";
}
