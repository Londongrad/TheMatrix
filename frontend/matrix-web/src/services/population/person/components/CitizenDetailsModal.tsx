import {useEffect, useState} from "react";
import type {PersonDto} from "@services/population/person/api/personTypes";
import {
    killCitizen,
    resurrectCitizen,
    updateCitizen,
} from "@services/population/person/api/personApi";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import "@services/population/person/styles/citizen-details-modal.css";

interface CitizenDetailsModalProps {
    person: PersonDto | null;
    isOpen: boolean;
    onClose: () => void;
    onPersonUpdated?: (person: PersonDto) => void;
    canUpdate?: boolean;
    canKill?: boolean;
    canResurrect?: boolean;
    readOnlyMessage?: string | null;
}

type PersonFormState = {
    fullName: string;
    educationLevel: string;
    health: string;
    happiness: string;
    energy: string;
    stress: string;
    socialNeed: string;
};

type FormErrors = Partial<Record<keyof PersonFormState, string>>;

const EDUCATION_LEVEL_OPTIONS = [
    "None",
    "Preschool",
    "Primary",
    "LowerSecondary",
    "UpperSecondary",
    "Vocational",
    "Higher",
    "Postgraduate",
] as const;

function createFormState(person: PersonDto): PersonFormState {
    return {
        fullName: person.fullName,
        educationLevel: person.educationLevel,
        health: String(person.health),
        happiness: String(person.happiness),
        energy: String(person.energy),
        stress: String(person.stress),
        socialNeed: String(person.socialNeed),
    };
}

function formatMetric(value: number): string {
    return Number.isFinite(value) ? value.toString() : "--";
}

function parseMetricField(value: string): number | null {
    if (!value.trim()) {
        return null;
    }

    const parsed = Number(value);
    if (!Number.isInteger(parsed)) {
        return null;
    }

    if (parsed < 0 || parsed > 100) {
        return null;
    }

    return parsed;
}

