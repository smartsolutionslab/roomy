export const availableLanguages = ['en', 'de'] as const;

export type LanguageCode = (typeof availableLanguages)[number];

export const defaultLanguage: LanguageCode = 'en';
