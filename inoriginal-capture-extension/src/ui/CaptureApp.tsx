import { useEffect, useRef, useState } from "react";
import { getPopupContext, saveAnkiSettings, sendRuntimeMessage } from "../shared/chromeApi";
import type { CaptureData, FieldMapping, PopupContext } from "../shared/types";
import "./styles.css";

type CaptureAppProps = {
  mode: "popup" | "sidepanel";
};

type FlowStatus = "Idle" | "Rewinding" | "Recording subtitle audio" | "Ready to review" | "Sending to Anki" | "Created" | "Failed" | "Cancelled";
type SentenceDraft = {
  expression: string;
  word: string;
  translation: string;
  definition: string;
  example: string;
  source: string;
  url: string;
};

export function CaptureApp({ mode }: CaptureAppProps) {
  const [context, setContext] = useState<PopupContext | null>(null);
  const [flowStatus, setFlowStatus] = useState<FlowStatus>("Idle");
  const [message, setMessage] = useState("Loading...");
  const [expression, setExpression] = useState("");
  const [word, setWord] = useState("");
  const [translation, setTranslation] = useState("");
  const [definition, setDefinition] = useState("");
  const [example, setExample] = useState("");
  const [source, setSource] = useState("");
  const [url, setUrl] = useState("");
  const [isTranslating, setIsTranslating] = useState(false);
  const [isLookingUpWord, setIsLookingUpWord] = useState(false);
  const [duplicateWarning, setDuplicateWarning] = useState("");
  const [audioDuration, setAudioDuration] = useState<number | null>(null);
  const lastCaptureSignature = useRef("");
  const lastAutoTranslateSignature = useRef("");
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

  async function refresh(showLoading = true) {
    if (showLoading) {
      setMessage("Loading...");
    }

    try {
      const nextContext = await getPopupContext();
      setContext(nextContext);
      hydrateEditor(nextContext.capture);
      setFlowStatus(deriveFlowStatus(nextContext));
      setMessage(buildMessage(nextContext));
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Failed to load extension UI.");
    }
  }

  function hydrateEditor(capture?: CaptureData) {
    const signature = buildCaptureSignature(capture);
    if (signature === lastCaptureSignature.current) {
      return;
    }

    setExpression(capture?.subtitle || "");
    setWord("");
    setTranslation("");
    setDefinition("");
    setExample(buildExampleText(capture));
    setSource(capture?.pageTitle || "");
    setUrl(capture?.pageUrl || "");
    setDuplicateWarning("");
    setAudioDuration(null);
    lastCaptureSignature.current = signature;
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
        subtitle: context?.capture?.subtitle || expression,
        draft: {
          expression,
          word,
          translation: finalTranslation,
          definition,
          example,
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
    const normalized = fallback.replace(/^[^\p{L}\p{N}]+|[^\p{L}\p{N}]+$/gu, "");

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
      definition: string;
      example?: string;
      partOfSpeech?: string;
      phonetic?: string;
      provider: string;
    }>({
      type: "lookup-word",
      word: targetWord
    });
    setIsLookingUpWord(false);

    if (!response.ok || !response.result?.definition) {
      setMessage(response.error || "Dictionary lookup failed.");
      return;
    }

    const parts = [
      response.result.partOfSpeech ? `(${response.result.partOfSpeech}) ${response.result.definition}` : response.result.definition,
      response.result.phonetic ? `Pronunciation: ${response.result.phonetic}` : "",
      response.result.example ? `Example: ${response.result.example}` : ""
    ].filter(Boolean);
    setDefinition(parts.join("\n"));
    setMessage(`Definition added from ${response.result.provider}.`);
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
    setTranslation("");
    setDefinition("");
    setExample("");
    setSource("");
    setUrl("");
    setDuplicateWarning("");
    setAudioDuration(null);
    lastCaptureSignature.current = "";
    lastAutoTranslateSignature.current = "";
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
  const draft = { expression, word, translation, definition, example, source, url };
  const ready = canSendToAnki(context, expression);
  const isRecording = Boolean(context?.isRecording);

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
          {isRecording && <button className="secondary" onClick={cancelCapture}>Cancel capture</button>}
          <button className="secondary" onClick={openSidePanel}>Open review panel</button>
          <button className="secondary" disabled={!ready || isRecording} onClick={createCard}>Send to Anki</button>
        </div>

        <p className="status">{audioDuration ? `Audio: ${audioDuration.toFixed(1)}s | ${message}` : message}</p>
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
              <h2>Audio</h2>
              <button className="secondary inline-action" disabled={isRecording} onClick={recaptureAudio}>Re-record audio</button>
            </div>
            {capture?.audio?.dataUrl ? (
              <>
                <audio className="media-preview media-preview--audio" controls src={capture.audio.dataUrl} onLoadedMetadata={(event) => {
                  const duration = event.currentTarget.duration;
                  setAudioDuration(Number.isFinite(duration) ? duration : null);
                }} />
                <p className="status">{audioDuration ? `Audio: ${audioDuration.toFixed(1)}s` : "Audio ready"}</p>
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
              <p className="muted footer-copy">Preview first, then send the final card to Anki.</p>
              <button className="primary-action" disabled={!ready || isRecording} onClick={createCard}>Send to Anki</button>
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

function AnkiFieldPreview({ context, draft }: { context: PopupContext | null; draft: SentenceDraft }) {
  const mapping = context?.settings.fieldMapping;
  if (!mapping) {
    return null;
  }

  const rows = [
    ["Expression", mapping.expression, draft.expression],
    ["Word", mapping.word, draft.word],
    ["Translation", mapping.translation, draft.translation],
    ["Definition", mapping.definition, draft.definition],
    ["Example", mapping.example, draft.example],
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

function canSendToAnki(context: PopupContext | null, expression: string) {
  const capture = context?.capture;
  return Boolean(
    (capture?.subtitle || expression)
    && capture?.screenshot?.dataUrl
    && capture?.audio?.dataUrl
    && context?.settings.deckName
    && context?.settings.modelName
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
