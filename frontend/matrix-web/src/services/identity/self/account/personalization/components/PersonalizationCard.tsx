import React, {useRef, useState} from "react";
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
    const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
    const [isClearingAvatar, setIsClearingAvatar] = useState(false);

    const fileInputRef = useRef<HTMLInputElement | null>(null);
    const initial = (username || email || "O").charAt(0).toUpperCase();

    const handleAvatarClick = () => fileInputRef.current?.click();

    const handleAvatarChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) {
            return;
        }

        if (!token) {
            setAvatarError("You are not authenticated.");
            return;
        }

        if (!file.type.startsWith("image/")) {
            setAvatarError("Please select an image file.");
            return;
        }

        if (file.size > 2 * 1024 * 1024) {
            setAvatarError("Maximum avatar size is 2 MB.");
            return;
        }

        try {
            setAvatarError(null);
            setIsUploadingAvatar(true);
            const result = await updateAvatar(file);
            patchUser({avatarUrl: result.avatarUrl});
        } catch (uploadError: any) {
            console.error(uploadError);
            setAvatarError(
                uploadError?.message || "Failed to upload avatar. Please try again.",
            );
        } finally {
            setIsUploadingAvatar(false);
            event.target.value = "";
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
            setIsClearingAvatar(true);
            const result = await clearAvatar();
            patchUser({avatarUrl: result.avatarUrl});
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
                        {avatarUrl ? (
                            <img
                                src={avatarUrl}
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
                                : "Choose a JPG, PNG, or WebP image up to 2 MB."}
                    </div>
                </div>
            </div>

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
                        {isUploadingAvatar ? "Uploading..." : "Upload new avatar"}
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
                        disabled={!avatarUrl || isUploadingAvatar || isClearingAvatar}
                    >
                        {isClearingAvatar ? "Clearing..." : "Clear avatar"}
                    </button>
                </RequirePermission>
            </div>

            <p className="settings-hint">
                Avatar changes now support both upload and explicit clearing through the same account command boundary.
            </p>
        </section>
    );
};

export default PersonalizationCard;