function validateForm(form: PersonFormState): FormErrors {
    const errors: FormErrors = {};

    if (!form.fullName.trim()) {
        errors.fullName = "Full name is required.";
    } else if (form.fullName.trim().length > 200) {
        errors.fullName = "Full name must stay within 200 characters.";
    }

    if (!EDUCATION_LEVEL_OPTIONS.includes(form.educationLevel as typeof EDUCATION_LEVEL_OPTIONS[number])) {
        errors.educationLevel = "Education level must stay within the supported population enum.";
    }

    const metricFields: Array<keyof Pick<PersonFormState, "health" | "happiness" | "energy" | "stress" | "socialNeed">> = [
        "health",
        "happiness",
        "energy",
        "stress",
        "socialNeed",
    ];

    for (const field of metricFields) {
        if (parseMetricField(form[field]) === null) {
            errors[field] = "Use a whole number between 0 and 100.";
        }
    }

    return errors;
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

const CitizenDetailsModal = ({
    person,
    isOpen,
    onClose,
    onPersonUpdated,
    canUpdate = false,
    canKill = false,
    canResurrect = false,
    readOnlyMessage,
}: CitizenDetailsModalProps) => {
    const [isBusy, setIsBusy] = useState(false);
    const [isEditing, setIsEditing] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [formErrors, setFormErrors] = useState<FormErrors>({});
    const [form, setForm] = useState<PersonFormState | null>(null);
    const {token} = useAuth();

    useEffect(() => {
        if (!person) {
            setForm(null);
            setIsEditing(false);
            setFormErrors({});
            return;
        }

        setForm(createFormState(person));
        setIsEditing(false);
        setFormErrors({});
        setError(null);
    }, [person]);

    useEffect(() => {
        if (!isOpen) {
            setIsBusy(false);
            setIsEditing(false);
            setFormErrors({});
            setError(null);
        }
    }, [isOpen]);

    if (!isOpen || !person || !form) {
        return null;
    }

    const resident = person;
    const residentForm = form;
    const isDead = resident.lifeStatus === "Deceased";
    const editingDisabledMessage = readOnlyMessage
        ?? (isDead ? "Deceased residents can be inspected, but direct editing is disabled." : null);
    const canRunUpdate = canUpdate && !editingDisabledMessage;
    const canRunKill = !isDead && canKill && !readOnlyMessage;
    const canRunResurrect = isDead && canResurrect && !readOnlyMessage;

    function updateFormValue<K extends keyof PersonFormState>(field: K, value: PersonFormState[K]) {
        setForm((current) => current ? {...current, [field]: value} : current);
        setFormErrors((current) => ({...current, [field]: undefined}));
    }

    function resetForm() {
        setForm(createFormState(resident));
        setFormErrors({});
        setError(null);
        setIsEditing(false);
    }

    async function handleSave() {
        if (!canRunUpdate) {
            return;
        }

        if (!token) {
            setError("Not authenticated.");
            return;
        }

        const validationErrors = validateForm(residentForm);
        if (Object.keys(validationErrors).length > 0) {
            setFormErrors(validationErrors);
            return;
        }

        try {
            setIsBusy(true);
            setError(null);

            const updated = await updateCitizen(
                resident.id,
                {
                    fullName: residentForm.fullName.trim(),
                    educationLevel: residentForm.educationLevel,
                    health: parseMetricField(residentForm.health) ?? resident.health,
                    happiness: parseMetricField(residentForm.happiness) ?? resident.happiness,
                    energy: parseMetricField(residentForm.energy) ?? resident.energy,
                    stress: parseMetricField(residentForm.stress) ?? resident.stress,
                    socialNeed: parseMetricField(residentForm.socialNeed) ?? resident.socialNeed,
                },
                token,
            );

            setForm(createFormState(updated));
            setIsEditing(false);
            setFormErrors({});
            onPersonUpdated?.(updated);
        } catch (updateError: unknown) {
            console.error(updateError);
            setError(getErrorMessage(updateError, "Failed to update resident."));
        } finally {
            setIsBusy(false);
        }
    }

    async function handleKill() {
        if (!canRunKill) {
            return;
        }

        if (!token) {
            setError("Not authenticated.");
            return;
        }

        const confirmed = window.confirm(`Kill ${resident.fullName}?`);
        if (!confirmed) {
            return;
        }

        try {
            setIsBusy(true);
            setError(null);

            const updated = await killCitizen(resident.id, token);
            onPersonUpdated?.(updated);
        } catch (killError: unknown) {
            console.error(killError);
            setError(getErrorMessage(killError, "Failed to kill resident."));
        } finally {
            setIsBusy(false);
        }
    }

    async function handleResurrect() {
        if (!canRunResurrect) {
            return;
        }

        if (!token) {
            setError("Not authenticated.");
            return;
        }

        try {
            setIsBusy(true);
            setError(null);

            const updated = await resurrectCitizen(resident.id, token);
            onPersonUpdated?.(updated);
        } catch (resurrectError: unknown) {
            console.error(resurrectError);
            setError(getErrorMessage(resurrectError, "Failed to resurrect resident."));
        } finally {
            setIsBusy(false);
        }
    }

    return (
        <div className="citizens-page-modal-backdrop" onClick={onClose}>
            <div
                className={
                    "citizens-page-modal" + (isDead ? " citizens-page-modal--dead" : "")
                }
                onClick={(event) => event.stopPropagation()}
            >
                <header className="citizens-page-modal-header">
                    <div>
                        <div className="citizens-page-modal-title-row">
                            {isEditing ? (
                                <input
                                    className="citizens-page-modal-input-text citizens-page-modal-title-edit-input"
                                    value={residentForm.fullName}
                                    onChange={(event) => updateFormValue("fullName", event.target.value)}
                                    disabled={isBusy}
                                />
                            ) : (
                                <h2 className="citizens-page-modal-title">{resident.fullName}</h2>
                            )}
                        </div>

                        {formErrors.fullName ? (
                            <div className="citizens-page-modal-error">{formErrors.fullName}</div>
                        ) : null}

                        <p className="citizens-page-modal-subtitle">
                            {resident.sex}, {resident.age} y.o. ({resident.ageGroup})
                        </p>
                        <p className="citizens-page-modal-subtitle">
                            Status: {resident.lifeStatus}
                            {resident.deathDate ? ` | Died: ${resident.deathDate}` : ""}
                        </p>
                    </div>

                    <IconButton aria-label="Close modal" onClick={onClose}>
                        X
                    </IconButton>
                </header>

                <section className="citizens-page-modal-body">
                    {editingDisabledMessage ? (
                        <div className="citycore-error-banner" role="status">
                            <span>{editingDisabledMessage}</span>
                        </div>
                    ) : null}

                    <div className="citizens-page-modal-grid">
                        <div>
                            <h3 className="citizens-page-modal-section-title">Identity</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Birth date</div>
                                <div>{resident.birthDate}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Marital</div>
                                <div>{resident.maritalStatus}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Education</div>
                                {isEditing ? (
                                    <>
                                        <select
                                            className="citizens-page-modal-select"
                                            value={residentForm.educationLevel}
                                            onChange={(event) => updateFormValue("educationLevel", event.target.value)}
                                            disabled={isBusy}
                                        >
                                            {EDUCATION_LEVEL_OPTIONS.map((option) => (
                                                <option key={option} value={option}>
                                                    {option}
                                                </option>
                                            ))}
                                        </select>
                                        {formErrors.educationLevel ? (
                                            <div className="citizens-page-modal-error">{formErrors.educationLevel}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{resident.educationLevel}</div>
                                )}
                            </div>
                        </div>

                        <div>
                            <h3 className="citizens-page-modal-section-title">Employment</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Status</div>
                                <div>{resident.employmentStatus}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Job title</div>
                                <div>{resident.jobTitle?.trim() ? resident.jobTitle : "--"}</div>
                            </div>
                        </div>
                    </div>

                    <div className="citizens-page-modal-grid">
                        <div>
                            <h3 className="citizens-page-modal-section-title">Wellbeing</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Health</div>
                                {isEditing ? (
                                    <>
                                        <input
                                            className="citizens-page-modal-input-number"
                                            type="number"
                                            min="0"
                                            max="100"
                                            step="1"
                                            value={residentForm.health}
                                            onChange={(event) => updateFormValue("health", event.target.value)}
                                            disabled={isBusy}
                                        />
                                        {formErrors.health ? (
                                            <div className="citizens-page-modal-error">{formErrors.health}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{formatMetric(resident.health)}</div>
                                )}
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Happiness</div>
                                {isEditing ? (
                                    <>
                                        <input
                                            className="citizens-page-modal-input-number"
                                            type="number"
                                            min="0"
                                            max="100"
                                            step="1"
                                            value={residentForm.happiness}
                                            onChange={(event) => updateFormValue("happiness", event.target.value)}
                                            disabled={isBusy}
                                        />
                                        {formErrors.happiness ? (
                                            <div className="citizens-page-modal-error">{formErrors.happiness}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{formatMetric(resident.happiness)}</div>
                                )}
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Energy</div>
                                {isEditing ? (
                                    <>
                                        <input
                                            className="citizens-page-modal-input-number"
                                            type="number"
                                            min="0"
                                            max="100"
                                            step="1"
                                            value={residentForm.energy}
                                            onChange={(event) => updateFormValue("energy", event.target.value)}
                                            disabled={isBusy}
                                        />
                                        {formErrors.energy ? (
                                            <div className="citizens-page-modal-error">{formErrors.energy}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{formatMetric(resident.energy)}</div>
                                )}
                            </div>
                        </div>

                        <div>
                            <h3 className="citizens-page-modal-section-title">Pressure</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Stress</div>
                                {isEditing ? (
                                    <>
                                        <input
                                            className="citizens-page-modal-input-number"
                                            type="number"
                                            min="0"
                                            max="100"
                                            step="1"
                                            value={residentForm.stress}
                                            onChange={(event) => updateFormValue("stress", event.target.value)}
                                            disabled={isBusy}
                                        />
                                        {formErrors.stress ? (
                                            <div className="citizens-page-modal-error">{formErrors.stress}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{formatMetric(resident.stress)}</div>
                                )}
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Social need</div>
                                {isEditing ? (
                                    <>
                                        <input
                                            className="citizens-page-modal-input-number"
                                            type="number"
                                            min="0"
                                            max="100"
                                            step="1"
                                            value={residentForm.socialNeed}
                                            onChange={(event) => updateFormValue("socialNeed", event.target.value)}
                                            disabled={isBusy}
                                        />
                                        {formErrors.socialNeed ? (
                                            <div className="citizens-page-modal-error">{formErrors.socialNeed}</div>
                                        ) : null}
                                    </>
                                ) : (
                                    <div>{formatMetric(resident.socialNeed)}</div>
                                )}
                            </div>
                        </div>
                    </div>

                    {error ? <p className="error-text">{error}</p> : null}
                </section>

                <footer className="citizens-page-modal-footer">
                    <div className="citizens-page-modal-footer-group">
                        {canRunUpdate && !isEditing ? (
                            <Button
                                variant="primary"
                                size="sm"
                                disabled={isBusy}
                                onClick={() => setIsEditing(true)}
                            >
                                Edit resident
                            </Button>
                        ) : null}

                        {canRunUpdate && isEditing ? (
                            <>
                                <Button
                                    variant="default"
                                    size="sm"
                                    disabled={isBusy}
                                    onClick={resetForm}
                                >
                                    Cancel
                                </Button>
                                <Button
                                    variant="success"
                                    size="sm"
                                    disabled={isBusy}
                                    onClick={() => void handleSave()}
                                >
                                    Save changes
                                </Button>
                            </>
                        ) : null}
                    </div>

                    <div className="citizens-page-modal-footer-group">
                        {!isDead ? (
                            <Button
                                variant="danger"
                                size="sm"
                                disabled={isBusy || !canRunKill}
                                onClick={() => void handleKill()}
                            >
                                Kill resident
                            </Button>
                        ) : null}

                        {isDead ? (
                            <Button
                                variant="success"
                                size="sm"
                                disabled={isBusy || !canRunResurrect}
                                onClick={() => void handleResurrect()}
                            >
                                Resurrect resident
                            </Button>
                        ) : null}
                    </div>
                </footer>
            </div>
        </div>
    );
};

export default CitizenDetailsModal;
