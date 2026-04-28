import { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import { saveAnkiSettings, sendRuntimeMessage } from "../shared/chromeApi";
import type { AnkiSettings, FieldMapping } from "../shared/types";
import "./styles.css";

const DEFAULT_SETTINGS: AnkiSettings = {
  endpoint: "http://127.0.0.1:8765",
  deckName: "Default",
  modelName: "Basic",
  rewindMs: 1200,
  tags: "inoriginal",
  fieldMapping: {
    front: "Front",
    back: "Back",
    subtitle: "",
    context: "",
    source: "",
    image: "",
    audio: ""
  }
};

function OptionsApp() {
  const [settings, setSettings] = useState<AnkiSettings>(DEFAULT_SETTINGS);
  const [deckNames, setDeckNames] = useState<string[]>([]);
  const [modelNames, setModelNames] = useState<string[]>([]);
  const [fieldNames, setFieldNames] = useState<string[]>([]);
  const [status, setStatus] = useState("Idle.");

  useEffect(() => {
    void initialize();
  }, []);

  async function initialize() {
    const { ankiSettings } = await chrome.storage.local.get("ankiSettings");
    const nextSettings = normalizeSettings(ankiSettings || {});
    setSettings(nextSettings);
    await refreshChoices(nextSettings);
  }

  async function testConnection() {
    setStatus("Testing AnkiConnect...");
    const response = await sendRuntimeMessage({
      type: "anki-action",
      action: "ping",
      payload: { endpoint: settings.endpoint }
    });
    setStatus(response.ok ? "Connected to AnkiConnect." : response.error || "Connection failed.");
  }

  async function refreshChoices(baseSettings = settings) {
    setStatus("Loading decks and note types...");
    const response = await sendRuntimeMessage<{
      deckNames: string[];
      modelNames: string[];
      modelFieldNames: string[];
    }>({
      type: "anki-action",
      action: "popupChoices",
      payload: {
        endpoint: baseSettings.endpoint,
        modelName: baseSettings.modelName
      }
    });

    if (!response.ok || !response.result) {
      setStatus(response.error || "Failed to load choices.");
      return;
    }

    setDeckNames(response.result.deckNames || []);
    setModelNames(response.result.modelNames || []);
    setFieldNames(response.result.modelFieldNames || []);
    setStatus("Loaded decks and note types.");
  }

  async function refreshFields(modelName: string) {
    const response = await sendRuntimeMessage<{ values: string[] }>({
      type: "anki-action",
      action: "modelFieldNames",
      payload: {
        endpoint: settings.endpoint,
        modelName
      }
    });

    if (response.ok && response.result?.values) {
      setFieldNames(response.result.values);
    }
  }

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    try {
      const saved = await saveAnkiSettings(settings);
      setSettings(saved);
      setStatus("Settings saved.");
    } catch (error) {
      setStatus(error instanceof Error ? error.message : "Failed to save settings.");
    }
  }

  function updateFieldMapping(key: keyof FieldMapping, value: string) {
    setSettings({
      ...settings,
      fieldMapping: {
        ...settings.fieldMapping,
        [key]: value
      }
    });
  }

  return (
    <main className="panel panel--wide">
      <h1>InOriginal Capture Helper</h1>
      <p className="muted">Configure AnkiConnect, the target deck, the note type, and which note fields receive subtitle, screenshot, and audio content.</p>

      <form className="form-grid" onSubmit={handleSubmit}>
        <label>
          <span>AnkiConnect URL</span>
          <input value={settings.endpoint} onChange={(event) => setSettings({ ...settings, endpoint: event.target.value })} />
        </label>

        <div className="split-grid">
          <label>
            <span>Deck name</span>
            <select value={settings.deckName} onChange={(event) => setSettings({ ...settings, deckName: event.target.value })}>
              {renderOptions(deckNames, settings.deckName)}
            </select>
          </label>
          <label>
            <span>Note type</span>
            <select value={settings.modelName} onChange={(event) => {
              const modelName = event.target.value;
              setSettings({ ...settings, modelName });
              void refreshFields(modelName);
            }}>
              {renderOptions(modelNames, settings.modelName)}
            </select>
          </label>
        </div>

        <label>
          <span>Tags</span>
          <input value={settings.tags} onChange={(event) => setSettings({ ...settings, tags: event.target.value })} />
        </label>

        <label>
          <span>Rewind before recording: {(settings.rewindMs / 1000).toFixed(1)}s</span>
          <input
            max={2000}
            min={500}
            step={100}
            type="range"
            value={settings.rewindMs}
            onChange={(event) => setSettings({ ...settings, rewindMs: Number(event.target.value) })}
          />
        </label>

        <div className="actions">
          <button type="button" className="secondary" onClick={testConnection}>Test AnkiConnect</button>
          <button type="button" className="secondary" onClick={() => refreshChoices()}>Refresh decks and note types</button>
        </div>

        <h2>Field Mapping</h2>
        <div className="split-grid">
          {fieldSelector("Front field", "front", false)}
          {fieldSelector("Back field", "back", false)}
          {fieldSelector("Subtitle field", "subtitle", true)}
          {fieldSelector("Context field", "context", true)}
          {fieldSelector("Source field", "source", true)}
          {fieldSelector("Image field", "image", true)}
          {fieldSelector("Audio field", "audio", true)}
        </div>

        <div className="actions">
          <button type="submit">Save settings</button>
        </div>
      </form>

      <section>
        <h2>Status</h2>
        <p className="status">{status}</p>
      </section>
    </main>
  );

  function fieldSelector(label: string, key: keyof FieldMapping, allowEmpty: boolean) {
    return (
      <label>
        <span>{label}</span>
        <select value={settings.fieldMapping[key]} onChange={(event) => updateFieldMapping(key, event.target.value)}>
          {allowEmpty && <option value="">Not used</option>}
          {renderOptions(fieldNames, settings.fieldMapping[key])}
        </select>
      </label>
    );
  }
}

function renderOptions(values: string[], selectedValue: string) {
  const options = values.includes(selectedValue) || !selectedValue
    ? values
    : [...values, selectedValue];

  return options.map((value) => (
    <option key={value} value={value}>{value}</option>
  ));
}

function normalizeSettings(value: Partial<AnkiSettings>): AnkiSettings {
  return {
    ...DEFAULT_SETTINGS,
    ...value,
    fieldMapping: {
      ...DEFAULT_SETTINGS.fieldMapping,
      ...(value.fieldMapping || {})
    }
  };
}

createRoot(document.getElementById("root")!).render(<OptionsApp />);
