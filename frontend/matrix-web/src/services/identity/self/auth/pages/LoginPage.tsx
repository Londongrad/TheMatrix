// src/services/identity/auth/pages/LoginPage.tsx
import React, { useState } from "react";
import { useLocation, useNavigate, Link } from "react-router-dom";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

export const LoginPage = () => {
  const { login: loginUser } = useAuth();
  const navigate = useNavigate();
  const location = useLocation() as { state?: { from?: Location } };

  const from = (location.state?.from as any)?.pathname || "/";

  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await loginUser({ login, password, rememberMe });
      navigate(from, { replace: true });
    } catch (err: any) {
      setError(err.message || "Login failed");
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
              Welcome back, <span>Overseer</span>
            </h2>
            <p className="auth-text">
              Sign in to resume orchestrating your city simulation. Monitor
              population, incidents and systems – all from a single control
              panel.
            </p>
            <div className="auth-feature-list">
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Real-time overview of your digital metropolis</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Trigger &amp; resolve incidents with one click</span>
              </div>
              <div className="auth-feature">
                <span className="auth-feature-dot" />
                <span>Fine-tune citizens, budgets and infrastructure</span>
              </div>
            </div>
          </>
        }
      >
        <h1 className="auth-title">Login</h1>
        <p className="auth-subtitle">
          Enter your credentials to access the dashboard.{" "}
          <Link
            to="/register"
            className={isSubmitting ? "auth-link--disabled" : ""}
            onClick={(e) => {
              if (isSubmitting) e.preventDefault();
            }}
          >
            Create an account
          </Link>{" "}
          if you don&apos;t have one yet.
        </p>

        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <div className="auth-label-row">
              <span className="auth-label">Login</span>
              <span>Use email or username</span>
            </div>
            <input
              className="auth-input"
              type="text"
              value={login}
              onChange={(e) => setLogin(e.target.value)}
              placeholder="you@example.com or matrix_god"
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
          <div className="auth-extra-row">
            <label className="auth-remember">
              <input
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                disabled={isSubmitting}
              />
              <span>Remember this device</span>
            </label>
            <Link
              to="/forgot-password"
              className={`auth-forgot ${
                isSubmitting ? "auth-link--disabled" : ""
              }`}
              onClick={(e) => {
                if (isSubmitting) e.preventDefault();
              }}
            >
              Forgot password?
            </Link>
          </div>

          {error && <div className="auth-error">{error}</div>}

          <button className="auth-button" type="submit" disabled={isSubmitting}>
            {isSubmitting && (
              <span className="auth-spinner" aria-hidden="true" />
            )}
            <span>{isSubmitting ? "Logging in..." : "Login"}</span>
          </button>
        </form>

        <div className="auth-switch">
          Don&apos;t have an account?{" "}
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
