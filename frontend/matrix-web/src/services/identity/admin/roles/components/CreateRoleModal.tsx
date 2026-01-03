import { useState } from "react";
import Button from "@shared/ui/controls/Button/Button";
import Modal from "@shared/ui/components/Modal/Modal";
import { createRole } from "@services/identity/api/admin/adminApi";

export default function CreateRoleModal({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: () => void;
}) {
  const [name, setName] = useState("");
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
      await createRole({ name: name.trim() });
      await onCreated();
      onClose();
    } catch (error: any) {
      console.error(error);
      setError(error?.message ?? "Failed to create role");
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal
      open
      title="Create role"
      onClose={onClose}
      footer={
        <>
          <Button onClick={onClose}>Cancel</Button>
          <Button variant="primary" onClick={submit} disabled={saving}>
            Create
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
