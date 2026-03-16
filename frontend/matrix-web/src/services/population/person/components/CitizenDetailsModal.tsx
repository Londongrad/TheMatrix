import {useEffect, useState} from "react";
import {X} from "lucide-react";
import {Link, useNavigate} from "react-router-dom";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {getClassicCityResidentDossierPath} from "@services/citycore/scenarios/registry";
import type {CityResidentDetailsDto, PersonDto} from "@services/population/person/api/personTypes";
import {killCitizen, resurrectCitizen,} from "@services/population/person/api/personApi";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import Button from "@shared/ui/controls/Button/Button";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import "@services/population/person/styles/citizen-details-modal.css";

interface CitizenDetailsModalProps {
    cityId: string;
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

function formatLabel(value: string): string {
    return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

function formatHouseholdLabel(householdId?: string | null): string {
    if (!householdId) {
        return "--";
    }

    return `Household ${householdId.slice(0, 8)}`;
}

function renderResidentLink(
    cityId: string,
    resident?: { id: string; fullName: string } | null,
    fallback = "--",
) {
    if (!resident) {
        return fallback;
    }

    return (
        <Link
            className="citizens-page-modal-inline-link"
            to={getClassicCityResidentDossierPath(cityId, resident.id)}
        >
            {resident.fullName}
        </Link>
    );
}

const CitizenDetailsModal = ({
                                 cityId,
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
    const navigate = useNavigate();
    const residentQuery = useCityResidentDetails(
        cityId,
        person?.id ?? "",
        isOpen && cityId.length > 0 && person !== null,
    );

    useEffect(() => {
        if (!isOpen) {
            setIsBusy(false);
            setError(null);
        }
    }, [isOpen]);

    if (!isOpen || !person) {
        return null;
    }

    const resident: PersonDto | CityResidentDetailsDto = residentQuery.data ?? person;
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
            void residentQuery.refetch();
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
            void residentQuery.refetch();
        } catch (resurrectError: unknown) {
            console.error(resurrectError);
            setError(getErrorMessage(resurrectError, "Failed to resurrect resident."));
        } finally {
            setIsBusy(false);
        }
    }

    const residentDetails = "currentSpouse" in resident
        ? resident as CityResidentDetailsDto
        : null;
    const currentSpouse = residentDetails?.currentSpouse ?? null;
    const mother = residentDetails?.mother ?? null;
    const father = residentDetails?.father ?? null;
    const children = residentDetails?.children ?? [];
    const currentHousing = residentDetails?.currentHousing ?? null;

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
                            {resident.deathDate ? ` | Died: ${resident.deathDate}` : ""}
                        </p>
                    </div>

                    <IconButton aria-label="Close modal" onClick={onClose}>
                        <X size={16}/>
                    </IconButton>
                </header>

                <section className="citizens-page-modal-body">
                    {residentQuery.isLoading ? (
                        <div className="citycore-error-banner" role="status">
                            <span>Loading city-scoped resident details...</span>
                        </div>
                    ) : null}

                    {residentQuery.error ? (
                        <div className="citycore-error-banner" role="status">
                            <span>{residentQuery.error}</span>
                        </div>
                    ) : null}

                    <div className="citycore-error-banner" role="status">
                        <span>
                            Resident cards are read-only. Relationship, education, employment, and other lifecycle
                            actions should later run through dedicated city services instead of direct person patching.
                        </span>
                    </div>

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
                                <div className="citizens-page-modal-field-label">Current spouse</div>
                                <div>
                                    {currentSpouse
                                        ? renderResidentLink(cityId, currentSpouse)
                                        : resident.maritalStatus === "Married"
                                            ? "Spouse record unavailable"
                                            : "--"}
                                </div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Mother</div>
                                <div>{renderResidentLink(cityId, mother, "Not recorded")}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Father</div>
                                <div>{renderResidentLink(cityId, father, "Not recorded")}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Children</div>
                                <div>
                                    {children.length > 0 ? (
                                        <div className="citizens-page-modal-reference-list">
                                            {children.map((child) => (
                                                <Link
                                                    key={child.id}
                                                    className="citizens-page-modal-reference-token"
                                                    to={getClassicCityResidentDossierPath(cityId, child.id)}
                                                    title={child.fullName}
                                                >
                                                    {child.fullName}
                                                </Link>
                                            ))}
                                        </div>
                                    ) : "--"}
                                </div>
                            </div>

                            {residentDetails?.lastChildbirthDate ? (
                                <div className="citizens-page-modal-field">
                                    <div className="citizens-page-modal-field-label">Last childbirth</div>
                                    <div>{residentDetails.lastChildbirthDate}</div>
                                </div>
                            ) : null}

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Education</div>
                                <div>{resident.educationLevel}</div>
                            </div>

                            {currentHousing ? (
                                <>
                                    <div className="citizens-page-modal-field">
                                        <div className="citizens-page-modal-field-label">Household</div>
                                        <div>{formatHouseholdLabel(currentHousing.householdId)}</div>
                                    </div>

                                    <div className="citizens-page-modal-field">
                                        <div className="citizens-page-modal-field-label">Housing</div>
                                        <div>{currentHousing.housingStatus}</div>
                                    </div>
                                </>
                            ) : null}
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
                                <div>{formatMetric(resident.health)}</div>
                            </div>

                            <div className="citizens-page-modal-field">
                                <div className="citizens-page-modal-field-label">Current illness</div>
                                <div>
                                    {residentDetails?.currentIllness
                                        ? `${formatLabel(residentDetails.currentIllness.kind)} (${residentDetails.currentIllness.severity.toLowerCase()})`
                                        : "--"}
                                </div>
                            </div>

                            {residentDetails?.currentIllness ? (
                                <div className="citizens-page-modal-field">
                                    <div className="citizens-page-modal-field-label">Diagnosed on</div>
                                    <div>{residentDetails.currentIllness.diagnosedOn}</div>
                                </div>
                            ) : null}

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

                            {residentDetails?.lastIllnessRecoveredOn ? (
                                <div className="citizens-page-modal-field">
                                    <div className="citizens-page-modal-field-label">Last recovery</div>
                                    <div>{residentDetails.lastIllnessRecoveredOn}</div>
                                </div>
                            ) : null}
                        </div>
                    </div>

                    {error ? <p className="error-text">{error}</p> : null}
                </section>

                <footer className="citizens-page-modal-footer">
                    <div className="citizens-page-modal-footer-group">
                        <Button
                            size="sm"
                            onClick={() => {
                                onClose();
                                navigate(getClassicCityResidentDossierPath(cityId, resident.id));
                            }}
                        >
                            Open dossier
                        </Button>

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
