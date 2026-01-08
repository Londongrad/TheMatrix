// src/services/identity/auth/pages/RegisterPage.tsx
import React, { useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import "@services/identity/self/auth/styles/register-page.css";

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

    // Бонус: простая проверка совпадения паролей на клиенте
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
      <div className="register-card">
        <div className="register-card__side">
          <div className="register-logo">
            <span className="register-logo__dot" />
            <span>Matrix Control Center</span>
          </div>

          <h2 className="register-heading">
            Create your <span>Overseer</span> account
          </h2>

          <p className="register-text">
            One account unlocks the full simulation: population, incidents,
            utilities and more. Shape the city the way you want it to behave.
          </p>

          <div className="register-feature-list">
            <div className="register-feature">
              <span className="register-feature-dot" />
              <span>Persistent identity for your god-level actions</span>
            </div>
            <div className="register-feature">
              <span className="register-feature-dot" />
              <span>Separate username and email for clean UI</span>
            </div>
            <div className="register-feature">
              <span className="register-feature-dot" />
              <span>Ready for future roles &amp; permissions</span>
            </div>
          </div>
        </div>

        <div className="register-card__form">
          <h1 className="register-title">Register</h1>
          <p className="register-subtitle">
            Choose your credentials to enter the Matrix. Already have an
            account?{" "}
            <Link
              to="/login"
              className={isSubmitting ? "register-link--disabled" : ""}
              onClick={(e) => {
                if (isSubmitting) e.preventDefault();
              }}
            >
              Sign in
            </Link>
            .
          </p>

          <form className="register-form" onSubmit={handleSubmit}>
            <div className="register-field">
              <div className="register-label-row">
                <span className="register-label">Email</span>
              </div>
              <input
                className="register-input"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="you@example.com"
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="register-field">
              <div className="register-label-row">
                <span className="register-label">Username</span>
                <span>Displayed in the dashboard</span>
              </div>
              <input
                className="register-input"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="matrix_god"
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="register-field">
              <div className="register-label-row">
                <span className="register-label">Password</span>
              </div>
              <input
                className="register-input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
                disabled={isSubmitting}
              />
            </div>

            <div className="register-field">
              <div className="register-label-row">
                <span className="register-label">Confirm password</span>
              </div>
              <input
                className="register-input"
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="••••••••"
                required
                disabled={isSubmitting}
              />
            </div>

            {error && <div className="register-error">{error}</div>}

            <button
              className="register-button"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting && (
                <span className="register-spinner" aria-hidden="true" />
              )}
              <span>{isSubmitting ? "Registering..." : "Register"}</span>
            </button>
          </form>

          <div className="register-switch">
            Already have an account?{" "}
            <Link
              to="/login"
              className={isSubmitting ? "register-link--disabled" : ""}
              onClick={(e) => {
                if (isSubmitting) e.preventDefault();
              }}
            >
              Login
            </Link>
          </div>
        </div>
      </div>
    </AuthShell>
  );
};
