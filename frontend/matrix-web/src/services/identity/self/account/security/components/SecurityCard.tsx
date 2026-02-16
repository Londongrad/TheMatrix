// src/services/identity/self/account/security/components/SecurityCard.tsx
import {useState} from "react";
import {sendEmailConfirmationEmail} from "@services/identity/api/self/auth/authApi";
import {usePasswordChange} from "../hooks/usePasswordChange";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/security/styles/security-card.css";

type Props = {
    token: string | null;
    email: string;
    isEmailConfirmed: boolean;
    emailConfirmationRequested: boolean;
};

const SecurityCard = ({
    token,
    email,
    isEmailConfirmed,
    emailConfirmationRequested,
}: Props) => {
    const {
        currentPassword,
        setCurrentPassword,
        newPassword,
        setNewPassword,
        confirmNewPassword,
        setConfirmNewPassword,
        securityError,
        isSavingSecurity,
        securitySaved,
        submit,
    } = usePasswordChange(token);

    const [isSendingConfirmation, setIsSendingConfirmation] = useState(false);
    const [confirmationNotice, setConfirmationNotice] = useState<string | null>(
        emailConfirmationRequested && !isEmailConfirmed
            ? "Verification email requested. Check your inbox and spam folder."
            : null,
    );
    const [confirmationError, setConfirmationError] = useState<string | null>(null);

    const resendConfirmation = async () => {
        if (!email || isEmailConfirmed) return;

        try {
            setConfirmationError(null);
            setIsSendingConfirmation(true);

            await sendEmailConfirmationEmail({email});
            setConfirmationNotice(
                "Verification email sent. Check your inbox and spam folder.",
            );
        } catch (err: any) {
            setConfirmationError(
                err?.message || "Failed to send verification email. Please try again.",
            );
        } finally {
            setIsSendingConfirmation(false);
        }
    };

    return (
        <section className="settings-card settings-card--security">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Security</h2>
                    <p className="settings-card-description">
                        Change your password to keep your simulation safe from intruders.
                    </p>
                </div>
            </div>

            <div className="settings-form" style={{marginBottom: "1.5rem"}}>
                <div className="settings-field">
                    <div className="settings-label-row">
                        <span className="settings-label">Email verification</span>
                        <span>{email || "No email loaded"}</span>
                    </div>
                    <p className="settings-card-description">
                        {isEmailConfirmed
                            ? "Your email is confirmed and ready for recovery flows."
                            : "Your email is not confirmed yet. Confirm it to make recovery flows safer and more predictable."}
                    </p>
                </div>

                {confirmationNotice && (
                    <p className="settings-save-badge">{confirmationNotice}</p>
                )}

                {confirmationError && (
                    <p className="settings-error-text">{confirmationError}</p>
                )}

                {!isEmailConfirmed && (
                    <div className="settings-actions-row">
                        <button
                            type="button"
                            className="settings-button"
                            disabled={isSendingConfirmation || !email}
                            onClick={() => {
                                void resendConfirmation();
                            }}
                        >
                            {isSendingConfirmation
                                ? "Sending..."
                                : "Send verification email"}
                        </button>
                    </div>
                )}
            </div>

            <form className="settings-form" onSubmit={submit}>
                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="currentPassword">
                            Current password
                        </label>
                    </div>
                    <input
                        id="currentPassword"
                        className="settings-input"
                        type="password"
                        value={currentPassword}
                        onChange={(e) => setCurrentPassword(e.target.value)}
                        placeholder="••••••••"
                    />
                </div>

                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="newPassword">
                            New password
                        </label>
                    </div>
                    <input
                        id="newPassword"
                        className="settings-input"
                        type="password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        placeholder="••••••••"
                    />
                </div>

                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="confirmNewPassword">
                            Confirm new password
                        </label>
                    </div>
                    <input
                        id="confirmNewPassword"
                        className="settings-input"
                        type="password"
                        value={confirmNewPassword}
                        onChange={(e) => setConfirmNewPassword(e.target.value)}
                        placeholder="••••••••"
                    />
                </div>

                {securityError && (
                    <p className="settings-error-text">{securityError}</p>
                )}

                <div className="settings-actions-row">
                    {securitySaved && <span className="settings-save-badge">Saved</span>}
                    <RequirePermission
                        perm={PermissionKeys.IdentityMePasswordChange}
                        mode="disable"
                    >
                        <button
                            type="submit"
                            className="settings-button"
                            disabled={isSavingSecurity}
                        >
                            {isSavingSecurity ? "Updating..." : "Update password"}
                        </button>
                    </RequirePermission>
                </div>
            </form>
        </section>
    );
};

export default SecurityCard;
