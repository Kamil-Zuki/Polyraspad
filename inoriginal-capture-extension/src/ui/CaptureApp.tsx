import { useEffect, useRef, useState } from "react";
import { getPopupContext, saveAnkiSettings, sendRuntimeMessage } from "../shared/chromeApi";
import type { CaptureData, PopupContext } from "../shared/types";
import "./styles.css";

type CaptureAppProps = {
  mode: "popup" | "sidepanel";
};

export function CaptureApp({ mode }: CaptureAppProps) {
  const [context, setContext] = useState<PopupContext | null>(null);
  const [status, setStatus] = useState("Loading...");
  const [front, setFront] = useState("");
  const [back, setBack] = useState("");
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
      setStatus("Loading...");
    }

    try {
      const nextContext = await getPopupContext();
      setContext(nextContext);
      hydrateEditor(nextContext.capture);
      setStatus(buildStatus(nextContext));
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Failed to load popup.");
    }
  }

  function hydrateEditor(capture?: CaptureData) {
    const signature = buildCaptureSignature(capture);
    if (signature === lastCaptureSignature.current) {
      return;
    }

    setFront(capture?.subtitle || "");
    setBack(buildEditableBack(capture));
    lastCaptureSignature.current = signature;
  }

  async function runAction(type: string) {
    setStatus("Working...");
    const response = await sendRuntimeMessage({ type });
    if (!response.ok) {
      setStatus(response.error || "Action failed.");
      return;
    }

    await refresh();
  }

  async function captureSubtitleClip() {
    setStatus("Making a card: screenshot now, audio until the subtitle changes...");
    const response = await sendRuntimeMessage({ type: "capture-subtitle-clip" });
    if (!response.ok) {
      setStatus(response.error || "Capture failed.");
      return;
    }

    setStatus("Recording audio until the subtitle changes...");
    await refresh();
  }

  async function createCard() {
    setStatus("Creating Anki card...");
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
      setStatus(response.error || "Failed to create card.");
      return;
    }

    setStatus(`Created note ${response.result?.noteId}.`);
  }

  async function openSidePanel() {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab?.windowId) {
      setStatus("No active window available for the side panel.");
      return;
    }

    if (typeof chrome.sidePanel?.open !== "function") {
      setStatus("This Chrome version does not support side panel opening.");
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

  const capture = context?.capture;
  const isRecording = Boolean(context?.isRecording);
  const isClipRecording = isRecording && context?.sessionMode === "clip";

  return (
    <main className="panel mining-layout">
      <header className="hero">
        <div>
          <p className="eyebrow">Subtitle Mining</p>
          <h1>InOriginal Capture</h1>
          <p className="muted hero-copy">Capture the current subtitle, preview screenshot and audio, then send the final card to Anki.</p>
        </div>
        <div className="hero-actions">
          {mode === "popup" && (
            <button id="open-sidepanel" className="secondary ghost-button" onClick={openSidePanel}>Side panel</button>
          )}
          <button className="secondary ghost-button" onClick={() => chrome.runtime.openOptionsPage()}>Settings</button>
        </div>
      </header>

      <section className="workflow-bar">
        <WorkflowStep index="1" title="Capture" copy="Grab subtitle, screenshot, and audio clip." />
        <WorkflowStep index="2" title="Preview" copy="Check image, audio, and subtitle context." />
        <WorkflowStep index="3" title="Create" copy="Send the finished card to Anki." />
      </section>

      <section className="capture-panel">
        <div className="capture-main">
          <button className="primary-action" disabled={isRecording} onClick={captureSubtitleClip}>Make a card</button>
          <p className="muted action-copy">Starts capture now, keeps the video playing, and stops audio when the subtitle changes.</p>
        </div>
        <div className="actions actions--tight">
          <button className="secondary" disabled={isClipRecording} onClick={() => runAction("take-screenshot")}>Retake screenshot</button>
          <button className="secondary" onClick={() => runAction("toggle-recording")}>
            {isRecording ? "Stop audio + subtitles" : "Start or stop audio + subtitles"}
          </button>
        </div>
      </section>

      <section className="compact-toolbar">
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
      </section>

      <section className="card subtitle-stage">
        <div className="section-head">
          <h2>Current Subtitle</h2>
          <p className="status">{status}</p>
        </div>
        <div className="subtitle-stack subtitle-stack--focused">
          <p className="subtitle subtitle--muted subtitle--context">{capture?.previousSubtitle || "No previous subtitle."}</p>
          <p className="subtitle subtitle--current subtitle--hero">{capture?.subtitle || "No current subtitle yet."}</p>
          <p className="subtitle subtitle--muted subtitle--context">{capture?.nextSubtitle || "No next subtitle."}</p>
        </div>
      </section>

      <section className="media-strip">
        <article className="card media-tile">
          <div className="section-head">
            <h2>Screenshot</h2>
            <span className="tile-badge">Preview</span>
          </div>
          {capture?.screenshot?.dataUrl ? (
            <img className="media-preview media-preview--image" alt="Screenshot preview" src={capture.screenshot.dataUrl} />
          ) : (
            <p className="muted media-empty">No screenshot yet.</p>
          )}
        </article>

        <article className="card media-tile">
          <div className="section-head">
            <h2>Audio</h2>
            <span className="tile-badge">Preview</span>
          </div>
          {capture?.audio?.dataUrl ? (
            <audio className="media-preview media-preview--audio" controls src={capture.audio.dataUrl} />
          ) : (
            <p className="muted media-empty">No audio yet.</p>
          )}
        </article>
      </section>

      <section className="editor-grid">
        <label className="card editor-card">
          <span>Front</span>
          <textarea rows={3} value={front} onChange={(event) => setFront(event.target.value)} />
        </label>
        <label className="card editor-card">
          <span>Back</span>
          <textarea rows={7} value={back} onChange={(event) => setBack(event.target.value)} />
        </label>
      </section>

      <footer className="footer-bar">
        <p className="muted footer-copy">Preview first, then create the final Anki card.</p>
        <button className="primary-action" disabled={isRecording} onClick={createCard}>Create card</button>
      </footer>
    </main>
  );
}

function WorkflowStep({ index, title, copy }: { index: string; title: string; copy: string }) {
  return (
    <div className="workflow-step">
      <span className="workflow-index">{index}</span>
      <div>
        <p className="workflow-title">{title}</p>
        <p className="workflow-copy">{copy}</p>
      </div>
    </div>
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
    capture.pageUrl || ""
  ].join("|");
}

function buildStatus(context: PopupContext) {
  if (context.isRecording && context.sessionMode === "clip") {
    return "Recording the current subtitle. The preview will be ready when the subtitle changes.";
  }

  const pieces = [];
  if (context.capture?.pageTitle) {
    pieces.push(context.capture.pageTitle);
  }
  if (context.capture?.screenshot?.filename) {
    pieces.push("Image ready");
  }
  if (context.capture?.audio?.filename) {
    pieces.push("Audio ready");
  }

  return pieces.join(" | ") || "Click 'Make a card' to capture screenshot and audio for the current subtitle.";
}
