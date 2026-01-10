import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import { IconLock, IconOpen, IconUnlock } from "@shared/ui/icons/icons";
import type { UserListItemResponse } from "@services/identity/api/admin/adminTypes";
import {
  RequirePermission,
  RequirePermissions,
} from "@shared/permissions/RequirePermission";
import { PermissionKeys } from "@shared/permissions/permissionKeys";
import UserBadge from "./UserBadge";

function formatUtc(utc: string) {
  return utc?.replace("T", " ").replace("Z", "");
}

export default function UserCard({
  user,
  onOpenAccess,
  onToggleLock,
  isLoading,
}: {
  user: UserListItemResponse;
  onOpenAccess: (id: string) => void;
  onToggleLock: (user: UserListItemResponse) => void;
  isLoading: boolean;
}) {
  const avatarLabel = user.username?.[0]?.toUpperCase() ?? "U";
  const avatarUrl = user.avatarUrl ?? "";
  const togglePermission = user.isLocked
    ? PermissionKeys.IdentityUsersUnlock
    : PermissionKeys.IdentityUsersLock;

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
          {user.isLocked ? (
            <UserBadge kind="bad">Locked</UserBadge>
          ) : (
            <UserBadge kind="ok">Active</UserBadge>
          )}
          {user.isEmailConfirmed ? (
            <UserBadge kind="ok">Email confirmed</UserBadge>
          ) : (
            <UserBadge kind="warn">Email pending</UserBadge>
          )}
        </div>
      </div>

      <div className="mx-admin-users__cardRow">
        <span className="mx-admin-users__muted">Created</span>
        <span>{formatUtc(user.createdAtUtc)}</span>
      </div>

      <div className="mx-admin-users__actions">
        <RequirePermissions
          perms={[
            PermissionKeys.IdentityUserRolesRead,
            PermissionKeys.IdentityUserPermissionsRead,
          ]}
          mode="disable"
          match="any"
        >
          <Button size="sm" onClick={() => onOpenAccess(user.id)}>
            <IconOpen /> Open access
          </Button>
        </RequirePermissions>
        <RequirePermission perm={togglePermission} mode="disable">
          <IconButton
            variant={user.isLocked ? "default" : "danger"}
            title={user.isLocked ? "Unlock" : "Lock"}
            onClick={() => void onToggleLock(user)}
            disabled={isLoading}
          >
            {user.isLocked ? <IconUnlock /> : <IconLock />}
          </IconButton>
        </RequirePermission>
      </div>
    </div>
  );
}
