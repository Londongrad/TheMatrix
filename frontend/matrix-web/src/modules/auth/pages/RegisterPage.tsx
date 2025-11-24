import React, { useState } from "react";
import { useAuth } from "../../../api/auth/AuthContext";
import { useNavigate, Link } from "react-router-dom";
import "../../../styles/auth/register-page.css";
import MatrixRainBackground from "../components/MatrixRainBackground";

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
    <div className="register-page">
      {/* фоновый цифровой дождь, всегда под всем */}
      <MatrixRainBackground />

      {/* слой поверх дождя */}
      <div className="register-page__inner">
        <div className="register-orb register-orb--cyan" />
        <div className="register-orb register-orb--violet" />

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
              account? <Link to="/login">Sign in</Link>.
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
                />
              </div>

              {error && <div className="register-error">{error}</div>}

              <button
                className="register-button"
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting ? "Registering..." : "Register"}
              </button>
            </form>

            <div className="register-switch">
              Already have an account? <Link to="/login">Login</Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
