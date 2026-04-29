export type FieldMapping = {
  expression: string;
  word: string;
  image: string;
  audio: string;
  transcription: string;
  source: string;
  wordTypes: string;
  definition: string;
  translation: string;
  mnemonic: string;
  example: string;
  antonyms: string;
  synonyms: string;
  url: string;
};

export type AnkiSettings = {
  endpoint: string;
  deckName: string;
  modelName: string;
  rewindMs: number;
  maxClipMs: number;
  translationMode: "manual" | "after-capture" | "before-send";
  translationSourceLang: string;
  translationTargetLang: string;
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
    durationMs?: number;
    stopReason?: "subtitle-change" | "manual" | "max-duration" | "range";
    videoStartTime?: number;
    videoEndTime?: number;
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

export type SentenceDraft = {
  expression: string;
  word: string;
  translation: string;
  definition: string;
  example: string;
  source: string;
  url: string;
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
  sentenceDraft?: SentenceDraft;
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
