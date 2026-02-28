import {useEffect, useMemo, useState} from "react";
import {changeUsername} from "@services/identity/api/self/account/accountApi";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/account/styles/account-card.css";

type Props = {
    username: string;
    email: string;
    isEmailConfirmed: boolean;
    patchUser: (patch: Partial<ProfileResponse>) => void;
};

const AccountCard = ({
    username,
    email,
    isEmailConfirmed,
    patchUser,
}: Props) => {
    const [draftUsername, setDraftUsername] = useState(username);
    const [isSaving, setIsSaving] = useState(false);
    const [saveError, setSaveError] = useState<string | null>(null);
    const [saved, setSaved] = useState(false);

    useEffect(() => {
        setDraftUsername(username);
    }, [username]);

    const normalizedDraft = useMemo(() => draftUsername.trim(), [draftUsername]);
    const hasUsernameChanged = normalizedDraft !== username;

    const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        if (!hasUsernameChanged) {
            return;
        }

        try {
            setSaveError(null);
            setSaved(false);
            setIsSaving(true);

            const result = await changeUsername({username: normalizedDraft});
            patchUser({username: result.username});
            setDraftUsername(result.username);
            setSaved(true);
        } catch (error: any) {
            setSaveError(
                error?.message || "Failed to update username. Please try again.",
            );
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <section className="settings-card settings-card--account">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Account identity</h2>
                    <p className="settings-card-description">
                        Core login and recovery identifiers for this operator account.
                    </p>
                </div>
                <span className="settings-pill">
                    {isEmailConfirmed ? "Email confirmed" : "Email pending"}
                </span>
            </div>

            <form className="settings-form" onSubmit={handleSubmit}>
                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="accountUsername">
                            Username
                        </label>
                        <span>Login alias</span>
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

                {saveError && <p className="settings-error-text">{saveError}</p>}

                <div className="settings-actions-row settings-actions-row--start">
                    {saved && <span className="settings-save-badge">Saved</span>}
                    <RequirePermission
                        perm={PermissionKeys.IdentityMeUsernameChange}
                        displayMode="disable"
                    >
                        <button
                            type="submit"
                            className="settings-button"
                            disabled={!hasUsernameChanged || isSaving}
                        >
                            {isSaving ? "Updating..." : "Update username"}
                        </button>
                    </RequirePermission>
                </div>
            </form>

            <div className="settings-account-grid">
                <article className="settings-account-panel">
                    <div className="settings-label-row">
                        <span className="settings-label">Email</span>
                        <span>Recovery and verification</span>
                    </div>
                    <div className="settings-account-value">
                        {email || "--"}
                    </div>
                </article>
            </div>

            <div className="settings-account-note">
                Email stays read-only for now. It should move through a dedicated confirmation
                flow instead of a direct overwrite form.
            </div>
        </section>
    );
};

export default AccountCard;
