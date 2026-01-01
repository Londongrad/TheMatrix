import { useEffect, useMemo, useState } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import Pagination from "@shared/ui/Pagination/Pagination";
import LoadingIndicator from "@shared/ui/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/Modal/Modal";
import {
  IconLock,
  IconUnlock,
  IconRefresh,
  IconOpen,
} from "@shared/ui/icons/icons";
import { usePagedQuery } from "@shared/lib/paging/usePagedQuery";
import {
  assignUserRoles,
  getPermissionsCatalog,
  getRolesCatalog,
  getUserDetails,
  getUserPermissions,
  getUserRoles,
  grantUserPermission,
  getUsersPage,
  lockUser,
  unlockUser,
  depriveUserPermission,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RoleResponse,
  UserDetailsResponse,
  UserListItemResponse,
  UserPermissionResponse,
  UserRoleResponse,
} from "@services/identity/api/admin/adminTypes";
import "./admin-users-page.css";

export default function AdminUsersPage() {
  const [refreshKey, setRefreshKey] = useState(0);
  const [selectedUserId, setSelectedUserId] = useState<string | null>(null);

  const pageSize = 10;

  const { data, pageNumber, setPageNumber, isLoading, error } = usePagedQuery(
    getUsersPage,
    pageSize,
    [refreshKey],
    {
      errorMessage: "Failed to load users",
    }
  );

  const items = data?.items ?? [];
  const totalPages = data?.totalPages ?? 1;

  const refresh = () => setRefreshKey((k) => k + 1);

  const toggleLock = async (u: UserListItemResponse) => {
    try {
      if (u.isLocked) await unlockUser(u.id);
      else await lockUser(u.id);
      refresh();
    } catch (e) {
      console.error(e);
    }
  };

  return (
    <div className="mx-admin-page">
      <Card
        title="Users"
        subtitle="Directory & access"
        right={
          <div className="mx-admin-users__headerRight">
            <Button onClick={refresh} disabled={isLoading}>
              <IconRefresh /> Refresh
            </Button>
            <Button variant="primary" type="button" disabled>
              + Add user
            </Button>
          </div>
        }
      >
        {error ? <div className="mx-admin-users__error">{error}</div> : null}

        {isLoading && items.length === 0 ? (
          <div className="mx-admin-users__loading">
            <LoadingIndicator label="Loading users" />
          </div>
        ) : null}

        <div className="mx-admin-users__grid" role="list">
          {items.map((u) => (
            <div key={u.id} className="mx-admin-users__card" role="listitem">
              <div className="mx-admin-users__cardTop">
                <div className="mx-admin-users__avatar">
                  {u.username?.[0]?.toUpperCase() ?? "U"}
                </div>
                <div className="mx-admin-users__meta">
                  <div className="mx-admin-users__username">{u.username}</div>
                  <div className="mx-admin-users__email">{u.email}</div>
                  <div className="mx-admin-users__id">{u.id}</div>
                </div>
                <div className="mx-admin-users__status">
                  {u.isLocked ? (
                    <Badge kind="bad">Locked</Badge>
                  ) : (
                    <Badge kind="ok">Active</Badge>
                  )}
                  {u.isEmailConfirmed ? (
                    <Badge kind="ok">Email confirmed</Badge>
                  ) : (
                    <Badge kind="warn">Email pending</Badge>
                  )}
                </div>
              </div>

              <div className="mx-admin-users__cardRow">
                <span className="mx-admin-users__muted">Created</span>
                <span>{formatUtc(u.createdAtUtc)}</span>
              </div>

              <div className="mx-admin-users__actions">
                <Button size="sm" onClick={() => setSelectedUserId(u.id)}>
                  <IconOpen /> Open access
                </Button>
                <IconButton
                  variant={u.isLocked ? "default" : "danger"}
                  title={u.isLocked ? "Unlock" : "Lock"}
                  onClick={() => void toggleLock(u)}
                  disabled={isLoading}
                >
                  {u.isLocked ? <IconUnlock /> : <IconLock />}
                </IconButton>
              </div>
            </div>
          ))}
        </div>

        {data ? (
          <div className="mx-admin-users__pager">
            <div className="mx-admin-users__muted">
              Page <b>{pageNumber}</b> / {totalPages} • {data.totalCount} total
            </div>
            <Pagination
              page={pageNumber}
              totalPages={totalPages}
              onChange={setPageNumber}
              disabled={isLoading}
            />
          </div>
        ) : null}
      </Card>

      {selectedUserId ? (
        <UserAccessModal
          userId={selectedUserId}
          onClose={() => setSelectedUserId(null)}
        />
      ) : null}
    </div>
  );
}

