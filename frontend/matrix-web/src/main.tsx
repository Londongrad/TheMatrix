// src/main.tsx
import ReactDOM from "react-dom/client";
import App from "./app/App";
import "@shared/styles/index.css";
import {WorkspacePreferencesProvider} from "@shared/theme/workspacePreferences";

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
    <WorkspacePreferencesProvider>
        <App/>
    </WorkspacePreferencesProvider>
);
