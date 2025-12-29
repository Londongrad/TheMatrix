import React, { useState } from "react";
import { Link } from "react-router-dom";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

export const ForgotPasswordPage = () => {
  const [email, setEmail] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setNotice(
      "If an account exists for this email, a reset link will appear in your inbox shortly."
    );
    setIsSubmitting(false);
  };

  return (
    <AuthShell>
      <AuthCard
        side={
          <>
            <AuthLogo />
            <h2 className="auth-heading">
              Restore access to the <span>Matrix</span>
            </h2>
            <p className="auth-text">
              Lost your access key? We&apos;ll route a reset signal to your
              terminal and help you get back online.
            </p>
            <div className="auth-feature-list">
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Instant delivery through secure channels</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Single-use reset token for your safety</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Support team standing by if you need help</span>
              </div>
            </div>
          </>
        }
      >
        <h1 className="auth-title">Forgot password</h1>
        <p className="auth-subtitle">
          Drop your email and we&apos;ll send a reset link. Remembered it
          already?{" "}
          <Link
            to="/login"
            className={isSubmitting ? "auth-link--disabled" : ""}
            onClick={(e) => {
              if (isSubmitting) e.preventDefault();
            }}
          >
            Back to login
          </Link>
          .
        </p>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Email</span>
              <span>We&apos;ll never share it</span>
            </div>
            <input
              className="auth-input"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="you@example.com"
              required
              disabled={isSubmitting}
            />
          </div>

          {notice && <div className="auth-success">{notice}</div>}

          <button className="auth-button" type="submit" disabled={isSubmitting}>
            {isSubmitting && (
              <span className="auth-spinner" aria-hidden="true" />
            )}
            <span>{isSubmitting ? "Sending..." : "Send reset link"}</span>
          </button>
        </form>

        <div className="auth-switch">
          Need a new account?{" "}
          <Link
            to="/register"
            className={isSubmitting ? "auth-link--disabled" : ""}
            onClick={(e) => {
              if (isSubmitting) e.preventDefault();
            }}
          >
            Register
          </Link>
        </div>
      </AuthCard>
    </AuthShell>
  );
};
