// src/services/identity/account/pages/user-settings/components/ProfileCard.tsx
import React, { useRef, useState } from "react";
import { updateAvatar } from "@services/identity/api/self/account/accountApi";
import "@services/identity/self/account/styles/profile-card.css";

type Props = {
  token: string | null;
  avatarUrl?: string;
  initialUsername: string;
  initialEmail: string;
  patchUser: (patch: any) => void;
};

const ProfileCard = ({
  token,
  avatarUrl,
  initialUsername,
  initialEmail,
  patchUser,
}: Props) => {
  const [displayName, setDisplayName] = useState(initialUsername);
  const [email, setEmail] = useState(initialEmail);

  const [avatarError, setAvatarError] = useState<string | null>(null);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);

  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [profileSaved, setProfileSaved] = useState(false);

  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const initial = (displayName || email || "O").charAt(0).toUpperCase();

  const simulateSave = () => {
    setIsSavingProfile(true);
    setProfileSaved(false);
    setTimeout(() => {
      setIsSavingProfile(false);
      setProfileSaved(true);
      setTimeout(() => setProfileSaved(false), 2000);
    }, 700);
  };

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    simulateSave();
  };

  const handleAvatarClick = () => fileInputRef.current?.click();

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

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
      patchUser({ avatarUrl: result.avatarUrl });
    } catch (err: any) {
      console.error(err);
      setAvatarError(
        err?.message || "Failed to upload avatar. Please try again."
      );
    } finally {
      setIsUploadingAvatar(false);
      e.target.value = "";
    }
  };

  return (
    <section className="settings-card settings-card--profile">
      <div className="settings-card-header">
        <div>
          <h2 className="settings-card-title">Profile</h2>
          <p className="settings-card-description">
            Update your display name, email and avatar used across the control
            panel.
          </p>
        </div>
      </div>

      <div className="settings-avatar-row">
        <button
          type="button"
          className="settings-avatar"
          onClick={handleAvatarClick}
          disabled={isUploadingAvatar}
        >
          {avatarUrl ? (
            <img
              src={avatarUrl}
              alt={displayName || email || "Avatar"}
              className="settings-avatar-image"
            />
          ) : (
            <span className="settings-avatar-initial">{initial}</span>
          )}
        </button>

        <input
          type="file"
          ref={fileInputRef}
          style={{ display: "none" }}
          accept="image/*"
          onChange={handleAvatarChange}
        />

        <div className="settings-avatar-text">
          <div className="settings-avatar-name">
            {displayName || "Overseer"}
          </div>
          <div className="settings-avatar-meta">
            {isUploadingAvatar
              ? "Uploading avatar..."
              : "Click to change avatar."}
          </div>
        </div>
      </div>

      {avatarError && <p className="settings-error-text">{avatarError}</p>}

      <form className="settings-form" onSubmit={handleProfileSubmit}>
        <div className="settings-field">
          <div className="settings-label-row">
            <label className="settings-label" htmlFor="displayName">
              Username
            </label>
            <span>Shown in the topbar and activity logs</span>
          </div>
          <input
            id="displayName"
            className="settings-input"
            type="text"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            placeholder="matrix_god"
          />
        </div>

        <div className="settings-field">
          <div className="settings-label-row">
            <label className="settings-label" htmlFor="email">
              Email
            </label>
            <span>Used for login and notifications</span>
          </div>
          <input
            id="email"
            className="settings-input"
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="you@example.com"
          />
        </div>

        <div className="settings-actions-row">
          {profileSaved && <span className="settings-save-badge">Saved</span>}
          <button
            type="submit"
            className="settings-button"
            disabled={isSavingProfile}
          >
            {isSavingProfile ? "Saving..." : "Save profile"}
          </button>
        </div>
      </form>
    </section>
  );
};

export default ProfileCard;
