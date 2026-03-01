// src/services/identity/account/components/DeleteAccountDialog.tsx
import React, {useEffect, useState} from "react";
import ReactDOM from "react-dom";
import "@shared/ui/components/ConfirmDialog/ConfirmDialog";

interface DeleteAccountDialogProps {
    open: boolean;
    isSubmitting: boolean;
    error?: string | null;
    onClose: () => void;
    onConfirm: (password: string) => void;
}

const DeleteAccountDialog = ({
                                 open,
                                 isSubmitting,
                                 error,
                                 onClose,
                                 onConfirm,
                             }: DeleteAccountDialogProps) => {
    const [password, setPassword] = useState("");

    // при закрытии очищаем пароль
    useEffect(() => {
        if (!open) {
            setPassword("");
        }
    }, [open]);

    if (!open) return null;

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (!password || isSubmitting) return;
        onConfirm(password);
    };

    const dialog = (
        <div className="confirm-dialog-overlay">
            <div className="confirm-dialog-backdrop" onClick={onClose}/>
            <form
                className="confirm-dialog"
                role="dialog"
                aria-modal="true"
                aria-labelledby="delete-account-title"
                onSubmit={handleSubmit}
            >
                <div className="confirm-dialog-header">
                    <h2 id="delete-account-title" className="confirm-dialog-title">
                        SOFT DELETE ACCOUNT?
                    </h2>
                    <p className="confirm-dialog-description">
                        This will disable sign-in for this account, revoke active sessions,
                        and keep the identity reserved for a possible recovery flow later.
                    </p>
                </div>

                <div className="confirm-dialog-field">
                    <label
                        className="confirm-dialog-label"
                        htmlFor="delete-account-password"
                    >
                        Enter your password to confirm
                    </label>
                    <input
                        id="delete-account-password"
                        type="password"
                        className="confirm-dialog-input"
                        autoComplete="current-password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        placeholder="••••••••"
                    />
                </div>

                {error && <p className="confirm-dialog-error">{error}</p>}

                <div className="confirm-dialog-actions">
                    <button
                        type="button"
                        className="confirm-dialog-button confirm-dialog-button--ghost"
                        onClick={onClose}
                        disabled={isSubmitting}
                    >
                        Cancel
                    </button>
                    <button
                        type="submit"
                        className="confirm-dialog-button confirm-dialog-button--danger"
                        disabled={isSubmitting || !password}
                    >
                        {isSubmitting ? "Deleting..." : "Delete account"}
                    </button>
                </div>
            </form>
        </div>
    );

    return ReactDOM.createPortal(dialog, document.body);
};

export default DeleteAccountDialog;
