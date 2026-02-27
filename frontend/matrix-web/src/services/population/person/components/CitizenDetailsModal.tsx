import {useEffect, useState} from "react";
import type {PersonDto} from "@services/population/person/api/personTypes";
import {
    killCitizen,
    resurrectCitizen,
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
    canKill?: boolean;
    canResurrect?: boolean;
    readOnlyMessage?: string | null;
}

function formatMetric(value: number): string {
    return Number.isFinite(value) ? value.toString() : "--";
}

const CitizenDetailsModal = ({
                                 person,
                                 isOpen,
                                 onClose,
                                 onPersonUpdated,
                                 canKill = false,
                                 canResurrect = false,
                                 readOnlyMessage,
                             }: CitizenDetailsModalProps) => {
    const [isBusy, setIsBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const {token} = useAuth();

    useEffect(() => {
        if (!isOpen) {
            setIsBusy(false);
            setError(null);
        }
    }, [isOpen]);

    if (!isOpen || !person) {
        return null;
    }

    const resident = person;
    const isDead = resident.lifeStatus === "Deceased";
    const canRunKill = !isDead && canKill && !readOnlyMessage;
    const canRunResurrect = isDead && canResurrect && !readOnlyMessage;

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
        } catch (error: unknown) {
            console.error(error);
            setError("Failed to kill resident.");
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
        } catch (error: unknown) {
            console.error(error);
            setError("Failed to resurrect resident.");
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
                            <h2 className="citizens-page-modal-title">{resident.fullName}</h2>
                        </div>

                        <p className="citizens-page-modal-subtitle">
                            {resident.sex}, {resident.age} y.o. ({resident.ageGroup})
                        </p>
                        <p className="citizens-page-modal-subtitle">
                            Status: {resident.lifeStatus}
                            {resident.deathDate ? ` • Died: ${resident.deathDate}` : ""}
                        </p>
                    </div>

                    <IconButton aria-label="Close modal" onClick={onClose}>
                        ×
                    </IconButton>
                </header>

                <section className="citizens-page-modal-body">
                    {readOnlyMessage ? (
                        <div className="citycore-error-banner" role="status">
                            <span>{readOnlyMessage}</span>
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
                                <div>{resident.educationLevel}</div>
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
                                <div>{resident.jobTitle?.trim() ? resident.jobTitle : "—"}</div>
                            </div>
                        </div>
                    </div>

                    <div className="citizens-page-modal-grid">
                        <div>
                            <h3 className="citizens-page-modal-section-title">Wellbeing</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Health</div>
                                <div>{formatMetric(resident.health)}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Happiness</div>
                                <div>{formatMetric(resident.happiness)}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Energy</div>
                                <div>{formatMetric(resident.energy)}</div>
                            </div>
                        </div>

                        <div>
                            <h3 className="citizens-page-modal-section-title">Pressure</h3>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Stress</div>
                                <div>{formatMetric(resident.stress)}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Social need</div>
                                <div>{formatMetric(resident.socialNeed)}</div>
                            </div>
                        </div>
                    </div>

                    {error ? <p className="error-text">{error}</p> : null}
                </section>

                <footer className="citizens-page-modal-footer">
                    <div className="citizens-page-modal-footer-group">
                        {!isDead ? (
                            <Button
                                variant="danger"
                                size="sm"
                                disabled={isBusy || !canRunKill}
                                onClick={handleKill}
                            >
                                Kill resident
                            </Button>
                        ) : null}

                        {isDead ? (
                            <Button
                                variant="success"
                                size="sm"
                                disabled={isBusy || !canRunResurrect}
                                onClick={handleResurrect}
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
