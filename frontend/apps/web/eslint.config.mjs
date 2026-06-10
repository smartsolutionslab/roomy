import nx from '@nx/eslint-plugin';

import baseConfig from '../../../eslint.config.mjs';

/**
 * Angular ESLint for the web app (issue #23, carry-over from #10).
 *
 * Layers the Angular component/template rules under the root flat config:
 * `baseConfig` carries the #7 `@nx/enforce-module-boundaries` boundary rule and
 * the #10 TypeScript/import ruleset, both spread in unchanged — extended, never
 * weakened. The Angular-specific selector rules below pin the `roomy` prefix.
 */
export default [
  ...nx.configs['flat/angular'],
  ...nx.configs['flat/angular-template'],
  ...baseConfig,
  {
    files: ['**/*.ts'],
    rules: {
      '@angular-eslint/directive-selector': [
        'error',
        {
          type: 'attribute',
          prefix: 'roomy',
          style: 'camelCase',
        },
      ],
      '@angular-eslint/component-selector': [
        'error',
        {
          type: 'element',
          prefix: 'roomy',
          style: 'kebab-case',
        },
      ],
    },
  },
  {
    files: ['**/*.html'],
    rules: {},
  },
];
