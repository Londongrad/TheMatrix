import React from "react";
import ReactDOM from "react-dom";
import "./modal.css";

interface ModalProps {
  open: boolean;
  title?: string;
  onClose: () => void;
  children: React.ReactNode;
  footer?: React.ReactNode;
}

const Modal = ({ open, title, onClose, children, footer }: ModalProps) => {
  if (!open) return null;

  const content = (
    <div className="mx-modal-overlay">
      <div className="mx-modal-backdrop" onClick={onClose} />
      <div className="mx-modal" role="dialog" aria-modal="true">
        <div className="mx-modal-header">
          <div className="mx-modal-title">{title}</div>
          <button
            type="button"
            className="mx-modal-close"
            aria-label="Close"
            onClick={onClose}
          >
            ✕
          </button>
        </div>
        <div className="mx-modal-body">{children}</div>
        {footer ? <div className="mx-modal-footer">{footer}</div> : null}
      </div>
    </div>
  );

  return ReactDOM.createPortal(content, document.body);
};

export default Modal;
