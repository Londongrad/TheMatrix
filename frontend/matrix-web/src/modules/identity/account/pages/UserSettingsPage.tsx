import React, { useState, useRef } from "react";
import { useAuth } from "@api/identity/auth/AuthContext";
import { changePassword, updateAvatar } from "@api/identity/account/accountApi";
import { useConfirm } from "@modules/shared/components/ConfirmDialog";
import DeleteAccountDialog from "../components/DeleteAccountDialog";
import "@styles/identity/account/user-settings-page.css";

const UserSettingsPage = () => {
  const { user, token, reloadMe, patchUser } = useAuth();
  const confirm = useConfirm();

  const typedUser = user as any;
  const avatarUrl: string | undefined = typedUser?.avatarUrl;

  const [displayName, setDisplayName] = useState<string>(
    typedUser?.username ?? ""
  );
  const [securityError, setSecurityError] = useState<string | null>(null);
  const [avatarError, setAvatarError] = useState<string | null>(null);
  const [isUploadingAvatar, setIsUploadingAvatar] = useState(false);
  const [email, setEmail] = useState<string>(typedUser?.email ?? "");
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmNewPassword, setConfirmNewPassword] = useState("");
  const [language, setLanguage] = useState<"en" | "ru">("en");
  const [theme, setTheme] = useState<"dark" | "light">("dark");
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isSavingSecurity, setIsSavingSecurity] = useState(false);
  const [isSavingPreferences, setIsSavingPreferences] = useState(false);
  const [profileSaved, setProfileSaved] = useState(false);
  const [securitySaved, setSecuritySaved] = useState(false);
  const [preferencesSaved, setPreferencesSaved] = useState(false);

  // Окно удаления аккаунта
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isDeletingAccount, setIsDeletingAccount] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const initial = (displayName || email || "O").charAt(0).toUpperCase();

  const simulateSave = (
    setterFlag: (v: boolean) => void,
    setterLoading: (v: boolean) => void
  ) => {
    setterLoading(true);
    setterFlag(false);
    setTimeout(() => {
      setterLoading(false);
      setterFlag(true);
      setTimeout(() => setterFlag(false), 2000);
    }, 700);
  };

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: здесь потом будет вызов API обновления профиля
    simulateSave(setProfileSaved, setIsSavingProfile);
  };

  // ========== PASSWORD CHANGE =============
  const handleSecuritySubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    setSecurityError(null);

    if (!token) {
      setSecurityError("You are not authenticated.");
      return;
    }

    if (!currentPassword || !newPassword || !confirmNewPassword) {
      setSecurityError("Please fill in all password fields.");
      return;
    }

    if (newPassword !== confirmNewPassword) {
      setSecurityError("New password and confirmation do not match.");
      return;
    }

    try {
      setIsSavingSecurity(true);
      setSecuritySaved(false);

      await changePassword(
        {
          currentPassword,
          newPassword,
          confirmNewPassword,
        },
        token
      );

      setSecuritySaved(true);
      setCurrentPassword("");
      setNewPassword("");
      setConfirmNewPassword("");

      setTimeout(() => setSecuritySaved(false), 2000);
    } catch (err: any) {
      console.error(err);
      setSecurityError(
        err?.message || "Failed to change password. Please try again."
      );
    } finally {
      setIsSavingSecurity(false);
    }
  };

  const handlePreferencesSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // TODO: здесь потом можно будет дергать API / сохранять в localStorage
    simulateSave(setPreferencesSaved, setIsSavingPreferences);
  };

  const handleDeleteAccountClick = () => {
    setDeleteError(null);
    setIsDeleteDialogOpen(true);
  };

  const handleConfirmDeleteAccount = async (password: string) => {
    if (!token) {
      setDeleteError("You are not authenticated.");
      return;
    }

    try {
      setIsDeletingAccount(true);
      setDeleteError(null);

      // TODO: здесь будет реальный delete-account API, например:
      // await deleteAccount({ password }, token);
      console.log("Delete account with password:", password);

      // после успешного удаления:
      // await logout();
      // window.location.href = "/goodbye";
    } catch (err) {
      console.error(err);
      setDeleteError("Failed to delete account. Please check your password.");
    } finally {
      setIsDeletingAccount(false);
    }
  };

  // =============== AVATAR ==================
  const handleAvatarClick = () => {
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
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

      const result = await updateAvatar(file, token);

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
    <div className="user-settings-page">
      <div className="user-settings-header">
        <div>
          <h1 className="user-settings-title">User settings</h1>
          <p className="user-settings-subtitle">
            Adjust your Overseer identity, security and control panel
            preferences.
          </p>
        </div>
      </div>

      <div className="user-settings-grid">
        {/* Профиль */}
        <section className="settings-card">
          <div className="settings-card-header">
            <div>
              <h2 className="settings-card-title">Profile</h2>
              <p className="settings-card-description">
                Update your display name, email and avatar used across the
                control panel.
              </p>
            </div>
          </div>

          {/* Avatar */}
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

          {/* Username */}
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

            {/* Email */}
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
              {profileSaved && (
                <span className="settings-save-badge">Saved</span>
              )}
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

        {/* Безопасность / пароль */}
        <section className="settings-card">
          <div className="settings-card-header">
            <div>
              <h2 className="settings-card-title">Security</h2>
              <p className="settings-card-description">
                Change your password to keep your simulation safe from
                intruders.
              </p>
            </div>
          </div>

          <form className="settings-form" onSubmit={handleSecuritySubmit}>
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
              {securitySaved && (
                <span className="settings-save-badge">Saved</span>
              )}
              <button
                type="submit"
                className="settings-button"
                disabled={isSavingSecurity}
              >
                {isSavingSecurity ? "Updating..." : "Update password"}
              </button>
            </div>
          </form>
        </section>

        {/* Предпочтения: язык, тема */}
        <section className="settings-card">
          <div className="settings-card-header">
            <div>
              <h2 className="settings-card-title">Preferences</h2>
              <p className="settings-card-description">
                Customize your language and theme for the Matrix control panel.
              </p>
            </div>
          </div>

          <form className="settings-form" onSubmit={handlePreferencesSubmit}>
            <div className="settings-field">
              <div className="settings-label-row">
                <label className="settings-label" htmlFor="language">
                  Language
                </label>
                <span>Interface language</span>
              </div>
              <select
                id="language"
                className="settings-input settings-select"
                value={language}
                onChange={(e) => setLanguage(e.target.value as "en" | "ru")}
              >
                <option value="en">English (EN)</option>
                <option value="ru">Русский (RU)</option>
              </select>
            </div>

            <div className="settings-field">
              <div className="settings-label-row">
                <span className="settings-label">Theme</span>
                <span>Dark mode is currently active</span>
              </div>
              <label className="settings-switch">
                <input
                  type="checkbox"
                  checked={theme === "dark"}
                  onChange={(e) =>
                    setTheme(e.target.checked ? "dark" : "light")
                  }
                />
                <span className="settings-switch-track">
                  <span className="settings-switch-thumb" />
                </span>
                <span className="settings-switch-text">
                  {theme === "dark"
                    ? "Dark theme (default)"
                    : "Light theme (stub)"}
                </span>
              </label>
              <p className="settings-hint">
                Theme toggle is visual only for now. In the future it will
                switch the entire control panel between dark and light modes.
              </p>
            </div>

            <div className="settings-actions-row">
              {preferencesSaved && (
                <span className="settings-save-badge">Saved</span>
              )}
              <button
                type="submit"
                className="settings-button"
                disabled={isSavingPreferences}
              >
                {isSavingPreferences ? "Saving..." : "Save preferences"}
              </button>
            </div>
          </form>
        </section>

        {/* Опасная зона */}
        <section className="settings-card settings-card--danger">
          <div className="settings-card-header">
            <div>
              <h2 className="settings-card-title">Danger zone</h2>
              <p className="settings-card-description">
                Permanently delete your account and all associated simulations.
              </p>
            </div>
          </div>

          <p className="settings-danger-text">
            This action is irreversible. In this prototype it&apos;s only a stub
            and does not actually remove data from the backend yet.
          </p>

          <div className="settings-actions-row settings-actions-row--end">
            <button
              type="button"
              className="settings-button settings-button--danger"
              onClick={handleDeleteAccountClick}
            >
              Delete account
            </button>
          </div>
        </section>

        <DeleteAccountDialog
          open={isDeleteDialogOpen}
          isSubmitting={isDeletingAccount}
          error={deleteError}
          onClose={() => {
            if (!isDeletingAccount) {
              setIsDeleteDialogOpen(false);
              setDeleteError(null);
            }
          }}
          onConfirm={handleConfirmDeleteAccount}
        />
      </div>
    </div>
  );
};

export default UserSettingsPage;
