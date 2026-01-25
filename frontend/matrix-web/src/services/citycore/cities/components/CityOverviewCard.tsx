import { useMemo, useState, type FormEvent } from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import type { CityView } from "@services/citycore/cities/contracts/citiesContracts";

type Props = {
    city: CityView | null;
    isLoading: boolean;
    isSubmitting: boolean;
    mutationError: string | null;
    onClearMutationError?: () => void;
    onRename: (name: string) => Promise<void>;
    onArchive: () => Promise<void>;
    onDelete: () => Promise<void>;
};

function formatDateTime(value: string | null | undefined): string {
    if (!value) {
        return "—";
    }

    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

export function CityOverviewCard({
                                     city,
                                     isLoading,
                                     isSubmitting,
                                     mutationError,
                                     onClearMutationError,
                                     onRename,
                                     onArchive,
                                     onDelete,
                                 }: Props) {
    const [renameInput, setRenameInput] = useState("");
    const [renameError, setRenameError] = useState<string | null>(null);

    const isArchived = Boolean(city?.archivedAtUtc);

    const effectiveNamePlaceholder = useMemo(() => {
        return city?.name ?? "New city name";
    }, [city?.name]);

    async function handleRename(event: FormEvent) {
        event.preventDefault();

        const normalized = renameInput.trim();

        if (!city) {
            return;
        }

        if (!normalized) {
            setRenameError("City name is required.");
            return;
        }

        if (normalized.length > 128) {
            setRenameError("City name must be 128 characters or less.");
            return;
        }

        if (normalized === city.name) {
            setRenameError("Enter a different city name.");
            return;
        }

        setRenameError(null);
        await onRename(normalized);
        setRenameInput("");
    }

    async function handleArchive() {
        if (!city || isArchived) {
            return;
        }

        await onArchive();
    }

    async function handleDelete() {
        if (!city || !isArchived) {
            return;
        }

        await onDelete();
    }

    return (
        <Card title={city?.name ?? "City"} subtitle={city?.cityId ?? "City details"}>
            {isLoading ? <p className="card-sub">Loading city...</p> : null}

            {city ? (
                <div className="city-details-grid">
                    <div>
                        <p className="card-sub">Status</p>
                        <p>{city.status}</p>
                    </div>

                    <div>
                        <p className="card-sub">Created</p>
                        <p>{formatDateTime(city.createdAtUtc)}</p>
                    </div>

                    <div>
                        <p className="card-sub">Archived at</p>
                        <p>{formatDateTime(city.archivedAtUtc)}</p>
                    </div>
                </div>
            ) : null}

            <form className="sim-inline-form" onSubmit={handleRename}>
                <input
                    className="text-input"
                    value={renameInput}
                    onChange={(event) => {
                        setRenameInput(event.target.value);
                        setRenameError(null);
                        onClearMutationError?.();
                    }}
                    placeholder={effectiveNamePlaceholder}
                    maxLength={128}
                    disabled={!city || isSubmitting}
                />
                <Button
                    type="submit"
                    variant="primary"
                    disabled={isSubmitting || !city}
                >
                    Rename
                </Button>
            </form>

            {renameError ? (
                <div className="city-inline-error" role="alert">
                    {renameError}
                </div>
            ) : null}

            {mutationError ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{mutationError}</span>
                </div>
            ) : null}

            <div className="sim-controls-row">
                <Button
                    onClick={() => void handleArchive()}
                    disabled={isSubmitting || !city || isArchived}
                >
                    Archive
                </Button>

                <Button
                    variant="danger"
                    onClick={() => void handleDelete()}
                    disabled={isSubmitting || !city || !isArchived}
                >
                    Delete city
                </Button>
            </div>

            {!isArchived && (
                <div className="city-details-hint">
                    Delete is available only after archive.
                </div>
            )}
        </Card>
    );
}
