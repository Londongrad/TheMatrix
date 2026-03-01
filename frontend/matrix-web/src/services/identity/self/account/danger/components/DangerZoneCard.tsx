import {useState} from "react";
import {useNavigate} from "react-router-dom";
import {deleteAccount} from "@services/identity/api/self/account/accountApi";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import DeleteAccountDialog from "@services/identity/self/account/danger/components/DeleteAccountDialog";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "@services/identity/self/account/danger/styles/danger-zone.css";

type Props = {
    token: string | null;
};

const DangerZoneCard = ({token}: Props) => {
    const {logout} = useAuth();
    const navigate = useNavigate();
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

            await deleteAccount({currentPassword: password});
            setIsDeleteDialogOpen(false);

            await logout();
            navigate("/login", {replace: true});
        } catch (error) {
            console.error(error);
            setDeleteError(
                error instanceof Error
                    ? error.message
                    : "Failed to delete account. Please check your password.",
            );
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
                        High-impact account actions that disable access and require recovery support.
                    </p>
                </div>
            </div>

            <p className="settings-danger-text">
                Soft delete disables sign-in for this account, revokes active sessions,
                and keeps the identity reserved so it can be restored later if needed.
            </p>

            {!token && (
                <p className="settings-muted" style={{marginTop: "0.6rem"}}>
                    Log in to delete your account.
                </p>
            )}

            <div className="settings-actions-row settings-actions-row--end">
                <RequirePermission
                    perm={PermissionKeys.IdentityMeAccountDelete}
                    displayMode="disable"
                >
                    <button
                        type="button"
                        className="settings-button settings-button--danger"
                        onClick={handleDeleteAccountClick}
                        disabled={!token}
                    >
                        Delete account
                    </button>
                </RequirePermission>
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
