// A translatable result announcement: a Transloco key plus optional interpolation params. Feature
// pages set it after a successful action and render it through `translate(message.key, message.params)`.
export interface ResultMessage {
  readonly key: string;
  readonly params?: Record<string, unknown>;
}
