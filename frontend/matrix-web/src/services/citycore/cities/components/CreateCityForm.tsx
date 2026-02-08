import {useEffect, useMemo, useState} from "react";
import Button from "@shared/ui/controls/Button/Button";
import type {
    CityCreatedView,
    CreateCityRequest,
    SimulationKindCatalogItemView,
} from "@services/citycore/cities/contracts/citiesContracts";
import {getNowLocalDateTimeInputValue, localDateTimeToUtcIso} from "@services/citycore/cities/utils/dateTime";

type ValidationErrors = {
    name?: string;
    simulationKind?: string;
    startSimTimeLocal?: string;
    speedMultiplier?: string;
};

type Props = {
    isSubmitting: boolean;
    submitError: string | null;
    simulationKinds: SimulationKindCatalogItemView[];
    simulationKindsError: string | null;
    isSimulationKindsLoading: boolean;
    onSubmit: (request: CreateCityRequest) => Promise<CityCreatedView | null>;
    onCreated: (created: CityCreatedView) => void | Promise<void>;
    onClearSubmitError?: () => void;
};

function validate(
    name: string,
    simulationKind: string,
    startSimTimeLocal: string,
    speedMultiplier: string,
): ValidationErrors {
    const errors: ValidationErrors = {};

    if (!name.trim()) {
        errors.name = "City name is required.";
    }

    if (!simulationKind.trim()) {
        errors.simulationKind = "Simulation type is required.";
    }

    if (!startSimTimeLocal.trim()) {
        errors.startSimTimeLocal = "Start simulation time is required.";
    } else if (!localDateTimeToUtcIso(startSimTimeLocal)) {
        errors.startSimTimeLocal = "Invalid date/time value.";
    }

    const speed = Number(speedMultiplier);

    if (!speedMultiplier.trim()) {
        errors.speedMultiplier = "Speed multiplier is required.";
    } else if (!Number.isFinite(speed)) {
        errors.speedMultiplier = "Speed multiplier must be a number.";
    } else if (speed <= 0) {
        errors.speedMultiplier = "Speed multiplier must be greater than 0.";
    }

    return errors;
}

