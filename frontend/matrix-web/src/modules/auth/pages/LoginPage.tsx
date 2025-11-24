import React, { useState } from "react";
import { useAuth } from "../../../api/auth/AuthContext";
import { useLocation, useNavigate, Link } from "react-router-dom";
import "../../../styles/auth/login-page.css";

export const LoginPage = () => {
  const { login: loginUser } = useAuth();
  const navigate = useNavigate();
  const location = useLocation() as { state?: { from?: Location } };

  const from = (location.state?.from as any)?.pathname || "/";

  const [login, setLogin] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);

    try {
      await loginUser({ login, password });
      navigate(from, { replace: true });
    } catch (err: any) {
      setError(err.message || "Login failed");
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="login-page">
      <div className="login-orb login-orb--cyan" />
      <div className="login-orb login-orb--violet" />

      <div className="login-card">
        <div className="login-card__side">
          <div className="login-logo">
            <span className="login-logo__dot" />
            <span>Matrix Control Center</span>
          </div>

          <h2 className="login-heading">
            Welcome back, <span>Overseer</span>
          </h2>

          <p className="login-text">
            Sign in to resume orchestrating your city simulation. Monitor
            population, incidents and systems – all from a single control panel.
          </p>

          <div className="login-feature-list">
            <div className="login-feature">
              <span className="login-feature-dot" />
              <span>Real-time overview of your digital metropolis</span>
            </div>
            <div className="login-feature">
              <span className="login-feature-dot" />
              <span>Trigger &amp; resolve incidents with one click</span>
            </div>
            <div className="login-feature">
              <span className="login-feature-dot" />
              <span>Fine-tune citizens, budgets and infrastructure</span>
            </div>
          </div>
        </div>

        <div className="login-card__form">
          <h1 className="login-title">Login</h1>
          <p className="login-subtitle">
            Enter your credentials to access the dashboard.{" "}
            <Link to="/register">Create an account</Link> if you don&apos;t have
            one yet.
          </p>

          <form className="login-form" onSubmit={handleSubmit}>
            <div className="login-field">
              <div className="login-label-row">
                <span className="login-label">Login</span>
                <span>Use email or username</span>
              </div>
              <input
                className="login-input"
                type="text"
                value={login}
                onChange={(e) => setLogin(e.target.value)}
                placeholder="you@example.com or matrix_god"
                required
              />
            </div>

            <div className="login-field">
              <div className="login-label-row">
                <span className="login-label">Password</span>
              </div>
              <input
                className="login-input"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder="••••••••"
                required
              />
            </div>

            {error && <div className="login-error">{error}</div>}

            <button
              className="login-button"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Logging in..." : "Login"}
            </button>
          </form>

          <div className="login-switch">
            Don&apos;t have an account? <Link to="/register">Register</Link>
          </div>
        </div>
      </div>
    </div>
  );
};
