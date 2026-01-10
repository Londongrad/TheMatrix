// src/services/identity/self/account/security/components/SecurityCard.tsx
import { usePasswordChange } from "../hooks/usePasswordChange";
import { RequirePermission } from "@shared/permissions/RequirePermission";
import { PermissionKeys } from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/security/styles/security-card.css";

type Props = { token: string | null };

const SecurityCard = ({ token }: Props) => {
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
