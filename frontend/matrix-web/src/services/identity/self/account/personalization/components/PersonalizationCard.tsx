import React, {useEffect, useRef, useState} from "react";
import {
    clearAvatar,
    updateAvatar,
} from "@services/identity/api/self/account/accountApi";
import type {ProfileResponse} from "@services/identity/api/self/account/accountTypes";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/personalization/styles/personalization-card.css";

type Props = {
    token: string | null;
    avatarUrl?: string;
    username: string;
    email: string;
    patchUser: (patch: Partial<ProfileResponse>) => void;
};

const PersonalizationCard = ({
    token,
    avatarUrl,
    username,
    email,
    patchUser,
}: Props) => {
    const [avatarError, setAvatarError] = useState<string | null>(null);
    const [avatarNotice, setAvatarNotice] = useState<string | null>(null);
    const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
    const [isClearingAvatar, setIsClearingAvatar] = useState(false);
    const [selectedAvatarFile, setSelectedAvatarFile] = useState<File | null>(null);
    const [previewUrl, setPreviewUrl] = useState<string | null>(null);

    const fileInputRef = useRef<HTMLInputElement | null>(null);
    const initial = (username || email || "O").charAt(0).toUpperCase();
    const activeAvatarUrl = previewUrl ?? avatarUrl;
    const hasPendingSelection = selectedAvatarFile !== null;

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
        if (!token) {
            setAvatarError("You are not authenticated.");
            return;
        }

        if (!selectedAvatarFile) {
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
        if (!token) {
            setAvatarError("You are not authenticated.");
            return;
        }

        if (!avatarUrl) {
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

    return (
        <section className="settings-card settings-card--personalization">
            <div className="settings-card-header">
                <div>
                    <h2 className="settings-card-title">Avatar</h2>
                    <p className="settings-card-description">
                        Update the image that represents this account in the topbar and across the console.
                    </p>
                </div>
            </div>

            <div className="settings-avatar-row">
                <RequirePermission
                    perm={PermissionKeys.IdentityMeAvatarChange}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-avatar"
                        onClick={handleAvatarClick}
                        disabled={isUploadingAvatar || isClearingAvatar}
                    >
                        {activeAvatarUrl ? (
                            <img
                                src={activeAvatarUrl}
                                alt={username || email || "Avatar"}
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
                    <div className="settings-avatar-name">
                        {username || "Overseer"}
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
                    {selectedAvatarFile && (
                        <div className="settings-avatar-selection">
                            <span className="settings-pill settings-pill--accent">
                                Preview
                            </span>
                            <span>{selectedAvatarFile.name}</span>
                            <span>{formatFileSize(selectedAvatarFile.size)}</span>
                        </div>
                    )}
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
                        disabled={isUploadingAvatar || isClearingAvatar}
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
                Personalization changes stay local until you apply them. Clearing the avatar falls back to your account initial.
            </p>
        </section>
    );
};

export default PersonalizationCard;
