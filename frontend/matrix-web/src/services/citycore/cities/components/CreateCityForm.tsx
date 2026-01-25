import {useMemo, useState} from "react";
import Button from "@shared/ui/controls/Button/Button";
import type {CityCreatedView, CreateCityRequest} from "@services/citycore/cities/contracts/citiesContracts";
import {getNowLocalDateTimeInputValue, localDateTimeToUtcIso,} from "@services/citycore/cities/utils/dateTime";

type ValidationErrors = {
    name?: string;
    startSimTimeLocal?: string;
    speedMultiplier?: string;
};

type Props = {
    isSubmitting: boolean;
    submitError: string | null;
    onSubmit: (request: CreateCityRequest) => Promise<CityCreatedView | null>;
    onCreated: (created: CityCreatedView) => void | Promise<void>;
    onClearSubmitError?: () => void;
};

function validate(
    name: string,
    startSimTimeLocal: string,
    speedMultiplier: string,
): ValidationErrors {
    const errors: ValidationErrors = {};

    if (!name.trim()) {
        errors.name = "City name is required.";
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
                                   onSubmit,
                                   onCreated,
                                   onClearSubmitError,
                               }: Props) {
    const [name, setName] = useState("");
    const [startSimTimeLocal, setStartSimTimeLocal] = useState(
        getNowLocalDateTimeInputValue(),
    );
    const [speedMultiplier, setSpeedMultiplier] = useState("1");
    const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});

    const hasValidationErrors = useMemo(
        () => Object.keys(validationErrors).length > 0,
        [validationErrors],
    );

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

    async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
        e.preventDefault();

        const errors = validate(name, startSimTimeLocal, speedMultiplier);
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
            startSimTimeUtc,
            speedMultiplier: Number(speedMultiplier),
        });

        if (!response) {
            return;
        }

        setName("");
        setStartSimTimeLocal(getNowLocalDateTimeInputValue());
        setSpeedMultiplier("1");
        setValidationErrors({});

        await onCreated(response);
    }

    return (
        <form className="cities-create-form" onSubmit={handleSubmit} noValidate>
            <div className="cities-form-grid">
                <div className="cities-field">
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
                        onChange={(e) => {
                            setName(e.target.value);
                            clearFieldError("name");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.name && (
                        <div className="cities-field-error">{validationErrors.name}</div>
                    )}
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
                        onChange={(e) => {
                            setStartSimTimeLocal(e.target.value);
                            clearFieldError("startSimTimeLocal");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.startSimTimeLocal && (
                        <div className="cities-field-error">
                            {validationErrors.startSimTimeLocal}
                        </div>
                    )}
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
                        onChange={(e) => {
                            setSpeedMultiplier(e.target.value);
                            clearFieldError("speedMultiplier");
                            onClearSubmitError?.();
                        }}
                    />
                    {validationErrors.speedMultiplier && (
                        <div className="cities-field-error">
                            {validationErrors.speedMultiplier}
                        </div>
                    )}
                </div>
            </div>

            <div className="cities-form-hint">
                The value shown above is local browser time. It will be converted to UTC before sending to backend.
            </div>

            {submitError && (
                <div className="cities-error-banner" role="alert">
                    {submitError}
                </div>
            )}

            <div className="cities-form-actions">
                <Button
                    type="submit"
                    variant="primary"
                    disabled={isSubmitting}
                >
                    {isSubmitting ? "Creating..." : "Create city"}
                </Button>
            </div>

            {hasValidationErrors && <div className="cities-form-spacer"/>}
        </form>
    );
}