function UserAccessModal({
  userId,
  onClose,
}: {
  userId: string;
  onClose: () => void;
}) {
  const [loading, setLoading] = useState(true);
  const [savingRoles, setSavingRoles] = useState(false);
  const [savingPermission, setSavingPermission] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const [details, setDetails] = useState<UserDetailsResponse | null>(null);
  const [rolesCatalog, setRolesCatalog] = useState<RoleResponse[]>([]);
  const [userRoles, setUserRoles] = useState<UserRoleResponse[]>([]);
  const [permissionsCatalog, setPermissionsCatalog] = useState<
    PermissionCatalogItemResponse[]
  >([]);
  const [userPermissions, setUserPermissions] = useState<
    UserPermissionResponse[]
  >([]);

  const [selectedRoleIds, setSelectedRoleIds] = useState<string[]>([]);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);

    Promise.all([
      getUserDetails(userId),
      getRolesCatalog(),
      getUserRoles(userId),
      getPermissionsCatalog(),
      getUserPermissions(userId),
    ])
      .then(([user, roles, assignedRoles, perms, overrides]) => {
        if (!active) return;
        setDetails(user);
        setRolesCatalog(roles);
        setUserRoles(assignedRoles);
        setSelectedRoleIds(assignedRoles.map((r) => r.id));
        setPermissionsCatalog(perms.filter((p) => !p.isDeprecated));
        setUserPermissions(overrides);
      })
      .catch((e) => {
        console.error(e);
        if (!active) return;
        setError("Failed to load user access data");
      })
      .finally(() => {
        if (!active) return;
        setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [userId]);

  const permissionMap = useMemo(() => {
    const map = new Map<string, UserPermissionResponse>();
    userPermissions.forEach((p) => map.set(p.permissionKey, p));
    return map;
  }, [userPermissions]);

  const saveRoles = async () => {
    setSavingRoles(true);
    setError(null);
    try {
      await assignUserRoles(userId, selectedRoleIds);
      const updated = await getUserRoles(userId);
      setUserRoles(updated);
      setSelectedRoleIds(updated.map((r) => r.id));
    } catch (e) {
      console.error(e);
      setError("Failed to update roles");
    } finally {
      setSavingRoles(false);
    }
  };

  const updatePermission = async (
    permissionKey: string,
    effect: "Allow" | "Deny"
  ) => {
    setSavingPermission(permissionKey);
    setError(null);
    try {
      if (effect === "Allow") {
        await grantUserPermission(userId, permissionKey);
      } else {
        await depriveUserPermission(userId, permissionKey);
      }
      const updated = await getUserPermissions(userId);
      setUserPermissions(updated);
    } catch (e) {
      console.error(e);
      setError("Failed to update permissions");
    } finally {
      setSavingPermission(null);
    }
  };

  return (
    <Modal
      open
      title="User access"
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {loading ? (
        <div className="mx-admin-users__loading">
          <LoadingIndicator label="Loading access details" />
        </div>
      ) : null}

      {error ? <div className="mx-admin-users__error">{error}</div> : null}

      {details ? (
        <div className="mx-admin-users__modal">
          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">Profile</div>
            <div className="mx-admin-users__profileGrid">
              <div>
                <div className="mx-admin-users__muted">Username</div>
                <div>{details.username}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Email</div>
                <div>{details.email}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Permissions version</div>
                <div>{details.permissionsVersion}</div>
              </div>
              <div>
                <div className="mx-admin-users__muted">Created</div>
                <div>{formatUtc(details.createdAtUtc)}</div>
              </div>
            </div>
          </div>

          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">Roles</div>
            <div className="mx-admin-users__roles">
              {rolesCatalog.map((role) => (
                <label key={role.id} className="mx-admin-users__roleItem">
                  <input
                    type="checkbox"
                    checked={selectedRoleIds.includes(role.id)}
                    onChange={(event) => {
                      if (event.target.checked) {
                        setSelectedRoleIds((prev) => [...prev, role.id]);
                      } else {
                        setSelectedRoleIds((prev) =>
                          prev.filter((id) => id !== role.id)
                        );
                      }
                    }}
                  />
                  <span>{role.name}</span>
                  {role.isSystem ? (
                    <span className="mx-admin-users__chip">System</span>
                  ) : null}
                </label>
              ))}
            </div>
            <div className="mx-admin-users__rolesActions">
              <Button onClick={saveRoles} disabled={savingRoles}>
                Save roles
              </Button>
              <div className="mx-admin-users__muted">
                {userRoles.length} assigned
              </div>
            </div>
          </div>

          <div className="mx-admin-users__section">
            <div className="mx-admin-users__sectionTitle">
              Direct permission overrides
            </div>
            <div className="mx-admin-users__permissions">
              {permissionsCatalog.map((permission) => {
                const override = permissionMap.get(permission.key);
                return (
                  <div
                    key={permission.key}
                    className="mx-admin-users__permissionRow"
                  >
                    <div>
                      <div className="mx-admin-users__permKey">
                        {permission.key}
                      </div>
                      <div className="mx-admin-users__permDesc">
                        {permission.description}
                      </div>
                    </div>
                    <div className="mx-admin-users__permActions">
                      <Badge
                        kind={override?.effect === "Allow" ? "ok" : "warn"}
                      >
                        {override?.effect ?? "None"}
                      </Badge>
                      <Button
                        size="sm"
                        disabled={savingPermission === permission.key}
                        onClick={() =>
                          updatePermission(permission.key, "Allow")
                        }
                      >
                        Allow
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        disabled={savingPermission === permission.key}
                        onClick={() => updatePermission(permission.key, "Deny")}
                      >
                        Deny
                      </Button>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </div>
      ) : null}
    </Modal>
  );
}

function Badge({
  kind,
  children,
}: {
  kind: "ok" | "warn" | "bad";
  children: React.ReactNode;
}) {
  return <span className={`mx-admin-users__badge ${kind}`}>{children}</span>;
}

function formatUtc(utc: string) {
  return utc?.replace("T", " ").replace("Z", "");
}
