import nx from '@nx/eslint-plugin';
import tseslint from 'typescript-eslint';
import importPlugin from 'eslint-plugin-import';
import prettier from 'eslint-config-prettier';

/**
 * Root ESLint flat config.
 *
 * Layered scope:
 *  - issue #7 added the Nx module-boundary rule (the Clean Architecture
 *    dependency rule + bounded-context isolation, ADR-0002/0003) as
 *    tag-based dependency constraints. That rule is preserved verbatim below.
 *  - issue #10 adds the workspace TypeScript/JS correctness ruleset
 *    (typescript-eslint recommended, import ordering, no-unused) and wires
 *    `eslint-config-prettier` last so ESLint and Prettier do not fight over
 *    formatting (Prettier is the source of truth — docs/coding-standards/typescript.md).
 *
 * Angular-specific linting (`angular-eslint`) is intentionally deferred to
 * issue #23, which introduces the Angular app — those rules require an app to
 * lint and would be inert (or misconfigured) before it exists.
 */
export default [
  ...nx.configs['flat/base'],
  // Ignore generated/build artefacts so they are never linted.
  {
    ignores: [
      '**/dist',
      '**/out-tsc',
      '**/tmp',
      '**/coverage',
      '**/node_modules',
      '**/.angular',
      '**/.nx',
      '**/bin',
      '**/obj',
      '**/vitest.config.*.timestamp*',
      // Generated OpenAPI clients (ADR-0036) are build artefacts; never lint them.
      '**/api/src/lib/generated',
    ],
  },
  // TypeScript correctness ruleset (issue #10). typescript-eslint recommended
  // rules encode the standards in docs/coding-standards/typescript.md.
  ...tseslint.configs.recommended.map((config) => ({
    ...config,
    files: ['**/*.ts', '**/*.tsx'],
  })),
  // Parse TypeScript so the boundary rule and TS rules can resolve TS imports.
  {
    files: ['**/*.ts', '**/*.tsx'],
    plugins: {
      import: importPlugin,
    },
    languageOptions: {
      parser: tseslint.parser,
    },
    settings: {
      'import/resolver': {
        typescript: true,
        node: true,
      },
    },
    rules: {
      // No `any` — use `unknown` and narrow; an inline justification is required
      // for the rare sanctioned case (typescript.md).
      '@typescript-eslint/no-explicit-any': 'error',
      // No non-null assertion (`!`) to bypass null checks (typescript.md).
      '@typescript-eslint/no-non-null-assertion': 'error',
      // No unused imports/vars; allow an explicit `_`-prefixed throwaway.
      '@typescript-eslint/no-unused-vars': [
        'error',
        { argsIgnorePattern: '^_', varsIgnorePattern: '^_' },
      ],
      // Named exports only; no default exports (typescript.md).
      'import/no-default-export': 'error',
      // Import ordering via ESLint (typescript.md): groups separated and sorted.
      'import/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', 'parent', 'sibling', 'index'],
          'newlines-between': 'always',
          alphabetize: { order: 'asc', caseInsensitive: true },
        },
      ],
      // No duplicate imports of the same module.
      'import/no-duplicates': 'error',
    },
  },
  // --- Nx module boundaries (issue #7 — DO NOT weaken) ---------------------
  // Encodes the Clean Architecture dependency rule and bounded-context
  // isolation (ADR-0002, ADR-0003) as tag-based dependency constraints.
  {
    files: ['**/*.ts', '**/*.tsx', '**/*.js', '**/*.jsx'],
    rules: {
      '@nx/enforce-module-boundaries': [
        'error',
        {
          enforceBuildableLibDependency: true,
          allow: [],
          depConstraints: [
            // --- Layer rule (Clean Architecture dependency direction) ---
            // domain depends on nothing (only shared util primitives).
            {
              sourceTag: 'type:domain',
              onlyDependOnLibsWithTags: ['type:domain', 'type:util'],
            },
            // application depends only on domain (and util).
            {
              sourceTag: 'type:application',
              onlyDependOnLibsWithTags: ['type:application', 'type:domain', 'type:util'],
            },
            // infrastructure depends inward (application, domain) and util.
            {
              sourceTag: 'type:infrastructure',
              onlyDependOnLibsWithTags: [
                'type:infrastructure',
                'type:application',
                'type:domain',
                'type:util',
              ],
            },
            // apps/hosts are the composition root: they may wire any layer,
            // backend or frontend (the SPA composes feature/ui/api/data-access libs).
            {
              sourceTag: 'type:app',
              onlyDependOnLibsWithTags: [
                'type:app',
                'type:infrastructure',
                'type:application',
                'type:domain',
                'type:feature',
                'type:ui',
                'type:api',
                'type:data-access',
                'type:util',
              ],
            },
            // --- Frontend layer rule (Angular feature libraries, ADR-0035) ---
            // feature (smart, routed UI) → ui, api, data-access, and util.
            {
              sourceTag: 'type:feature',
              onlyDependOnLibsWithTags: [
                'type:feature',
                'type:ui',
                'type:api',
                'type:data-access',
                'type:util',
              ],
            },
            // ui (presentational) → ui and util only.
            {
              sourceTag: 'type:ui',
              onlyDependOnLibsWithTags: ['type:ui', 'type:util'],
            },
            // api (typed per-context OpenAPI client + gateway) → shared data-access
            // (session/pagination) and util.
            {
              sourceTag: 'type:api',
              onlyDependOnLibsWithTags: ['type:api', 'type:data-access', 'type:util'],
            },
            // data-access (shared client-side data utilities: session, theme,
            // pagination) → data-access and util.
            {
              sourceTag: 'type:data-access',
              onlyDependOnLibsWithTags: ['type:data-access', 'type:util'],
            },
            // util is a leaf: shared primitives depend on nothing but util.
            {
              sourceTag: 'type:util',
              onlyDependOnLibsWithTags: ['type:util'],
            },

            // --- Context isolation (no cross-context coupling) ---
            // Each context may depend only on its own libs and shared ones.
            // Cross-context communication is by ID + integration events only
            // (ADR-0003/0005/0014) — never by importing another context's libs.
            {
              sourceTag: 'context:identity',
              onlyDependOnLibsWithTags: ['context:identity', 'context:shared'],
            },
            {
              sourceTag: 'context:organization',
              onlyDependOnLibsWithTags: ['context:organization', 'context:shared'],
            },
            {
              sourceTag: 'context:attendance',
              onlyDependOnLibsWithTags: ['context:attendance', 'context:shared'],
            },
            // Shared libs must stay context-agnostic: depend only on shared.
            {
              sourceTag: 'context:shared',
              onlyDependOnLibsWithTags: ['context:shared'],
            },
            // The single SPA is the frontend composition root: unlike the
            // per-context backend hosts, ADR-0016/0030 mandate ONE Angular app
            // across all contexts, so it may compose any context's frontend
            // libs (ADR-0035). This is the only project tagged context:web.
            {
              sourceTag: 'context:web',
              onlyDependOnLibsWithTags: [
                'context:web',
                'context:identity',
                'context:organization',
                'context:attendance',
                'context:shared',
              ],
            },
          ],
        },
      ],
    },
  },
  // Config files (this flat config, etc.) are ESM modules that legitimately use
  // a default export; exempt them from the named-exports-only rule.
  {
    files: ['**/*.config.{js,mjs,cjs,ts}', 'eslint.config.mjs'],
    rules: {
      'import/no-default-export': 'off',
    },
  },
  // Prettier compatibility — MUST be last so it disables every formatting rule
  // that would conflict with Prettier (the formatting source of truth).
  prettier,
];
