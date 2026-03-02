import React, {useEffect, useMemo, useRef, useState} from "react";
import {
    changeDisplayName,
    clearAvatar,
    updateAvatar,
} from "@services/identity/api/self/account/accountApi";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import "@services/identity/self/account/personalization/styles/personalization-card.css";

type Props = {
    token: string | null;
    avatarUrl?: string;
    displayName?: string;
    username: string;
    email: string;
    patchUser: (patch: Partial<ProfileResponse>) => void;
};

const PersonalizationCard = ({
    token,
    avatarUrl,
    displayName,
    username,
    email,
    patchUser,
}: Props) => {
    const {can} = usePermissions();
    const canChangeDisplayName = can(PermissionKeys.IdentityMeDisplayNameChange);
    const canChangeAvatar = can(PermissionKeys.IdentityMeAvatarChange);

    const [avatarError, setAvatarError] = useState<string | null>(null);
    const [avatarNotice, setAvatarNotice] = useState<string | null>(null);
    const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
    const [isClearingAvatar, setIsClearingAvatar] = useState(false);
    const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);

    const [draftDisplayName, setDraftDisplayName] = useState(displayName ?? "");
    const [displayNameError, setDisplayNameError] = useState<string | null>(null);
    const [isSavingDisplayName, setIsSavingDisplayName] = useState(false);
    const [displayNameSaved, setDisplayNameSaved] = useState(false);

    const fileInputRef = useRef<HTMLInputElement | null>(null);

    const resolvedDisplayName =
        displayName?.trim() || username || email || "Overseer";
    const currentDisplayName = displayName?.trim() || null;
    const normalizedDraftDisplayName = useMemo(
        () => draftDisplayName.trim(),
        [draftDisplayName],
    );
    const effectiveDraftDisplayName =
        normalizedDraftDisplayName.length > 0 ? normalizedDraftDisplayName : null;

    const initial = resolvedDisplayName.charAt(0).toUpperCase();
    const activeAvatarUrl = previewUrl ?? avatarUrl;
    const hasPendingSelection = selectedAvatarFile !== null;
    const hasDisplayNameChanged = effectiveDraftDisplayName !== currentDisplayName;

    useEffect(() => {
        setDraftDisplayName(displayName ?? "");
    }, [displayName]);

    useEffect(() => {
        if (!displayNameSaved) {
            return undefined;
        }

        const timeoutId = window.setTimeout(() => {
            setDisplayNameSaved(false);
        }, 2200);

        return () => {
            window.clearTimeout(timeoutId);
        };
    }, [displayNameSaved]);

    useEffect(() => {
        return () => {
            if (previewUrl?.startsWith("blob:")) {
                URL.revokeObjectURL(previewUrl);
            }
        };
    }, [previewUrl]);

    const handleAvatarClick = () => fileInputRef.current?.click();

    const clearPendingSelection = () => {
        setSelectedAvatarFile(null);
        setPreviewUrl((currentPreviewUrl) => {
            if (currentPreviewUrl?.startsWith("blob:")) {
                URL.revokeObjectURL(currentPreviewUrl);
            }

            return null;
        });
    };

    const formatFileSize = (value: number) => {
        if (value < 1024) {
            return `${value} B`;
        }

        if (value < 1024 * 1024) {
            return `${(value / 1024).toFixed(1)} KB`;
        }

        return `${(value / (1024 * 1024)).toFixed(2)} MB`;
    };

    const handleAvatarChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) {
            return;
        }

        if (!token) {
            setAvatarError("You are not authenticated.");
            return;
        }

        const allowedTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
        if (!allowedTypes.has(file.type)) {
            setAvatarError("Use a JPG, PNG, or WebP image.");
            return;
        }

        if (file.size > 2 * 1024 * 1024) {
            setAvatarError("Maximum avatar size is 2 MB.");
            return;
        }

        try {
            setAvatarError(null);
            setAvatarNotice("Preview ready. Apply it when you are happy with the result.");
            clearPendingSelection();

            const objectUrl = URL.createObjectURL(file);
            setSelectedAvatarFile(file);
            setPreviewUrl(objectUrl);
        } finally {
            event.target.value = "";
        }
    };

    const handleApplyAvatar = async () => {
        if (!token || !selectedAvatarFile) {
            return;
        }

        try {
            setAvatarError(null);
            setAvatarNotice(null);
            setIsUploadingAvatar(true);
            const result = await updateAvatar(selectedAvatarFile);
            patchUser({avatarUrl: result.avatarUrl});
            clearPendingSelection();
            setAvatarNotice("Avatar updated. The new image is now active across the console.");
        } catch (uploadError: any) {
            console.error(uploadError);
            setAvatarError(
                uploadError?.message || "Failed to upload avatar. Please try again.",
            );
        } finally {
            setIsUploadingAvatar(false);
        }
    };

    const handleClearAvatar = async () => {
        if (!token || !avatarUrl) {
            return;
        }

        try {
            setAvatarError(null);
            setAvatarNotice(null);
            setIsClearingAvatar(true);
            const result = await clearAvatar();
            patchUser({avatarUrl: result.avatarUrl});
            clearPendingSelection();
            setAvatarNotice("Avatar cleared. Your fallback initial is active again.");
        } catch (clearError: any) {
            console.error(clearError);
            setAvatarError(
                clearError?.message || "Failed to clear avatar. Please try again.",
            );
        } finally {
            setIsClearingAvatar(false);
        }
    };

    const handleDisplayNameSubmit = async (
        event: React.FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        if (!hasDisplayNameChanged) {
            return;
        }

        try {
            setDisplayNameError(null);
            setDisplayNameSaved(false);
            setIsSavingDisplayName(true);

            const result = await changeDisplayName({
                displayName: effectiveDraftDisplayName,
            });

            patchUser({displayName: result.displayName});
            setDraftDisplayName(result.displayName ?? "");
            setDisplayNameSaved(true);
        } catch (error: any) {
            setDisplayNameError(
                error?.message || "Failed to update display name. Please try again.",
            );
        } finally {
            setIsSavingDisplayName(false);
        }
    };

    const handleClearDisplayName = async () => {
        if (!currentDisplayName) {
            setDraftDisplayName("");
            setDisplayNameSaved(false);
            setDisplayNameError(null);
            return;
        }

        try {
            setDisplayNameError(null);
            setDisplayNameSaved(false);
            setIsSavingDisplayName(true);

            const result = await changeDisplayName({displayName: null});
            patchUser({displayName: result.displayName});
            setDraftDisplayName("");
            setDisplayNameSaved(true);
        } catch (error: any) {
            setDisplayNameError(
                error?.message || "Failed to clear display name. Please try again.",
            );
        } finally {
            setIsSavingDisplayName(false);
        }
    };

    return (
        <section className="settings-card settings-card--personalization">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Identity presentation</h2>
                    <p className="settings-card-description">
                        Choose the public label and avatar shown across the console
                        without touching your login credentials.
                    </p>
                </div>
            </div>

            <form className="settings-form" onSubmit={handleDisplayNameSubmit}>
                <div className="settings-field">
                    <div className="settings-label-row">
                        <label className="settings-label" htmlFor="displayName">
                            Display name
                        </label>
                        <span>Shown in the topbar and user menu</span>
                    </div>
                    <input
                        id="displayName"
                        className="settings-input"
                        type="text"
                        value={draftDisplayName}
                        onChange={(event) => {
                            setDraftDisplayName(event.target.value);
                            setDisplayNameSaved(false);
                        }}
                        maxLength={64}
                        placeholder="How the console should address you"
                        disabled={!canChangeDisplayName || isSavingDisplayName}
                    />
                    <p className="settings-hint">
                        Leave it empty to fall back to your username. This changes presentation
                        only, not the login alias itself.
                    </p>
                </div>

                {displayNameError ? (
                    <p className="settings-error-text">{displayNameError}</p>
                ) : null}

                <div className="settings-actions-row settings-actions-row--start">
                    {displayNameSaved ? (
                        <span className="settings-save-badge">Saved</span>
                    ) : null}

                    <RequirePermission
                        perm={PermissionKeys.IdentityMeDisplayNameChange}
                        displayMode="disable"
                    >
                        <button
                            type="submit"
                            className="settings-button"
                            disabled={!hasDisplayNameChanged || isSavingDisplayName}
                        >
                            {isSavingDisplayName ? "Saving..." : "Save display name"}
                        </button>
                    </RequirePermission>

                    <RequirePermission
                        perm={PermissionKeys.IdentityMeDisplayNameChange}
                        displayMode="disable"
                    >
                        <button
                            type="button"
                            className="settings-button settings-button--secondary"
                            onClick={() => {
                                void handleClearDisplayName();
                            }}
                            disabled={!currentDisplayName || isSavingDisplayName}
                        >
                            {isSavingDisplayName ? "Clearing..." : "Use username fallback"}
                        </button>
                    </RequirePermission>
                </div>
            </form>

            <div className="settings-avatar-row">
                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-avatar"
                        onClick={handleAvatarClick}
                        disabled={!canChangeAvatar || isUploadingAvatar || isClearingAvatar}
                    >
                        {activeAvatarUrl ? (
                            <img
                                src={activeAvatarUrl}
                                alt={resolvedDisplayName || "Avatar"}
                                className="settings-avatar-image"
                            />
                        ) : (
                            <span className="settings-avatar-initial">{initial}</span>
                        )}
                    </button>
                </RequirePermission>

                <input
                    type="file"
                    ref={fileInputRef}
                    style={{display: "none"}}
                    accept="image/*"
                    onChange={handleAvatarChange}
                />

                <div className="settings-avatar-text">
                    <div className="settings-avatar-name">{resolvedDisplayName}</div>
                    <div className="settings-avatar-handle">
                        {username ? `@${username}` : email}
                    </div>
                    <div className="settings-avatar-meta">
                        {isUploadingAvatar
                            ? "Uploading avatar..."
                            : isClearingAvatar
                                ? "Clearing avatar..."
                                : hasPendingSelection
                                    ? "Preview is local until you apply it."
                                    : "Choose a JPG, PNG, or WebP image up to 2 MB."}
                    </div>
                    {selectedAvatarFile ? (
                        <div className="settings-avatar-selection">
                            <span className="settings-pill settings-pill--accent">
                                Preview
                            </span>
                            <span>{selectedAvatarFile.name}</span>
                            <span>{formatFileSize(selectedAvatarFile.size)}</span>
                        </div>
                    ) : null}
                </div>
            </div>

            {avatarNotice ? <p className="settings-hint">{avatarNotice}</p> : null}
            {avatarError ? <p className="settings-error-text">{avatarError}</p> : null}

            <div className="settings-actions-row settings-actions-row--start">
                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-button"
                        onClick={handleAvatarClick}
                        disabled={!canChangeAvatar || isUploadingAvatar || isClearingAvatar}
                    >
                        {hasPendingSelection ? "Choose another image" : "Choose image"}
                    </button>
                </RequirePermission>

                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-button settings-button--secondary"
                        onClick={() => {
                            void handleApplyAvatar();
                        }}
                        disabled={!hasPendingSelection || isUploadingAvatar || isClearingAvatar}
                    >
                        {isUploadingAvatar ? "Applying..." : "Apply avatar"}
                    </button>
                </RequirePermission>

                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-button settings-button--secondary"
                        onClick={clearPendingSelection}
                        disabled={!hasPendingSelection || isUploadingAvatar || isClearingAvatar}
                    >
                        Discard preview
                    </button>
                </RequirePermission>

                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-button settings-button--secondary"
                        onClick={() => {
                            void handleClearAvatar();
                        }}
                        disabled={!avatarUrl || hasPendingSelection || isUploadingAvatar || isClearingAvatar}
                    >
                        {isClearingAvatar ? "Clearing..." : "Clear avatar"}
                    </button>
                </RequirePermission>
            </div>

            <p className="settings-hint">
                Avatar changes stay local until you apply them. Clearing the avatar falls back to the
                current display label initial.
            </p>
        </section>
    );
};

export default PersonalizationCard;
