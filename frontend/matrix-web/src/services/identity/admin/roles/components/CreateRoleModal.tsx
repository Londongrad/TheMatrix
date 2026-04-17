import {useState} from "react";
import Button from "@shared/ui/controls/Button/Button";
import Modal from "@shared/ui/components/Modal/Modal";
import {createRole} from "@services/identity/api/admin/adminApi";

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
    const trimmedName = name.trim();

    const submit = async () => {
        if (!trimmedName) {
            setError("Role name is required");
            return;
        }
        setSaving(true);
        setError(null);
        try {
            await createRole({name: trimmedName});
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
            onClose={() => {
                if (!saving) onClose();
            }}
            footer={
                <>
                    <Button onClick={onClose} disabled={saving}>
                        Cancel
                    </Button>
                    <Button
                        variant="primary"
                        onClick={submit}
                        disabled={saving || !trimmedName}
                        aria-busy={saving}
                    >
                        <span className="mx-admin-roles__submit">
                            {saving ? (
                                <span
                                    className="mx-admin-roles__submitSpinner"
                                    aria-hidden="true"
                                />
                            ) : null}
                            <span>{saving ? "Creating role..." : "Create"}</span>
                        </span>
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
                    disabled={saving}
                    aria-disabled={saving}
                />
            </label>
            <div className="mx-admin-roles__hint" aria-live="polite">
                {saving
                    ? "Applying the role to the directory baseline..."
                    : "The new role will appear as soon as the request completes."}
            </div>
        </Modal>
    );
}
