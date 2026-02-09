import {type FormEvent, useMemo, useState} from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import type {CityView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";
import {
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
    isArchivedCity,
} from "@services/citycore/scenarios/classic-city/utils/presentation";

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
        return "--";
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

    const isArchived = isArchivedCity(city?.status, city?.archivedAtUtc);
    const statusTone = getCityStatusTone(city?.status, city?.archivedAtUtc);
    const statusLabel = formatCityStatusLabel(city?.status, city?.archivedAtUtc);

    const effectiveNamePlaceholder = useMemo(() => {
        return city?.name ?? "New city name";
    }, [city?.name]);

    async function handleRename(event: FormEvent) {
        event.preventDefault();

        const normalized = renameInput.trim();

        if (!city || isArchived) {
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
        <Card
            title="Lifecycle"
            subtitle="Identity, archival state, and management actions"
            right={<span className={`cities-status-pill cities-status-pill--${statusTone}`}>{statusLabel}</span>}
        >
            {isLoading && !city ? <p className="card-sub">Loading city...</p> : null}

            {city ? (
                <div className="city-overview-grid">
                    <div className="city-overview-stat">
                        <span className="city-overview-stat__label">City name</span>
                        <strong className="city-overview-stat__value">{city.name}</strong>
                    </div>

                    <div className="city-overview-stat">
                        <span className="city-overview-stat__label">City ID</span>
                        <strong className="city-overview-stat__value city-overview-stat__value--mono"
                                title={city.cityId}>
                            {city.cityId}
                        </strong>
                    </div>

                    <div className="city-overview-stat">
                        <span className="city-overview-stat__label">Simulation type</span>
                        <strong className="city-overview-stat__value">
                            {formatSimulationKindLabel(city.simulationKind)}
                        </strong>
                    </div>

                    <div className="city-overview-stat">
                        <span className="city-overview-stat__label">Created</span>
                        <strong className="city-overview-stat__value">{formatDateTime(city.createdAtUtc)}</strong>
                    </div>

                    <div className="city-overview-stat">
                        <span className="city-overview-stat__label">Archived at</span>
                        <strong className="city-overview-stat__value">{formatDateTime(city.archivedAtUtc)}</strong>
                    </div>
                </div>
            ) : null}

            <div className={`city-state-banner city-state-banner--${statusTone}`}>
                <div className="city-state-banner__title">
                    {isArchived ? "Archived city" : "Active city"}
                </div>
                <div className="city-state-banner__text">
                    {isArchived
                        ? "Simulation controls are disabled. The city remains visible for audit and can still be deleted as a cleanup action."
                        : "Archiving will freeze simulation activity and lock further control mutations for this city."}
                </div>
            </div>

            <form className="city-rename-form" onSubmit={handleRename}>
                <div className="cities-field city-rename-form__field">
                    <label className="cities-label" htmlFor="city-rename-input">
                        Rename city
                    </label>
                    <input
                        id="city-rename-input"
                        className="text-input"
                        value={renameInput}
                        onChange={(event) => {
                            setRenameInput(event.target.value);
                            setRenameError(null);
                            onClearMutationError?.();
                        }}
                        placeholder={effectiveNamePlaceholder}
                        maxLength={128}
                        disabled={!city || isSubmitting || isArchived}
                    />
                </div>

                <Button
                    type="submit"
                    variant="primary"
                    disabled={isSubmitting || !city || isArchived}
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

            <div className="city-actions-row">
                <Button
                    onClick={() => void handleArchive()}
                    disabled={isSubmitting || !city || isArchived}
                >
                    Archive city
                </Button>

                <Button
                    variant="danger"
                    onClick={() => void handleDelete()}
                    disabled={isSubmitting || !city || !isArchived}
                >
                    Delete city
                </Button>
            </div>
        </Card>
    );
}
