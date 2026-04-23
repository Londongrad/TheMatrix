import {defineConfig} from "vite";
import react from "@vitejs/plugin-react";
import fs from "node:fs";
import path from "node:path";
import {fileURLToPath, URL} from "node:url";

const configDirectory = path.dirname(fileURLToPath(import.meta.url));
const localhostKeyPath = path.resolve(configDirectory, "certs/localhost-key.pem");
const localhostCertPath = path.resolve(configDirectory, "certs/localhost.pem");
const hasLocalHttpsCertificates =
    fs.existsSync(localhostKeyPath) && fs.existsSync(localhostCertPath);

export default defineConfig({
    plugins: [react()],

    resolve: {
        alias: {
            "@app": fileURLToPath(new URL("./src/app", import.meta.url)),
            "@services": fileURLToPath(new URL("./src/services", import.meta.url)),
            "@shared": fileURLToPath(new URL("./src/shared", import.meta.url)),
            "@pages": fileURLToPath(new URL("./src/pages", import.meta.url)),
        },
    },

    server: {
        https: hasLocalHttpsCertificates
            ? {
                  key: fs.readFileSync(localhostKeyPath),
                  cert: fs.readFileSync(localhostCertPath),
              }
            : false,
        host: "localhost",
        port: 5173,
    },
});
