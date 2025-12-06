import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import fs from "node:fs";
import path from "node:path";
import { fileURLToPath, URL } from "node:url";

export default defineConfig({
  plugins: [react()],

  resolve: {
    alias: {
      "@api": fileURLToPath(new URL("./src/api", import.meta.url)),
      "@modules": fileURLToPath(new URL("./src/modules", import.meta.url)),
      "@app": fileURLToPath(new URL("./src/app", import.meta.url)),
      "@styles": fileURLToPath(new URL("./src/styles", import.meta.url)),
    },
  },

  server: {
    https: {
      key: fs.readFileSync(path.resolve(__dirname, "certs/localhost-key.pem")),
      cert: fs.readFileSync(path.resolve(__dirname, "certs/localhost.pem")),
    },
    host: "localhost",
    port: 5173,
  },
});
