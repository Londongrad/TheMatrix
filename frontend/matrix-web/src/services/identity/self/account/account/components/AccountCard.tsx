import {useEffect, useMemo, useState} from "react";
import {changeUsername} from "@services/identity/api/self/account/accountApi";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {getErrorMessage} from "@shared/lib/errors/getErrorMessage";
import "@services/identity/self/account/account/styles/account-card.css";

type Props = {
    userId: string;
    username: string;
    pendingEmail: string | null;
    isEmailConfirmed: boolean;
    createdAtUtc: string;
    emailConfirmedAtUtc: string | null;
    patchUser: (patch: Partial<ProfileResponse>) => void;
};

const AccountCard = ({
                         userId,
                         username,
                         pendingEmail,
                         isEmailConfirmed,
                         createdAtUtc,
                         emailConfirmedAtUtc,
                         patchUser,
                     }: Props) => {
    const [draftUsername, setDraftUsername] = useState(username);
    const [currentPassword, setCurrentPassword] = useState("");
    const [isSaving, setIsSaving] = useState(false);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [saved, setSaved] = useState(false);
    const [copiedAccountId, setCopiedAccountId] = useState(false);

    useEffect(() => {
        setDraftUsername(username);
    }, [username]);

    useEffect(() => {
        if (!copiedAccountId) {
            return;
        }

        const timeoutId = window.setTimeout(() => {
            setCopiedAccountId(false);
        }, 1800);

        return () => window.clearTimeout(timeoutId);
    }, [copiedAccountId]);

    const normalizedDraft = useMemo(() => draftUsername.trim(), [draftUsername]);
    const hasUsernameChanged = normalizedDraft !== username;
    const usernameUpdateButtonClassName = hasUsernameChanged && !!currentPassword && !isSaving
        ? "settings-button settings-button--prompt-edit"
        : "settings-button";
    const emailStateLabel = pendingEmail
        ? "Pending change"
        : isEmailConfirmed
            ? "Confirmed"
            : "Needs confirmation";
    const emailStateDescription = pendingEmail
        ? "A replacement email is waiting for confirmation in Security."
        : isEmailConfirmed
            ? "Email sign-in and recovery are active."
            : "Email sign-in exists, but verification still needs attention in Security.";
    const formatUtc = (value?: string | null) => {
        if (!value) {
            return "--";
        }

        const date = new Date(value);
        return Number.isNaN(date.getTime())
            ? value
            : date.toLocaleString();
    };

    const copyAccountId = async () => {
        if (!userId || !navigator.clipboard?.writeText) {
            return;
        }

        try {
            await navigator.clipboard.writeText(userId);
            setCopiedAccountId(true);
        } catch {
            setCopiedAccountId(false);
        }
    };

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!hasUsernameChanged) {
            return;
        }

        try {
            setSaveError(null);
            setSaved(false);
            setIsSaving(true);

            const result = await changeUsername({
                username: normalizedDraft,
                currentPassword,
            });
            patchUser({username: result.username});
            setDraftUsername(result.username);
            setCurrentPassword("");
            setSaved(true);
        } catch (error: unknown) {
            setSaveError(getErrorMessage(error, "Failed to update username. Please try again."));
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <section className="settings-card settings-card--account">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Sign-in identity</h2>
                    <p className="settings-card-description">
                        Core sign-in identity for this operator account, without duplicating
                        personalization or recovery-email workflows.
                    </p>
                </div>
                <span className="settings-pill">
                    {isEmailConfirmed ? "Email confirmed" : "Email attention"}
                </span>
            </div>

            <form className="settings-form" onSubmit={handleSubmit}>
                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="accountUsername">
                            Username
                        </label>
                        <span>Primary login alias</span>
                    </div>
                    <input
                        id="accountUsername"
                        className="settings-input"
                        type="text"
                        value={draftUsername}
                        onChange={(event) => {
                            setDraftUsername(event.target.value);
                            setSaved(false);
                        }}
                        maxLength={16}
                        autoComplete="username"
                        placeholder="Your operator handle"
                    />
                </div>

                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="accountCurrentPassword">
                            Current password
                        </label>
                        <span>Required to confirm the change</span>
                    </div>
                    <input
                        id="accountCurrentPassword"
                        className="settings-input"
                        type="password"
                        value={currentPassword}
                        onChange={(event) => {
                            setCurrentPassword(event.target.value);
                            setSaved(false);
                        }}
                        autoComplete="current-password"
                        placeholder="********"
                    />
                </div>

                {saveError && <p className="settings-error-text">{saveError}</p>}

                <div className="settings-actions-row settings-actions-row--start">
                    {saved && <span className="settings-save-badge">Saved</span>}
                    <RequirePermission
                        perm={PermissionKeys.IdentityMeUsernameChange}
                        displayMode="disable"
                    >
                        <button
                            type="submit"
                            className={usernameUpdateButtonClassName}
                            disabled={!hasUsernameChanged || !currentPassword || isSaving}
                        >
                            {isSaving ? "Updating..." : "Update username"}
                        </button>
                    </RequirePermission>
                </div>
            </form>

            <div className="settings-account-grid">
                <article className="settings-account-panel">
                    <div className="settings-account-panel__header">
                        <div className="settings-account-panel__heading">
                            <span className="settings-label">Account ID</span>
                            <span className="settings-account-panel__caption">Stable support identifier</span>
                        </div>
                        <div className="settings-account-inline-actions">
                            {copiedAccountId && (
                                <span className="settings-save-badge">Copied</span>
                            )}
                            <button
                                type="button"
                                className="settings-button settings-button--secondary settings-button--small"
                                onClick={() => {
                                    void copyAccountId();
                                }}
                                disabled={!userId}
                            >
                                Copy ID
                            </button>
                        </div>
                    </div>
                    <div className="settings-account-value">
                        {userId || "--"}
                    </div>
                </article>

                <article className="settings-account-panel">
                    <div className="settings-account-panel__heading">
                        <span className="settings-label">Sign-in model</span>
                        <span className="settings-account-panel__caption">Identity policy</span>
                    </div>
                    <div className="settings-account-value">
                        Username + email sign-in
                    </div>
                    <div className="settings-account-panel__meta">
                        Username is managed here. Recovery email stays in Security because it
                        follows a dedicated confirmation flow instead of a direct overwrite.
                    </div>
                </article>

                <article className="settings-account-panel">
                    <div className="settings-account-panel__heading">
                        <span className="settings-label">Account created</span>
                        <span className="settings-account-panel__caption">Lifecycle origin</span>
                    </div>
                    <div className="settings-account-value">
                        {formatUtc(createdAtUtc)}
                    </div>
                    <div className="settings-account-panel__meta">
                        The operator account was first created at this time and keeps the same stable identifier
                        afterwards.
                    </div>
                </article>

                <article className="settings-account-panel">
                    <div className="settings-account-panel__heading">
                        <span className="settings-label">Email verification</span>
                        <span className="settings-account-panel__caption">Managed in Security</span>
                    </div>
                    <div className="settings-account-value">
                        {emailStateLabel}
                    </div>
                    <div className="settings-account-panel__meta">
                        {isEmailConfirmed && !pendingEmail
                            ? `Verified at ${formatUtc(emailConfirmedAtUtc)}.`
                            : emailStateDescription}
                    </div>
                </article>
            </div>

            <div className="settings-account-note">
                Username changes require your current password and are limited to once every 30
                days. This page stays focused on stable account identity, while Security owns
                recovery email and other confirmation-based flows.
            </div>
        </section>
    );
};

export default AccountCard;
