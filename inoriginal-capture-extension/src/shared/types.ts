export type FieldMapping = {
  front: string;
  back: string;
  subtitle: string;
  context: string;
  source: string;
  image: string;
  audio: string;
};

export type AnkiSettings = {
  endpoint: string;
  deckName: string;
  modelName: string;
  rewindMs: number;
  tags: string;
  fieldMapping: FieldMapping;
};

export type CaptureData = {
  capturedAt?: number;
  pageTitle?: string;
  pageUrl?: string;
  subtitle?: string;
  previousSubtitle?: string;
  nextSubtitle?: string;
  screenshot?: {
    dataUrl?: string;
    filename?: string;
  };
  audio?: {
    dataUrl?: string;
    filename?: string;
  };
  cardState?: "capturing" | "review" | "created";
  captureStep?: "idle" | "screenshot" | "rewinding" | "waiting-subtitle" | "recording-audio" | "stopping" | "review-ready" | "sending-anki" | "created" | "failed" | "cancelled";
  captureEvents?: Array<{
    at: number;
    level: "info" | "success" | "warning" | "error";
    message: string;
    step: string;
  }>;
  error?: string;
  noteId?: number;
  createdAt?: number;
};

export type CardHistoryItem = {
  noteId: number;
  subtitle: string;
  pageTitle: string;
  pageUrl: string;
  createdAt: number;
};

export type PopupContext = {
  capture?: CaptureData;
  settings: AnkiSettings;
  choices: {
    deckNames?: string[];
    modelNames?: string[];
    modelFieldNames?: string[];
  };
  cardHistory?: CardHistoryItem[];
  isRecording: boolean;
  sessionMode: string | null;
};

export type RuntimeResponse<T> = {
  ok?: boolean;
  error?: string;
  result?: T;
  context?: PopupContext;
  settings?: AnkiSettings;
};
