import { useEffect, useRef, useState } from "react";
import { getPopupContext, saveAnkiSettings, sendRuntimeMessage } from "../shared/chromeApi";
import type { CaptureData, FieldMapping, PopupContext } from "../shared/types";
import "./styles.css";

type CaptureAppProps = {
  mode: "popup" | "sidepanel";
};

type FlowStatus = "Idle" | "Rewinding" | "Recording subtitle audio" | "Ready to review" | "Sending to Anki" | "Created" | "Failed" | "Cancelled";

export function CaptureApp({ mode }: CaptureAppProps) {
  const [context, setContext] = useState<PopupContext | null>(null);
  const [flowStatus, setFlowStatus] = useState<FlowStatus>("Idle");
  const [message, setMessage] = useState("Loading...");
  const [front, setFront] = useState("");
  const [back, setBack] = useState("");
  const [audioDuration, setAudioDuration] = useState<number | null>(null);
  const lastCaptureSignature = useRef("");

  useEffect(() => {
    void refresh();
    const timer = window.setInterval(() => {
      void refresh(false);
    }, 1500);

    return () => window.clearInterval(timer);
  }, []);

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

    setFront(capture?.subtitle || "");
    setBack(buildEditableBack(capture));
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
    if (!canSendToAnki(context, front)) {
      setMessage("Capture subtitle, screenshot, audio, deck, and note type first.");
      return;
    }

    setFlowStatus("Sending to Anki");
    setMessage("Sending card to Anki...");
    const response = await sendRuntimeMessage<{ noteId: number }>({
      type: "create-anki-card",
      payload: {
        subtitle: context?.capture?.subtitle || front,
        front,
        back,
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

  async function makeAnother() {
    await sendRuntimeMessage({ type: "clear-draft" });
    setFront("");
    setBack("");
    setAudioDuration(null);
    lastCaptureSignature.current = "";
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
  const ready = canSendToAnki(context, front);
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
          <Checklist context={context} front={front} />
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
            <FieldMappingSelect label="Front" fieldKey="front" context={context} allowEmpty={false} onChange={updateFieldMapping} />
            <FieldMappingSelect label="Back" fieldKey="back" context={context} allowEmpty={false} onChange={updateFieldMapping} />
            <FieldMappingSelect label="Subtitle" fieldKey="subtitle" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Context" fieldKey="context" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Source" fieldKey="source" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Image" fieldKey="image" context={context} allowEmpty onChange={updateFieldMapping} />
            <FieldMappingSelect label="Audio" fieldKey="audio" context={context} allowEmpty onChange={updateFieldMapping} />
          </div>
          <p className="muted">Fields are loaded from the selected note type.</p>
        </details>

        <section className="editor-grid">
          <label className="editor-card">
            <span>Front</span>
            <textarea rows={3} value={front} onChange={(event) => setFront(event.target.value)} />
          </label>
          <label className="editor-card">
            <span>Back</span>
            <textarea rows={7} value={back} onChange={(event) => setBack(event.target.value)} />
          </label>
        </section>

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

function Checklist({ context, front }: { context: PopupContext | null; front: string }) {
  const capture = context?.capture;
  const items = [
    ["Subtitle", Boolean(capture?.subtitle || front)],
    ["Screenshot", Boolean(capture?.screenshot?.dataUrl)],
    ["Audio", Boolean(capture?.audio?.dataUrl)],
    ["Deck", Boolean(context?.settings.deckName)],
    ["Note type", Boolean(context?.settings.modelName)]
  ] as const;

  return (
    <div className="checklist">
      {items.map(([label, done]) => (
        <span key={label} className={done ? "checklist-item checklist-item--done" : "checklist-item"}>{label}</span>
      ))}
    </div>
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

function canSendToAnki(context: PopupContext | null, front: string) {
  const capture = context?.capture;
  return Boolean(
    (capture?.subtitle || front)
    && capture?.screenshot?.dataUrl
    && capture?.audio?.dataUrl
    && context?.settings.deckName
    && context?.settings.modelName
    && !context?.isRecording
  );
}

function buildEditableBack(capture?: CaptureData) {
  if (!capture) {
    return "";
  }

  return [
    capture.previousSubtitle ? `Previous: ${capture.previousSubtitle}` : "",
    capture.subtitle ? `Current: ${capture.subtitle}` : "",
    capture.nextSubtitle ? `Next: ${capture.nextSubtitle}` : "",
    capture.pageTitle ? `Title: ${capture.pageTitle}` : "",
    capture.pageUrl ? `Source: ${capture.pageUrl}` : ""
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
