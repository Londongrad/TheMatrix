import React, {useMemo, useState} from "react";
import {Link, useSearchParams} from "react-router-dom";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";
import {requestAccountRecovery} from "@services/identity/api/self/auth/authApi";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";

export const RecoverAccountPage = () => {
    const [searchParams] = useSearchParams();
    const [email, setEmail] = useState(searchParams.get("email") ?? "");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [notice, setNotice] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const normalizedEmail = useMemo(() => email.trim(), [email]);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();
        setError(null);
        setIsSubmitting(true);

        try {
            await requestAccountRecovery({email: normalizedEmail});
            setNotice(
                "If a deleted account exists for this email, a recovery link will arrive shortly.",
            );
        } catch (error: unknown) {
            setError(getErrorMessage(error, "Failed to start account recovery. Please try again."));
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Restore your <span>Matrix</span> operator profile
                        </h2>
                        <p className="auth-text">
                            Soft-deleted accounts stay reserved for recovery. Confirm the request
                            from your inbox and you can sign in again with the same credentials.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Recover account</h1>
                <p className="auth-subtitle">
                    Enter the email of a soft-deleted account and we&apos;ll send a recovery link.
                </p>

                <form className="auth-form" onSubmit={handleSubmit}>
                    <div className="auth-field">
                        <div className="auth-label-row">
                            <span className="auth-label">Email</span>
                            <span>Must match the deleted account</span>
                        </div>
                        <input
                            className="auth-input"
                            type="email"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                            placeholder="you@example.com"
                            required
                            disabled={isSubmitting}
                        />
                    </div>

                    {notice && <div className="auth-success">{notice}</div>}
                    {error && <div className="auth-error">{error}</div>}

                    <button
                        className="auth-button"
                        type="submit"
                        disabled={isSubmitting || !normalizedEmail}
                    >
                        {isSubmitting && (
                            <span className="auth-spinner" aria-hidden="true"/>
                        )}
                        <span>{isSubmitting ? "Sending..." : "Send recovery link"}</span>
                    </button>
                </form>

                <div className="auth-switch">
                    Remembered your login? <Link to="/login">Back to login</Link>
                </div>
            </AuthCard>
        </AuthShell>
    );
};
