import React, {useState} from "react";
import {Link, useSearchParams} from "react-router-dom";
import {resetPassword} from "@services/identity/api/self/auth/authApi";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

export const ResetPasswordPage = () => {
    const [searchParams] = useSearchParams();
    const {logout, token} = useAuth();

    const userId = searchParams.get("userId");
    const resetToken = searchParams.get("token");

    const [newPassword, setNewPassword] = useState("");
    const [confirmNewPassword, setConfirmNewPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [success, setSuccess] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError(null);
        setSuccess(null);

        if (!userId || !resetToken) {
            setError("This reset link is incomplete or invalid.");
            return;
        }

        if (!newPassword || !confirmNewPassword) {
            setError("Please fill in both password fields.");
            return;
        }

        if (newPassword !== confirmNewPassword) {
            setError("New password and confirmation do not match.");
            return;
        }

        try {
            setIsSubmitting(true);

            await resetPassword({
                userId,
                token: resetToken,
                newPassword,
                confirmNewPassword,
            });

            if (token) {
                await logout();
            }

            setSuccess("Password reset completed. You can sign in with the new password.");
            setNewPassword("");
            setConfirmNewPassword("");
        } catch (err: any) {
            setError(err?.message || "Failed to reset password.");
        } finally {
            setIsSubmitting(false);
        }
    };

    const isLinkValid = Boolean(userId && resetToken);

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Reset your <span>Matrix</span> credentials
                        </h2>
                        <p className="auth-text">
                            Use a strong new password so the account can safely rejoin the
                            simulation network.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Reset password</h1>
                <p className="auth-subtitle">
                    {isLinkValid
                        ? "Choose a new password for your account."
                        : "This reset link is incomplete or invalid."}
                </p>

                <form className="auth-form" onSubmit={handleSubmit}>
                    <div className="auth-field">
                        <div className="auth-label-row">
                            <span className="auth-label">New password</span>
                        </div>
                        <input
                            className="auth-input"
                            type="password"
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            placeholder="вЂўвЂўвЂўвЂўвЂўвЂўвЂўвЂў"
                            required
                            disabled={isSubmitting || !isLinkValid}
                        />
                    </div>

                    <div className="auth-field">
                        <div className="auth-label-row">
                            <span className="auth-label">Confirm new password</span>
                        </div>
                        <input
                            className="auth-input"
                            type="password"
                            value={confirmNewPassword}
                            onChange={(e) => setConfirmNewPassword(e.target.value)}
                            placeholder="вЂўвЂўвЂўвЂўвЂўвЂўвЂўвЂў"
                            required
                            disabled={isSubmitting || !isLinkValid}
                        />
                    </div>

                    {error && <div className="auth-error">{error}</div>}
                    {success && <div className="auth-success">{success}</div>}

                    <button
                        className="auth-button"
                        type="submit"
                        disabled={isSubmitting || !isLinkValid}
                    >
                        {isSubmitting && (
                            <span className="auth-spinner" aria-hidden="true"/>
                        )}
                        <span>
                            {isSubmitting ? "Resetting..." : "Save new password"}
                        </span>
                    </button>
                </form>

                <div className="auth-switch">
                    Remembered it already? <Link to="/login">Back to login</Link>
                </div>
            </AuthCard>
        </AuthShell>
    );
};
