// src/services/identity/auth/pages/RegisterPage.tsx
import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

export const RegisterPage = () => {
  const { register } = useAuth();
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (password !== confirmPassword) {
      setError("Passwords do not match");
      return;
    }

    setIsSubmitting(true);

    try {
      await register({ email, username, password, confirmPassword });
      navigate("/", { replace: true });
    } catch (err: any) {
      setError(err.message || "Registration failed");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthShell>
      <AuthCard
        side={
          <>
            <AuthLogo />
            <h2 className="auth-heading">
              Create your <span>Overseer</span> account
            </h2>
            <p className="auth-text">
              One account unlocks the full simulation: population, incidents,
              utilities and more. Shape the city the way you want it to behave.
            </p>
            <div className="auth-feature-list">
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Persistent identity for your god-level actions</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Separate username and email for clean UI</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Ready for future roles &amp; permissions</span>
              </div>
            </div>
          </>
        }
      >
        <h1 className="auth-title">Register</h1>
        <p className="auth-subtitle">
          Choose your credentials to enter the Matrix. Already have an account?{" "}
          <Link
            to="/login"
            className={isSubmitting ? "auth-link--disabled" : ""}
            onClick={(e) => {
              if (isSubmitting) e.preventDefault();
            }}
          >
            Sign in
          </Link>
          .
        </p>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Email</span>
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
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Username</span>
              <span>Displayed in the dashboard</span>
            </div>
            <input
              className="auth-input"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="matrix_god"
              required
              disabled={isSubmitting}
            />
          </div>
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Password</span>
            </div>
            <input
              className="auth-input"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="••••••••"
              required
              disabled={isSubmitting}
            />
          </div>
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Confirm password</span>
            </div>
            <input
              className="auth-input"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              placeholder="••••••••"
              required
              disabled={isSubmitting}
            />
          </div>

          {error && <div className="auth-error">{error}</div>}

          <button className="auth-button" type="submit" disabled={isSubmitting}>
            {isSubmitting && (
              <span className="auth-spinner" aria-hidden="true" />
            )}
            <span>{isSubmitting ? "Registering..." : "Register"}</span>
          </button>
        </form>

        <div className="auth-switch">
          Already have an account?{" "}
          <Link
            to="/login"
            className={isSubmitting ? "auth-link--disabled" : ""}
            onClick={(e) => {
              if (isSubmitting) e.preventDefault();
            }}
          >
            Login
          </Link>
        </div>
      </AuthCard>
    </AuthShell>
  );
};
