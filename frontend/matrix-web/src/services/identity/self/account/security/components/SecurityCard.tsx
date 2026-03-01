// src/services/identity/self/account/security/components/SecurityCard.tsx
import {useEffect, useState} from "react";
import {sendEmailConfirmationEmail} from "@services/identity/api/self/auth/authApi";
import {
    cancelPendingEmailChange,
    changeEmail,
    resendPendingEmailChange,
} from "@services/identity/api/self/account/accountApi";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {usePasswordChange} from "../hooks/usePasswordChange";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/security/styles/security-card.css";

type Props = {
    token: string | null;
    email: string;
    pendingEmail: string | null;
    isEmailConfirmed: boolean;
    emailConfirmationRequested: boolean;
    patchUser: (patch: Partial<ProfileResponse>) => void;
};

const SecurityCard = ({
    token,
    email,
    pendingEmail,
    isEmailConfirmed,
    emailConfirmationRequested,
    patchUser,
}: Props) => {
    const {
        currentPassword: passwordCurrentPassword,
        setCurrentPassword: setPasswordCurrentPassword,
        newPassword,
        setNewPassword,
        confirmNewPassword,
        setConfirmNewPassword,
        securityError,
        isSavingSecurity,
        securitySaved,
        submit,
    } = usePasswordChange(token);

    const [nextEmail, setNextEmail] = useState("");
    const [emailChangePassword, setEmailChangePassword] = useState("");
    const [isSavingEmailChange, setIsSavingEmailChange] = useState(false);
    const [isResendingPendingEmailChange, setIsResendingPendingEmailChange] = useState(false);
    const [isCancellingPendingEmailChange, setIsCancellingPendingEmailChange] = useState(false);
    const [emailChangeNotice, setEmailChangeNotice] = useState<string | null>(null);
    const [emailChangeError, setEmailChangeError] = useState<string | null>(null);
    const [isSendingConfirmation, setIsSendingConfirmation] = useState(false);
    const [confirmationNotice, setConfirmationNotice] = useState<string | null>(
        emailConfirmationRequested && !isEmailConfirmed
            ? "If your email still needs confirmation, use the latest message in your inbox or spam folder."
            : null,
    );
    const [confirmationError, setConfirmationError] = useState<string | null>(null);

    useEffect(() => {
        if (!pendingEmail) {
            return;
        }

        setNextEmail("");
        setEmailChangePassword("");
        setEmailChangeError(null);
        setEmailChangeNotice(
            `A confirmation link was sent to ${pendingEmail}. Your current email stays active until that link is used.`,
        );
    }, [pendingEmail]);

    const resendConfirmation = async () => {
        if (!email || isEmailConfirmed) return;

        try {
            setConfirmationError(null);
            setIsSendingConfirmation(true);

            await sendEmailConfirmationEmail({email});
            setConfirmationNotice(
                "If this email can receive a confirmation link, use the latest message in your inbox or spam folder.",
            );
        } catch (err: any) {
            setConfirmationError(
                err?.message || "Failed to send verification email. Please try again.",
            );
        } finally {
            setIsSendingConfirmation(false);
        }
    };

    const submitEmailChange = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const normalizedNextEmail = nextEmail.trim();
        if (!normalizedNextEmail || !emailChangePassword) {
            return;
        }

        try {
            setEmailChangeError(null);
            setEmailChangeNotice(null);
            setIsSavingEmailChange(true);

            const result = await changeEmail({
                newEmail: normalizedNextEmail,
                currentPassword: emailChangePassword,
            });

            patchUser({pendingEmail: result.pendingEmail});
            setNextEmail("");
            setEmailChangePassword("");
            setEmailChangeNotice(
                `Confirmation was sent to ${result.pendingEmail}. The current email will stay active until confirmation.`,
            );
        } catch (err: any) {
            setEmailChangeError(
                err?.message || "Failed to start the email change flow. Please try again.",
            );
        } finally {
            setIsSavingEmailChange(false);
        }
    };

    const resendPendingChange = async () => {
        if (!pendingEmail) {
            return;
        }

        try {
            setEmailChangeError(null);
            setEmailChangeNotice(null);
            setIsResendingPendingEmailChange(true);

            await resendPendingEmailChange();
            setEmailChangeNotice(
                `A fresh confirmation link was sent to ${pendingEmail}. Your current email stays active until confirmation.`,
            );
        } catch (err: any) {
            setEmailChangeError(
                err?.message || "Failed to resend the pending email confirmation.",
            );
        } finally {
            setIsResendingPendingEmailChange(false);
        }
    };

    const cancelPendingChange = async () => {
        if (!pendingEmail) {
            return;
        }

        try {
            setEmailChangeError(null);
            setEmailChangeNotice(null);
            setIsCancellingPendingEmailChange(true);

            await cancelPendingEmailChange();
            patchUser({pendingEmail: null});
            setNextEmail("");
            setEmailChangePassword("");
            setEmailChangeNotice(
                "Pending email change was cancelled. Your current email remains active.",
            );
        } catch (err: any) {
            setEmailChangeError(
                err?.message || "Failed to cancel the pending email change.",
            );
        } finally {
            setIsCancellingPendingEmailChange(false);
        }
    };

    return (
        <section className="settings-card settings-card--security">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Security</h2>
                    <p className="settings-card-description">
                        Keep login, recovery, and sensitive self-service flows under tighter control.
                    </p>
                </div>
            </div>

            {!isEmailConfirmed ? (
                <div className="settings-email-warning" role="alert">
                    <div className="settings-email-warning__content">
                        <div className="settings-email-warning__eyebrow">
                            Email verification required
                        </div>
                        <h3 className="settings-email-warning__title">
                            Confirm your email
                        </h3>
                        <p className="settings-email-warning__text">
                            Confirm <strong>{email || "your email address"}</strong> to keep password recovery and account security flows available.
                        </p>

                        {confirmationNotice && (
                            <p className="settings-email-warning__notice">
                                {confirmationNotice}
                            </p>
                        )}

                        {confirmationError && (
                            <p className="settings-error-text">{confirmationError}</p>
                        )}
                    </div>

                    <div className="settings-email-warning__actions">
                        <button
                            type="button"
                            className="settings-button settings-button--warning"
                            disabled={isSendingConfirmation || !email}
                            onClick={() => {
                                void resendConfirmation();
                            }}
                        >
                            {isSendingConfirmation ? "Sending..." : "Send verification email"}
                        </button>
                    </div>
                </div>
            ) : (
                <div className="settings-email-status">
                    <div>
                        <div className="settings-email-status__eyebrow">
                            Email verification
                        </div>
                        <p className="settings-email-status__text">
                            {email || "Your email"} is confirmed and ready for recovery flows.
                        </p>
                    </div>
                    <span className="settings-pill">Confirmed</span>
                </div>
            )}

            {pendingEmail && (
                <div className="settings-email-warning" role="status">
                    <div className="settings-email-warning__content">
                        <div className="settings-email-warning__eyebrow">
                            Pending email change
                        </div>
                        <h3 className="settings-email-warning__title">
                            New email is waiting for confirmation
                        </h3>
                        <p className="settings-email-warning__text">
                            <strong>{pendingEmail}</strong> still needs confirmation. Until then, <strong>{email || "your current email"}</strong> remains active for login recovery and notifications.
                        </p>
                    </div>

                    <div className="settings-email-warning__actions">
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeEmailChange}
                            displayMode="disable"
                        >
                            <button
                                type="button"
                                className="settings-button settings-button--secondary"
                                disabled={isResendingPendingEmailChange || isCancellingPendingEmailChange}
                                onClick={() => {
                                    void resendPendingChange();
                                }}
                            >
                                {isResendingPendingEmailChange ? "Sending..." : "Resend confirmation"}
                            </button>
                        </RequirePermission>
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeEmailChange}
                            displayMode="disable"
                        >
                            <button
                                type="button"
                                className="settings-button settings-button--ghost"
                                disabled={isCancellingPendingEmailChange || isResendingPendingEmailChange}
                                onClick={() => {
                                    void cancelPendingChange();
                                }}
                            >
                                {isCancellingPendingEmailChange ? "Cancelling..." : "Cancel change"}
                            </button>
                        </RequirePermission>
                        <span className="settings-pill">Pending</span>
                    </div>
                </div>
            )}

            <div className="settings-security-section">
                <div className="settings-security-section__header">
                    <div>
                        <h3 className="settings-security-section__title">Email change</h3>
                        <p className="settings-security-section__description">
                            Replace the recovery email through a confirmation link sent to the next address.
                        </p>
                    </div>
                </div>

                <form className="settings-form" onSubmit={submitEmailChange}>
                    <div className="settings-field">
                        <div className="settings-label-row">
                            <label className="settings-label" htmlFor="nextEmail">
                                New email
                            </label>
                            <span>
                                {pendingEmail
                                    ? "Submitting a different email replaces the current pending confirmation."
                                    : "Current email stays active until confirmation"}
                            </span>
                        </div>
                        <input
                            id="nextEmail"
                            className="settings-input"
                            type="email"
                            value={nextEmail}
                            onChange={(event) => {
                                setNextEmail(event.target.value);
                                setEmailChangeError(null);
                            }}
                            autoComplete="email"
                            placeholder="operator@matrix.example"
                        />
                    </div>

                    <div className="settings-field">
                        <div className="settings-label-row">
                            <label className="settings-label" htmlFor="emailChangePassword">
                                Current password
                            </label>
                            <span>Required to authorize the request</span>
                        </div>
                        <input
                            id="emailChangePassword"
                            className="settings-input"
                            type="password"
                            value={emailChangePassword}
                            onChange={(event) => {
                                setEmailChangePassword(event.target.value);
                                setEmailChangeError(null);
                            }}
                            autoComplete="current-password"
                            placeholder="********"
                        />
                    </div>

                    {emailChangeNotice && (
                        <p className="settings-hint">{emailChangeNotice}</p>
                    )}

                    {emailChangeError && (
                        <p className="settings-error-text">{emailChangeError}</p>
                    )}

                    <div className="settings-actions-row settings-actions-row--start">
                        <RequirePermission
                            perm={PermissionKeys.IdentityMeEmailChange}
                            displayMode="disable"
                        >
                            <button
                                type="submit"
                                className="settings-button"
                                disabled={
                                    !nextEmail.trim() ||
                                    !emailChangePassword ||
                                    isSavingEmailChange ||
                                    isResendingPendingEmailChange ||
                                    isCancellingPendingEmailChange
                                }
                            >
                                {isSavingEmailChange
                                    ? "Sending..."
                                    : pendingEmail
                                        ? "Replace pending email"
                                        : "Send email change link"}
                            </button>
                        </RequirePermission>
                    </div>
                </form>
            </div>

            <div className="settings-security-section">
                <div className="settings-security-section__header">
                    <div>
                        <h3 className="settings-security-section__title">Password</h3>
                        <p className="settings-security-section__description">
                            Change the account password used for sign-in and sensitive self-service actions.
                        </p>
                    </div>
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
                            value={passwordCurrentPassword}
                            onChange={(e) => setPasswordCurrentPassword(e.target.value)}
                            placeholder="********"
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
                            placeholder="********"
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
                            placeholder="********"
                        />
                    </div>

                    {securityError && (
                        <p className="settings-error-text">{securityError}</p>
                    )}

                    <div className="settings-actions-row">
                        {securitySaved && <span className="settings-save-badge">Saved</span>}
                        <RequirePermission
                            perm={PermissionKeys.IdentityMePasswordChange}
                            displayMode="disable"
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
            </div>
        </section>
    );
};

export default SecurityCard;