export function CreateCityForm({
    isSubmitting,
    submitError,
    simulationKinds,
    simulationKindsError,
    isSimulationKindsLoading,
    onSubmit,
    onCreated,
    onClearSubmitError,
}: Props) {
    const defaultSimulationKind = useMemo(() => {
        return simulationKinds.find((kind) => kind.isDefault)?.kind ??
               simulationKinds[0]?.kind ??
               "ClassicCity";
    }, [simulationKinds]);

    const [name, setName] = useState("");
    const [simulationKind, setSimulationKind] = useState(defaultSimulationKind);
    const [startSimTimeLocal, setStartSimTimeLocal] = useState(
        getNowLocalDateTimeInputValue(),
    );
    const [speedMultiplier, setSpeedMultiplier] = useState("1");
    const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

    const hasValidationErrors = useMemo(
        () => Object.keys(validationErrors).length > 0,
        [validationErrors],
    );

    const selectedSimulation = useMemo(() => {
        return simulationKinds.find((kind) => kind.kind === simulationKind) ?? null;
    }, [simulationKind, simulationKinds]);

    useEffect(() => {
        setSimulationKind((current) => {
            const hasCurrent = simulationKinds.some((kind) => kind.kind === current);
            return hasCurrent ? current : defaultSimulationKind;
        });
    }, [defaultSimulationKind, simulationKinds]);

    function clearFieldError(field: keyof ValidationErrors) {
        setValidationErrors((current) => {
            if (!current[field]) {
                return current;
            }

            const next = {...current};
            delete next[field];
            return next;
        });
    }

    async function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
        event.preventDefault();

        const effectiveSimulationKind = simulationKind || defaultSimulationKind;
        const errors = validate(name, effectiveSimulationKind, startSimTimeLocal, speedMultiplier);
        setValidationErrors(errors);

        if (Object.keys(errors).length > 0) {
            return;
        }

        const startSimTimeUtc = localDateTimeToUtcIso(startSimTimeLocal);

        if (!startSimTimeUtc) {
            setValidationErrors({
                startSimTimeLocal: "Invalid date/time value.",
            });
            return;
        }

        const response = await onSubmit({
            name: name.trim(),
            simulationKind: effectiveSimulationKind,
            startSimTimeUtc,
            speedMultiplier: Number(speedMultiplier),
        });

        if (!response) {
            return;
        }

        setName("");
        setSimulationKind(defaultSimulationKind);
        setStartSimTimeLocal(getNowLocalDateTimeInputValue());
        setSpeedMultiplier("1");
        setValidationErrors({});

        await onCreated(response);
    }

    return (
        <form className="cities-create-form" onSubmit={handleSubmit} noValidate>
            <div className="cities-create-form__intro">
                Provision a new world state, choose the simulation baseline, and hand off the launched timeline to operators.
            </div>

            <div className="cities-create-form__grid">
                <div className="cities-field cities-field--wide">
                    <label className="cities-label" htmlFor="city-name">
                        City name
                    </label>
                    <input
                        id="city-name"
                        className="cities-input"
                        type="text"
                        value={name}
                        maxLength={128}
                        placeholder="New Amsterdam"
                        onChange={(event) => {
                            setName(event.target.value);
                            clearFieldError("name");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.name ? (
                        <div className="cities-field-error">{validationErrors.name}</div>
                    ) : null}
                </div>

                <div className="cities-field cities-field--wide">
                    <label className="cities-label" htmlFor="city-simulation-kind">
                        Simulation type
                    </label>
                    <select
                        id="city-simulation-kind"
                        className="cities-input"
                        value={simulationKind}
                        disabled={isSimulationKindsLoading || simulationKinds.length === 0}
                        onChange={(event) => {
                            setSimulationKind(event.target.value);
                            clearFieldError("simulationKind");
                            onClearSubmitError?.();
                        }}
                    >
                        {simulationKinds.length === 0 ? (
                            <option value={defaultSimulationKind}>Classic City</option>
                        ) : simulationKinds.map((kind) => (
                            <option key={kind.kind} value={kind.kind}>
                                {kind.displayName}
                            </option>
                        ))}
                    </select>
                    {validationErrors.simulationKind ? (
                        <div className="cities-field-error">{validationErrors.simulationKind}</div>
                    ) : null}
                    {selectedSimulation ? (
                        <div className="cities-form-hint">
                            {selectedSimulation.description}
                        </div>
                    ) : null}
                    {simulationKindsError ? (
                        <div className="cities-field-error">{simulationKindsError}</div>
                    ) : null}
                </div>

                <div className="cities-field">
                    <label className="cities-label" htmlFor="city-start-sim-time">
                        Start simulation time
                    </label>
                    <input
                        id="city-start-sim-time"
                        className="cities-input"
                        type="datetime-local"
                        value={startSimTimeLocal}
                        onChange={(event) => {
                            setStartSimTimeLocal(event.target.value);
                            clearFieldError("startSimTimeLocal");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.startSimTimeLocal ? (
                        <div className="cities-field-error">
                            {validationErrors.startSimTimeLocal}
                        </div>
                    ) : null}
                </div>

                <div className="cities-field">
                    <label className="cities-label" htmlFor="city-speed-multiplier">
                        Speed multiplier
                    </label>
                    <input
                        id="city-speed-multiplier"
                        className="cities-input"
                        type="number"
                        min="0.1"
                        step="0.1"
                        value={speedMultiplier}
                        onChange={(event) => {
                            setSpeedMultiplier(event.target.value);
                            clearFieldError("speedMultiplier");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.speedMultiplier ? (
                        <div className="cities-field-error">
                            {validationErrors.speedMultiplier}
                        </div>
                    ) : null}
                </div>
            </div>

            <div className="cities-form-hint">
                Start time uses the local browser timezone and is converted to UTC before the request is sent.
            </div>

            {submitError ? (
                <div className="cities-error-banner" role="alert">
                    {submitError}
                </div>
            ) : null}

            <div className="cities-form-actions">
                <Button
                    type="submit"
                    variant="primary"
                    disabled={isSubmitting || isSimulationKindsLoading}
                >
                    {isSubmitting ? "Launching simulation..." : "Launch simulation"}
                </Button>
            </div>

            {hasValidationErrors ? <div className="cities-form-spacer"/> : null}
        </form>
    );
}
