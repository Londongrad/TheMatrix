import React, { useEffect, useState } from "react";
import ReactDOM from "react-dom";
import "@shared/styles/confirm-dialog.css";

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
      <div className="confirm-dialog-backdrop" onClick={onClose} />
      <form
        className="confirm-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="delete-account-title"
        onSubmit={handleSubmit}
      >
        <div className="confirm-dialog-header">
          <h2 id="delete-account-title" className="confirm-dialog-title">
            DELETE ACCOUNT?
          </h2>
          <p className="confirm-dialog-description">
            This will permanently delete your Overseer identity and all
            associated simulations. This action cannot be undone.
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
