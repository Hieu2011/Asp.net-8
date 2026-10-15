import { defineConfig, globalIgnores } from 'eslint/config';
import nextVitals from 'eslint-config-next/core-web-vitals';
import nextTs from 'eslint-config-next/typescript';
import prettier from 'eslint-config-prettier/flat';

// Tên được miễn tiền tố: biến 1 ký tự (x, i), bắt đầu bằng _, hằng UPPER_CASE, export đặc biệt của Next
const EXEMPT_NAME = {
  regex:
    '^(?:[a-z]|_.*|[A-Z][A-Z0-9_]*|dynamic|dynamicParams|revalidate|runtime|fetchCache|preferredRegion|maxDuration)$',
  match: false,
};

// Tiền tố theo kiểu dữ liệu — đồng bộ coding-standards backend (str/int/dec/num/bol/lst/arr)
const PREFIX_BY_TYPE = {
  string: ['str'],
  number: ['int', 'dec', 'num'],
  boolean: ['bol'],
  array: ['lst', 'arr'],
};
// Biến destructure giữ tên theo nguồn (props, object) → miễn. Rule có filter được ưu tiên nhất; cùng filter thì rule có modifier thắng
const typePrefix = (selector) =>
  Object.entries(PREFIX_BY_TYPE).flatMap(([strType, arrPrefix]) => [
    { selector, types: [strType], prefix: arrPrefix, format: ['PascalCase'], filter: EXEMPT_NAME },
    { selector, modifiers: ['destructured'], types: [strType], format: null, filter: { regex: '.*', match: true } },
  ]);

export default defineConfig([
  ...nextVitals,
  ...nextTs,
  {
    files: ['**/*.{ts,tsx}'],
    languageOptions: { parserOptions: { projectService: true, tsconfigRootDir: import.meta.dirname } },
    rules: {
      '@typescript-eslint/naming-convention': [
        'error',
        { selector: 'default', format: ['camelCase'], leadingUnderscore: 'allow' },
        { selector: 'variable', format: ['camelCase', 'PascalCase', 'UPPER_CASE'], leadingUnderscore: 'allow' },
        { selector: 'function', format: ['camelCase', 'PascalCase'] },
        { selector: 'typeLike', format: ['PascalCase'] },
        { selector: 'enumMember', format: ['PascalCase'] },
        // Field của DTO/props/object là hợp đồng JSON → không ép tiền tố
        { selector: ['property', 'objectLiteralMethod', 'typeMethod'], format: null },
        { selector: 'import', format: null },
        ...typePrefix('variable'),
        ...typePrefix('parameter'),
      ],
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/consistent-type-imports': 'error',
      '@typescript-eslint/no-unused-vars': ['error', { argsIgnorePattern: '^_', varsIgnorePattern: '^_' }],
      'no-console': ['error', { allow: ['warn', 'error'] }],
      eqeqeq: ['error', 'always'],
      'prefer-const': 'error',
      'react/self-closing-comp': 'error',
      'import/order': [
        'error',
        {
          groups: ['builtin', 'external', 'internal', ['parent', 'sibling', 'index']],
          pathGroups: [
            { pattern: '{react,react-dom/**,next,next/**}', group: 'external', position: 'before' },
            { pattern: '@/**', group: 'internal' },
          ],
          pathGroupsExcludedImportTypes: ['react', 'next'],
          'newlines-between': 'always',
          alphabetize: { order: 'asc', caseInsensitive: true },
        },
      ],
    },
  },
  // Component shadcn sinh tự động → không ép tiền tố
  { files: ['src/components/ui/**'], rules: { '@typescript-eslint/naming-convention': 'off' } },
  prettier,
  globalIgnores(['.next/**', 'out/**', 'build/**', 'next-env.d.ts']),
]);
