import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import {IconLock, IconOpen, IconUnlock} from "@shared/ui/icons/icons";
import type {UserListItemResponse} from "@services/identity/api/admin/adminTypes";
import {RequirePermission, RequirePermissions} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import {
    formatAdminRelativeVisit,
    formatAdminUtc,
    formatAdminVisitUtc,
} from "@services/identity/admin/shared/utils/dateTime";
import UserBadge from "./UserBadge";

export default function UserCard({
                                     user,
                                     onOpenAccess,
                                     onToggleLock,
                                     onRestore,
                                     isLoading,
                                 }: {
    user: UserListItemResponse;
    onOpenAccess: (id: string) => void;
    onToggleLock: (user: UserListItemResponse) => void;
    onRestore: (user: UserListItemResponse) => void;
    isLoading: boolean;
}) {
    const {user: currentUser} = useAuth();

    const avatarLabel = user.username?.[0]?.toUpperCase() ?? "U";
    const avatarUrl = user.avatarUrl ?? "";
    const togglePermission = user.isLocked
        ? PermissionKeys.IdentityUsersUnlock
        : PermissionKeys.IdentityUsersLock;
    const isCurrentUser = currentUser?.userId === user.id;
    const toggleTitle = isCurrentUser
        ? "You cannot lock your own account"
        : user.isLocked
            ? "Unlock"
            : "Lock";

    return (
        <div className="mx-admin-users__card" role="listitem">
            <div className="mx-admin-users__cardTop">
                <div className="mx-admin-users__avatar">
                    {avatarUrl ? (
                        <img
                            className="mx-admin-users__avatarImage"
                            src={avatarUrl}
                            alt={`${user.username} avatar`}
                            loading="lazy"
                        />
                    ) : (
                        avatarLabel
                    )}
                </div>
                <div className="mx-admin-users__meta">
                    <div className="mx-admin-users__username">{user.username}</div>
                    <div className="mx-admin-users__email">{user.email}</div>
                    <div className="mx-admin-users__id">{user.id}</div>
                </div>
                <div className="mx-admin-users__status">
                    {user.isDeleted ? (
                        <UserBadge kind="bad">Deleted</UserBadge>
                    ) : (
                        <UserBadge kind="ok">Active</UserBadge>
                    )}
                    {user.isLocked ? <UserBadge kind="warn">Locked</UserBadge> : null}
                    {user.isEmailConfirmed ? (
                        <UserBadge kind="ok">Email confirmed</UserBadge>
                    ) : (
                        <UserBadge kind="warn">Email pending</UserBadge>
                    )}
                </div>
            </div>

            <div className="mx-admin-users__cardRow">
                <span className="mx-admin-users__muted">Created</span>
                <span>{formatAdminUtc(user.createdAtUtc)}</span>
            </div>

            <div className="mx-admin-users__cardRow">
                <span className="mx-admin-users__muted">Last visit</span>
                <span
                    className="mx-admin-users__time"
                    title={formatAdminVisitUtc(user.lastVisitedAtUtc)}
                >
                    <span className="mx-admin-users__timePrimary">
                        {formatAdminRelativeVisit(user.lastVisitedAtUtc)}
                    </span>
                    {user.lastVisitedAtUtc ? (
                        <span className="mx-admin-users__timeSecondary">
                            {formatAdminVisitUtc(user.lastVisitedAtUtc)}
                        </span>
                    ) : null}
                </span>
            </div>

            <div className="mx-admin-users__actions">
                <RequirePermissions
                    perms={[
                        PermissionKeys.IdentityUserRolesRead,
                        PermissionKeys.IdentityUserPermissionsRead,
                    ]}
                    displayMode="disable"
                    permissionMatchMode="any"
                >
                    <Button size="sm" onClick={() => onOpenAccess(user.id)}>
                        <IconOpen/> Open access
                    </Button>
                </RequirePermissions>

                {user.isDeleted ? (
                    <RequirePermission
                        perm={PermissionKeys.IdentityUsersRestore}
                        displayMode="disable"
                    >
                        <Button
                            size="sm"
                            variant="success"
                            onClick={() => void onRestore(user)}
                            disabled={isLoading || isCurrentUser}
                        >
                            Restore
                        </Button>
                    </RequirePermission>
                ) : (
                    <RequirePermission perm={togglePermission} displayMode="disable">
                        <IconButton
                            variant={user.isLocked ? "default" : "danger"}
                            title={toggleTitle}
                            onClick={() => void onToggleLock(user)}
                            disabled={isLoading || isCurrentUser}
                        >
                            {user.isLocked ? <IconUnlock/> : <IconLock/>}
                        </IconButton>
                    </RequirePermission>
                )}
            </div>
        </div>
    );
}
