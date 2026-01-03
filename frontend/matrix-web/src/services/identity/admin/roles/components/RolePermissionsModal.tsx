import { useEffect, useMemo, useState } from "react";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import Modal from "@shared/ui/components/Modal/Modal";
import Button from "@shared/ui/controls/Button/Button";
import {
  getPermissionsCatalog,
  getRolePermissions,
} from "@services/identity/api/admin/adminApi";
import type {
  PermissionCatalogItemResponse,
  RolePermissionsResponse,
  RoleResponse,
} from "@services/identity/api/admin/adminTypes";

export default function RolePermissionsModal({
  role,
  onClose,
}: {
  role: RoleResponse;
  onClose: () => void;
}) {
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [catalog, setCatalog] = useState<PermissionCatalogItemResponse[]>([]);
  const [rolePermissions, setRolePermissions] =
    useState<RolePermissionsResponse | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);

    Promise.all([getPermissionsCatalog(), getRolePermissions(role.id)])
      .then(([permissions, rolePerms]) => {
        if (!active) return;
        setCatalog(permissions.filter((p) => !p.isDeprecated));
        setRolePermissions(rolePerms);
      })
      .catch((error) => {
        console.error(error);
        if (!active) return;
        setError("Failed to load role permissions");
      })
      .finally(() => {
        if (!active) return;
        setLoading(false);
      });

    return () => {
      active = false;
    };
  }, [role.id]);

  const assigned = useMemo(
    () => new Set(rolePermissions?.permissionKeys ?? []),
    [rolePermissions]
  );

  return (
    <Modal
      open
      title={`Permissions · ${role.name}`}
      onClose={onClose}
      footer={
        <Button variant="primary" onClick={onClose}>
          Close
        </Button>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      {loading ? (
        <div className="mx-admin-roles__loading">
          <LoadingIndicator label="Loading permissions" />
        </div>
      ) : null}

      <div className="mx-admin-roles__permissions">
        {catalog.map((permission) => (
          <div key={permission.key} className="mx-admin-roles__permissionRow">
            <div>
              <div className="mx-admin-roles__permissionKey">
                {permission.key}
              </div>
              <div className="mx-admin-roles__permissionDesc">
                {permission.description}
              </div>
            </div>
            <span
              className={
                assigned.has(permission.key)
                  ? "mx-admin-roles__permissionBadge is-active"
                  : "mx-admin-roles__permissionBadge"
              }
            >
              {assigned.has(permission.key) ? "Granted" : "Not granted"}
            </span>
          </div>
        ))}
      </div>
    </Modal>
  );
}
