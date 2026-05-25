// src/services/identity/auth/pages/LoginPage.tsx
import React, {useState} from "react";
import {Link, useLocation, useNavigate} from "react-router";
import {HttpError} from "@shared/api/http";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";
import {useAuth} from "@services/identity/api/self/auth/useAuth";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

type RedirectLocationState = {
    from?: {
        pathname?: string;
    };
};

function getHttpErrorCode(error: unknown): string | null {
    if (!(error instanceof HttpError) || !error.payload || typeof error.payload !== "object") {
        return null;
    }

    return "code" in error.payload
        ? String((error.payload as { code?: unknown }).code)
        : null;
}

export const LoginPage = () => {
    const {login: loginUser} = useAuth();
    const navigate = useNavigate();
    const location = useLocation();
    const from = (location.state as RedirectLocationState | null)?.from?.pathname || "/";

    const [login, setLogin] = useState("");
    const [password, setPassword] = useState("");
    const [rememberMe, setRememberMe] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isDeletedAccountError, setIsDeletedAccountError] = useState(false);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setError(null);
        setIsDeletedAccountError(false);
        setIsSubmitting(true);

        try {
            await loginUser({login, password, rememberMe});
            navigate(from, {replace: true});
        } catch (error: unknown) {
            const errorCode = getHttpErrorCode(error);

            setIsDeletedAccountError(errorCode === "Identity.AccountDeleted");
            setError(getErrorMessage(error, "Login failed"));
        } finally {
            setIsSubmitting(false);
        }
    };

    const recoveryHref = login.includes("@")
        ? `/recover-account?email=${encodeURIComponent(login.trim())}`
        : "/recover-account";

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Welcome back, <span>Overseer</span>
                        </h2>
                        <p className="auth-text">
                            Sign in to resume orchestrating your city simulation. Monitor
                            population, incidents and systems - all from a single control
                            panel.
                        </p>
                        <div className="auth-feature-list">
                            <div className="auth-feature">
                                <span className="auth-feature-dot"/>
                                <span>Real-time overview of your digital metropolis</span>
                            </div>
                            <div className="auth-feature">
                                <span className="auth-feature-dot"/>
                                <span>Trigger &amp; resolve incidents with one click</span>
                            </div>
                            <div className="auth-feature">
                                <span className="auth-feature-dot"/>
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
                        onClick={(event) => {
                            if (isSubmitting) {
                                event.preventDefault();
                            }
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
                            onChange={(event) => setLogin(event.target.value)}
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
                            onChange={(event) => setPassword(event.target.value)}
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
                                onChange={(event) => setRememberMe(event.target.checked)}
                                disabled={isSubmitting}
                            />
                            <span>Remember this device</span>
                        </label>
                        <Link
                            to="/forgot-password"
                            className={`auth-forgot ${isSubmitting ? "auth-link--disabled" : ""}`}
                            onClick={(event) => {
                                if (isSubmitting) {
                                    event.preventDefault();
                                }
                            }}
                        >
                            Forgot password?
                        </Link>
                    </div>

                    {error ? (
                        <div className="auth-error">
                            <div>{error}</div>
                            {isDeletedAccountError ? (
                                <div style={{marginTop: "0.45rem"}}>
                                    <Link to={recoveryHref}>Restore this account</Link>
                                </div>
                            ) : null}
                        </div>
                    ) : null}

                    <button className="auth-button" type="submit" disabled={isSubmitting}>
                        {isSubmitting ? (
                            <span className="auth-spinner" aria-hidden="true"/>
                        ) : null}
                        <span>{isSubmitting ? "Logging in..." : "Login"}</span>
                    </button>
                </form>

                <div className="auth-switch">
                    Don&apos;t have an account?{" "}
                    <Link
                        to="/register"
                        className={isSubmitting ? "auth-link--disabled" : ""}
                        onClick={(event) => {
                            if (isSubmitting) {
                                event.preventDefault();
                            }
                        }}
                    >
                        Register
                    </Link>
                </div>
            </AuthCard>
        </AuthShell>
    );
};
