# React + TypeScript + Vite

## Local Development

`frontend/matrix-web/.env.development` should keep `VITE_API_BASE_URL=https://localhost:7155`.

The frontend supports two local dev modes:

- Recommended full local mode: `https://localhost:5173`
- requires local HTTPS certificates at `frontend/matrix-web/certs/localhost-key.pem` and
  `frontend/matrix-web/certs/localhost.pem`
- supports the gateway secure refresh-cookie flow

- Fallback clean-checkout mode: `http://localhost:5173`
- starts automatically when the certificate files are missing
- useful for build and basic frontend startup, but full authenticated cookie refresh/logout flow is expected to use
  HTTPS because the gateway refresh cookie is `Secure` and `SameSite=Strict`

One common local setup is `mkcert`:

```powershell
Set-Location frontend/matrix-web
New-Item -ItemType Directory -Force certs | Out-Null
mkcert -key-file certs/localhost-key.pem -cert-file certs/localhost.pem localhost 127.0.0.1 ::1
```

Do not commit generated certificates.

## Verification

Run the frontend quality gate locally with:

```powershell
Set-Location frontend/matrix-web
npm run check
```

`npm run check` runs:

- `npm run lint`
- `npm run build`

The build must pass even when `frontend/matrix-web/certs` is missing because Vite falls back to `http://localhost:5173`
when the local certificate files are absent.

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react)
  uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used
  in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc)
  uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it,
see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also
install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x)
and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom)
for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
