import { useState } from "react";
import Button from "@shared/ui/controls/Button/Button";
import Modal from "@shared/ui/components/Modal/Modal";
import { renameRole } from "@services/identity/api/admin/adminApi";
import type {
  RenameRoleRequest,
  RoleResponse,
} from "@services/identity/api/admin/adminTypes";

export default function RenameRoleModal({
  role,
  onClose,
  onUpdated,
}: {
  role: RoleResponse;
  onClose: () => void;
  onUpdated: () => void;
}) {
  const [name, setName] = useState(role.name);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submit = async () => {
    if (!name.trim()) {
      setError("Role name is required");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      const payload: RenameRoleRequest = { name: name.trim() };
      await renameRole(role.id, payload);
      await onUpdated();
      onClose();
    } catch (error: any) {
      console.error(error);
      setError(error?.message ?? "Failed to rename role");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open
      title={`Rename role · ${role.name}`}
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} disabled={saving}>
            Save
          </Button>
        </>
      }
    >
      {error ? <div className="mx-admin-roles__error">{error}</div> : null}
      <label className="mx-admin-roles__field">
        <span>Role name</span>
        <input
          className="mx-admin-roles__input"
          value={name}
          onChange={(event) => setName(event.target.value)}
        />
      </label>
    </Modal>
  );
}
