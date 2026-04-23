import {createContext, useContext} from "react";

type ConfirmTone = "default" | "danger";

export interface ConfirmOptions {
    title?: string;
    description?: string;
    confirmText?: string;
    cancelText?: string;
    tone?: ConfirmTone;
}

export type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

export const ConfirmContext = createContext<ConfirmFn | null>(null);

export function useConfirm(): ConfirmFn {
    const context = useContext(ConfirmContext);

    if (!context) {
        return async ({
            title = "Are you sure?",
            description,
            confirmText,
        }: ConfirmOptions) => {
            const message = [title, description, confirmText ? `Action: ${confirmText}` : null]
                .filter(Boolean)
                .join("\n\n");

            if (typeof window !== "undefined" && typeof window.confirm === "function") {
                console.warn("useConfirm was used outside ConfirmProvider. Falling back to window.confirm().");
                return window.confirm(message);
            }

            return false;
        };
    }

    return context;
}
