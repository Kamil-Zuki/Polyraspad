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
};

export type PopupContext = {
  capture?: CaptureData;
  settings: AnkiSettings;
  choices: {
    deckNames?: string[];
    modelNames?: string[];
    modelFieldNames?: string[];
  };
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
