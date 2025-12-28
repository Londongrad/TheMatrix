// src/services/identity/account/pages/user-settings/components/DangerZoneCard.tsx
import { useState } from "react";
import DeleteAccountDialog from "@services/identity/account/components/DeleteAccountDialog";

type Props = {
  token: string | null;
  // опционально: если хочешь позже дернуть реальный API удаления
  // onDeleteAccount?: (password: string) => Promise<void>;
};

const DangerZoneCard = ({ token }: Props) => {
  const [isDeleteDialogOpen, setIsDeleteDialogOpen] = useState(false);
  const [isDeletingAccount, setIsDeletingAccount] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);

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

      // TODO: здесь будет реальный delete-account API:
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

  return (
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
        This action is irreversible. In this prototype it&apos;s only a stub and
        does not actually remove data from the backend yet.
      </p>

      {!token && (
        <p className="settings-muted" style={{ marginTop: "0.6rem" }}>
          Log in to delete your account.
        </p>
      )}

      <div className="settings-actions-row settings-actions-row--end">
        <button
          type="button"
          className="settings-button settings-button--danger"
          onClick={handleDeleteAccountClick}
          disabled={!token}
        >
          Delete account
        </button>
      </div>

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
    </section>
  );
};

export default DangerZoneCard;
