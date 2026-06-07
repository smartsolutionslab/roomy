import nx from '@nx/eslint-plugin';
import tseslint from 'typescript-eslint';

/**
 * Root ESLint flat config.
 *
 * Scope (issue #7): the Nx module-boundary rule only. This encodes the
 * Clean Architecture dependency rule and bounded-context isolation
 * (ADR-0002, ADR-0003) as tag-based dependency constraints. The full
 * Angular/TypeScript style ruleset (import ordering, no-unused, Prettier)
 * is intentionally deferred to issue #10.
 */
export default [
  ...nx.configs['flat/base'],
  // Parse TypeScript so the boundary rule can resolve TS imports. No style
  // rules here — the full lint ruleset is issue #10.
  {
    files: ['**/*.ts', '**/*.tsx'],
    languageOptions: {
      parser: tseslint.parser,
    },
  },
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
              onlyDependOnLibsWithTags: [
                'type:application',
                'type:domain',
                'type:util',
              ],
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
            // apps/hosts are the composition root: they may wire any layer.
            {
              sourceTag: 'type:app',
              onlyDependOnLibsWithTags: [
                'type:app',
                'type:infrastructure',
                'type:application',
                'type:domain',
                'type:util',
              ],
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
              onlyDependOnLibsWithTags: [
                'context:organization',
                'context:shared',
              ],
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
          ],
        },
      ],
    },
  },
];
