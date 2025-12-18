import React, {
  createContext,
  useCallback,
  useContext,
  useRef,
  useState,
  type ReactNode,
} from "react";
import ReactDOM from "react-dom";
import "@styles/shared/confirm-dialog.css";

type ConfirmTone = "default" | "danger";

export interface ConfirmOptions {
  title?: string;
  description?: string;
  confirmText?: string;
  cancelText?: string;
  tone?: ConfirmTone;
}

type ConfirmFn = (options: ConfirmOptions) => Promise<boolean>;

const ConfirmContext = createContext<ConfirmFn | null>(null);

interface DialogState extends ConfirmOptions {
  open: boolean;
}

interface ConfirmDialogProps extends DialogState {
  onConfirm: () => void;
  onCancel: () => void;
}

const ConfirmDialog = ({
  open,
  title = "Are you sure?",
  description,
  confirmText = "Confirm",
  cancelText = "Cancel",
  tone = "default",
  onConfirm,
  onCancel,
}: ConfirmDialogProps) => {
  if (!open) return null;

  const dialog = (
    <div className="confirm-dialog-overlay">
      <div className="confirm-dialog-backdrop" onClick={onCancel} />
      <div
        className="confirm-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby="confirm-dialog-title"
      >
        <div className="confirm-dialog-header">
          <h2 id="confirm-dialog-title" className="confirm-dialog-title">
            {title}
          </h2>
          {description && (
            <p className="confirm-dialog-description">{description}</p>
          )}
        </div>

        <div className="confirm-dialog-actions">
          <button
            type="button"
            className="confirm-dialog-button confirm-dialog-button--ghost"
            onClick={onCancel}
          >
            {cancelText}
          </button>
          <button
            type="button"
            className={
              tone === "danger"
                ? "confirm-dialog-button confirm-dialog-button--danger"
                : "confirm-dialog-button"
            }
            onClick={onConfirm}
          >
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );

  // через портал, чтобы модалка была поверх всего
  return ReactDOM.createPortal(dialog, document.body);
};

export const ConfirmProvider: React.FC<{ children: ReactNode }> = ({
  children,
}) => {
  const [state, setState] = useState<DialogState>({
    open: false,
    title: undefined,
    description: undefined,
    confirmText: undefined,
    cancelText: undefined,
    tone: "default",
  });

  const resolverRef = useRef<((value: boolean) => void) | null>(null);

  const confirm = useCallback<ConfirmFn>((options: ConfirmOptions) => {
    return new Promise<boolean>((resolve) => {
      resolverRef.current = resolve;
      setState({
        open: true,
        title: options.title,
        description: options.description,
        confirmText: options.confirmText,
        cancelText: options.cancelText,
        tone: options.tone ?? "default",
      });
    });
  }, []);

  const handleClose = (result: boolean) => {
    if (resolverRef.current) {
      resolverRef.current(result);
      resolverRef.current = null;
    }
    setState((prev) => ({ ...prev, open: false }));
  };

  return (
    <ConfirmContext.Provider value={confirm}>
      {children}
      <ConfirmDialog
        open={state.open}
        title={state.title}
        description={state.description}
        confirmText={state.confirmText}
        cancelText={state.cancelText}
        tone={state.tone}
        onConfirm={() => handleClose(true)}
        onCancel={() => handleClose(false)}
      />
    </ConfirmContext.Provider>
  );
};

export const useConfirm = (): ConfirmFn => {
  const ctx = useContext(ConfirmContext);
  if (!ctx) {
    throw new Error("useConfirm must be used within ConfirmProvider");
  }
  return ctx;
};
