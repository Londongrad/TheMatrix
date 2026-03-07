import {useEffect, useMemo, useState} from "react";
import {Link, Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {
    enrollCityResident,
    getCityResidentsPage,
    graduateCityResident,
    withdrawCityResidentFromStudy,
} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
    getClassicCityResidentsPath,
} from "@services/citycore/scenarios/registry";
import {
    getCityStatusTone,
    isArchivedCity,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import type {
    CityEducationOperationResultDto,
    CityResidentDetailsDto,
    PersonDto,
} from "@services/population/person/api/personTypes";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import Button from "@shared/ui/controls/Button/Button";
import {getPageRange} from "@shared/lib/paging/pageRange";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-education.css";

const PAGE_SIZE = 100;

const NEXT_EDUCATION_LEVELS: Record<string, string[]> = {
    None: ["Preschool"],
    Preschool: ["Primary"],
    Primary: ["LowerSecondary"],
    LowerSecondary: ["UpperSecondary"],
    UpperSecondary: ["Vocational", "Higher"],
    Vocational: ["Higher"],
    Higher: ["Postgraduate"],
    Postgraduate: [],
};

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

function formatEducationLevel(level: string): string {
    return level.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatActionLabel(action: string): string {
    switch (action) {
        case "ResidentEnrolledInStudy":
            return "Resident enrolled";
        case "ResidentWithdrawnFromStudy":
            return "Study withdrawn";
        default:
            return "Education advanced";
    }
}

function formatTimestamp(value: string): string {
    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

type SelectedResidentCardProps = {
    cityId: string;
    residentId: string;
    resident: CityResidentDetailsDto | null;
    isLoading: boolean;
    nextLevels: string[];
    onClear: () => void;
};

function SelectedResidentCard({
    cityId,
    residentId,
    resident,
    isLoading,
    nextLevels,
    onClear,
}: SelectedResidentCardProps) {
    return (
        <section className="city-education__selected-card">
            <div className="city-education__selected-header">
                <div>
                    <span className="city-education__selected-label">Resident</span>
                    <h3 className="city-education__selected-name">
                        {residentId ? resident?.fullName ?? "Loading resident..." : "Select a resident below"}
                    </h3>
                </div>

                {residentId ? (
                    <Button type="button" size="sm" onClick={onClear}>
                        Clear
                    </Button>
                ) : null}
            </div>

            {residentId ? (
                <div className="city-education__selected-body">
                    <div className="city-education__tag-row">
                        <span className="city-education__tag">
                            {resident?.employmentStatus === "Student" ? "Studying now" : "Not studying"}
                        </span>
                        {resident?.lifeStatus ? (
                            <span className="city-education__tag city-education__tag--muted">
                                {resident.lifeStatus}
                            </span>
                        ) : null}
                        {resident?.ageGroup ? (
                            <span className="city-education__tag city-education__tag--muted">
                                {resident.ageGroup}
                            </span>
                        ) : null}
                    </div>

                    <p className="city-education__selected-copy">
                        Current level:
                        {" "}
                        <strong>{resident?.educationLevel ? formatEducationLevel(resident.educationLevel) : "Snapshot pending"}</strong>
                    </p>

                    {nextLevels.length > 0 ? (
                        <div className="city-education__next-levels">
                            {nextLevels.map((level) => (
                                <span key={level} className="city-education__tag city-education__tag--muted">
                                    Next: {formatEducationLevel(level)}
                                </span>
                            ))}
                        </div>
                    ) : (
                        <p className="card-sub">No higher education transitions remain for this resident.</p>
                    )}

                    {resident ? (
                        <Link
                            className="city-resident-dossier__inline-link"
                            to={getClassicCityResidentDossierPath(cityId, resident.id)}
                        >
                            Open resident dossier
                        </Link>
                    ) : null}

                    {isLoading ? (
                        <p className="card-sub">Refreshing resident snapshot...</p>
                    ) : null}
                </div>
            ) : (
                <p className="card-sub">
                    Pick a resident from the registry, then enroll them in study, graduate them to the next level, or
                    withdraw them from study through this classic-city service workspace.
                </p>
            )}
        </section>
    );
}

const CityEducationPage = () => {
    const params = useParams<{ cityId: string }>();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const cityId = params.cityId ?? "";
    const focusResidentId = searchParams.get("residentId") ?? "";

    const [selectedResidentId, setSelectedResidentId] = useState(focusResidentId);
    const [selectedTargetEducationLevel, setSelectedTargetEducationLevel] = useState("");
    const [refreshNonce, setRefreshNonce] = useState(0);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [operationError, setOperationError] = useState<string | null>(null);
    const [operationResult, setOperationResult] = useState<CityEducationOperationResultDto | null>(null);

    const cityQuery = useCityDetails(cityId);
    const residentQuery = useCityResidentDetails(cityId, selectedResidentId, selectedResidentId.length > 0);
    const residentsQuery = usePagedQuery<PersonDto>(
        (pageNumber, pageSize) => getCityResidentsPage(cityId, pageNumber, pageSize),
        PAGE_SIZE,
        [cityId, refreshNonce],
        {
            enabled: cityId.length > 0,
            errorMessage: "Failed to load city residents for education service.",
        },
    );

    useEffect(() => {
        if (!focusResidentId) {
            return;
        }

        setSelectedResidentId(focusResidentId);
        setOperationError(null);
        setOperationResult(null);
    }, [focusResidentId]);

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);

    if (!cityId) {
        return <Navigate to={CLASSIC_CITY_LIST_PATH} replace/>;
    }

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

    const residents = residentsQuery.data?.items ?? [];
    const total = residentsQuery.data?.totalCount ?? 0;
    const totalPages = residentsQuery.data?.totalPages ?? 1;
    const currentPage = residentsQuery.data?.pageNumber ?? residentsQuery.pageNumber;
    const pageSize = residentsQuery.data?.pageSize ?? PAGE_SIZE;
    const range = getPageRange(currentPage, pageSize, total);
    const selectedResident = residentQuery.data;
    const nextLevels = selectedResident
        ? NEXT_EDUCATION_LEVELS[selectedResident.educationLevel] ?? []
        : [];

    useEffect(() => {
        if (!selectedResident) {
            setSelectedTargetEducationLevel("");
            return;
        }

        setSelectedTargetEducationLevel((current) => {
            if (current && nextLevels.includes(current)) {
                return current;
            }

            return nextLevels[0] ?? "";
        });
    }, [nextLevels, selectedResident]);

    const canEnrollResident =
        selectedResidentId.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.employmentStatus !== "Student" &&
        selectedResident?.employmentStatus !== "Retired" &&
        selectedResident?.ageGroup !== "Senior" &&
        !isArchived &&
        !isSubmitting;

    const canWithdrawResident =
        selectedResidentId.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.employmentStatus === "Student" &&
        !isArchived &&
        !isSubmitting;

    const canGraduateResident =
        selectedResidentId.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.employmentStatus === "Student" &&
        nextLevels.includes(selectedTargetEducationLevel) &&
        !isArchived &&
        !isSubmitting;

    const headerLinks = useMemo(() => {
        const links: Array<{ to: string; label: string }> = [];

        if (focusResidentId) {
            links.push({to: getClassicCityResidentDossierPath(cityId, focusResidentId), label: "Back to resident"});
        }

        links.push(
            {to: getClassicCityResidentsPath(cityId), label: "Back to residents"},
            {to: getClassicCityDetailsPath(cityId), label: "Back to city"},
            {to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"},
        );

        return links;
    }, [cityId, focusResidentId]);

    const pageTitle = cityQuery.data?.name
        ? `${cityQuery.data.name} education service`
        : "Education service";

    async function refreshSnapshots() {
        await residentQuery.refetch();
        setRefreshNonce((value) => value + 1);
    }

    async function runOperation(action: "enroll" | "graduate" | "withdraw") {
        if (!selectedResidentId) {
            return;
        }

        try {
            setIsSubmitting(true);
            setOperationError(null);

            let result: CityEducationOperationResultDto;

            if (action === "enroll") {
                result = await enrollCityResident(cityId, {
                    residentId: selectedResidentId,
                });
            } else if (action === "withdraw") {
                result = await withdrawCityResidentFromStudy(cityId, {
                    residentId: selectedResidentId,
                });
            } else {
                result = await graduateCityResident(cityId, {
                    residentId: selectedResidentId,
                    targetEducationLevel: selectedTargetEducationLevel,
                });
            }

            setOperationResult(result);
            await refreshSnapshots();
        } catch (error: unknown) {
            setOperationError(getErrorMessage(error, "Failed to run education operation."));
        } finally {
            setIsSubmitting(false);
        }
    }

    function handleSelectResident(personId: string) {
        setSelectedResidentId(personId);
        setOperationError(null);
        setOperationResult(null);
    }

    return (
        <div className="cities-page city-education-page">
            <CityDetailsHeader
                title={pageTitle}
                cityId={cityQuery.data?.cityId ?? cityId}
                simulationKind={cityQuery.data?.simulationKind}
                status={cityQuery.data?.status}
                archivedAtUtc={cityQuery.data?.archivedAtUtc}
                links={headerLinks}
            />

            {cityQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{cityQuery.error}</span>
                    <Button
                        type="button"
                        variant="primary"
                        onClick={() => {
                            void cityQuery.refetch();
                        }}
                    >
                        Retry
                    </Button>
                </div>
            ) : null}

            <section className="cities-card">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Education service</h2>
                        <p className="cities-card__subtitle">
                            Manage current study state and next education transitions through a dedicated classic-city
                            service instead of patching resident records directly.
                        </p>
                    </div>
                </div>

                {isArchived ? (
                    <div className="citycore-error-banner" role="status">
                        <span>Archived cities are read-only snapshots. Education operations are disabled.</span>
                    </div>
                ) : null}

                {operationError ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{operationError}</span>
                    </div>
                ) : null}

                {operationResult ? (
                    <div className="city-state-banner city-state-banner--active">
                        <div className="city-state-banner__title">{formatActionLabel(operationResult.action)}</div>
                        <div className="city-state-banner__text">
                            {operationResult.resident.fullName}
                            {" "}was updated on {formatTimestamp(operationResult.recordedAtUtc)}.
                        </div>
                    </div>
                ) : null}

                <div className="city-education__workspace-grid">
                    <SelectedResidentCard
                        cityId={cityId}
                        residentId={selectedResidentId}
                        resident={selectedResident}
                        isLoading={residentQuery.isLoading}
                        nextLevels={nextLevels}
                        onClear={() => setSelectedResidentId("")}
                    />

                    <section className="city-education__action-card">
                        <div className="city-education__selected-header">
                            <div>
                                <span className="city-education__selected-label">Service action</span>
                                <h3 className="city-education__selected-name">Education controls</h3>
                            </div>
                        </div>

                        <label className="city-education__field">
                            <span>Next education level</span>
                            <select
                                value={selectedTargetEducationLevel}
                                onChange={(event) => setSelectedTargetEducationLevel(event.target.value)}
                                disabled={isSubmitting || isArchived || !selectedResidentId || nextLevels.length === 0}
                            >
                                {nextLevels.length === 0 ? (
                                    <option value="">No available next level</option>
                                ) : (
                                    nextLevels.map((level) => (
                                        <option key={level} value={level}>
                                            {formatEducationLevel(level)}
                                        </option>
                                    ))
                                )}
                            </select>
                        </label>

                        <div className="city-education__meta">
                            <span>Children, youths, and adults can be marked as students in the current classic-city model.</span>
                            <span>Graduation moves the resident only to the next valid education transition.</span>
                        </div>

                        <div className="city-education__action-row">
                            <Button
                                type="button"
                                variant="success"
                                disabled={!canEnrollResident}
                                onClick={() => {
                                    void runOperation("enroll");
                                }}
                            >
                                Enroll in study
                            </Button>

                            <Button
                                type="button"
                                variant="primary"
                                disabled={!canGraduateResident}
                                onClick={() => {
                                    void runOperation("graduate");
                                }}
                            >
                                Graduate resident
                            </Button>

                            <Button
                                type="button"
                                variant="danger"
                                disabled={!canWithdrawResident}
                                onClick={() => {
                                    void runOperation("withdraw");
                                }}
                            >
                                Withdraw from study
                            </Button>

                            <Button
                                type="button"
                                disabled={isSubmitting || !selectedResidentId}
                                onClick={() => {
                                    setSelectedResidentId(focusResidentId);
                                    setSelectedTargetEducationLevel("");
                                    setOperationError(null);
                                    setOperationResult(null);
                                }}
                            >
                                Reset selection
                            </Button>
                        </div>
                    </section>
                </div>
            </section>

            <section className="cities-card">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Resident selection</h2>
                        <p className="cities-card__subtitle">
                            Pick a resident from the city registry, then run the education service above.
                        </p>
                    </div>
                </div>

                <div className="city-education__toolbar">
                    <div className="city-education__summary">
                        {residentsQuery.isLoading ? (
                            <span className="card-sub">Loading residents...</span>
                        ) : null}

                        {!residentsQuery.isLoading && !residentsQuery.error && total > 0 ? (
                            <span className="card-sub">
                                Showing {range.start}-{range.end} of {total} residents
                            </span>
                        ) : null}
                    </div>

                    <Pagination
                        page={residentsQuery.pageNumber}
                        totalPages={Math.max(1, totalPages)}
                        onChange={residentsQuery.setPageNumber}
                        disabled={residentsQuery.isLoading || isSubmitting}
                    />
                </div>

                {residentsQuery.error ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{residentsQuery.error}</span>
                    </div>
                ) : null}

                {residents.length > 0 ? (
                    <>
                        <div className="city-education__resident-grid">
                            {residents.map((resident) => {
                                const isSelected = selectedResidentId === resident.id;
                                const nextResidentLevels = NEXT_EDUCATION_LEVELS[resident.educationLevel] ?? [];

                                return (
                                    <article
                                        key={resident.id}
                                        className={`city-education__resident-card${isSelected ? " city-education__resident-card--selected" : ""}`}
                                    >
                                        <div className="city-education__resident-copy">
                                            <div className="city-education__resident-topline">
                                                <h3>{resident.fullName}</h3>
                                                {isSelected ? (
                                                    <span className="city-education__tag">Selected</span>
                                                ) : null}
                                            </div>

                                            <p className="card-sub">
                                                {resident.sex}, {resident.age} y.o. ({resident.ageGroup})
                                            </p>

                                            <dl className="city-education__resident-facts">
                                                <div>
                                                    <dt>Study status</dt>
                                                    <dd>{resident.employmentStatus === "Student" ? "Studying now" : resident.employmentStatus}</dd>
                                                </div>
                                                <div>
                                                    <dt>Education</dt>
                                                    <dd>{formatEducationLevel(resident.educationLevel)}</dd>
                                                </div>
                                                <div>
                                                    <dt>Next step</dt>
                                                    <dd>
                                                        {nextResidentLevels.length > 0
                                                            ? nextResidentLevels.map(formatEducationLevel).join(" / ")
                                                            : "Final level reached"}
                                                    </dd>
                                                </div>
                                            </dl>
                                        </div>

                                        <div className="city-education__resident-actions">
                                            <Button
                                                type="button"
                                                size="sm"
                                                variant={isSelected ? "success" : "default"}
                                                disabled={isSelected || isSubmitting}
                                                onClick={() => handleSelectResident(resident.id)}
                                            >
                                                {isSelected ? "Resident selected" : "Use resident"}
                                            </Button>

                                            <Button
                                                type="button"
                                                size="sm"
                                                onClick={() => navigate(getClassicCityResidentDossierPath(cityId, resident.id))}
                                            >
                                                Open dossier
                                            </Button>
                                        </div>
                                    </article>
                                );
                            })}
                        </div>

                        <div className="city-education__pagination-bottom">
                            <Pagination
                                page={residentsQuery.pageNumber}
                                totalPages={Math.max(1, totalPages)}
                                onChange={residentsQuery.setPageNumber}
                                disabled={residentsQuery.isLoading || isSubmitting}
                            />
                        </div>
                    </>
                ) : null}

                {!residentsQuery.isLoading && !residentsQuery.error && total === 0 ? (
                    <div className="city-state-banner city-state-banner--active">
                        <div className="city-state-banner__title">No residents available</div>
                        <div className="city-state-banner__text">
                            Education services need at least one resident inside this city registry.
                        </div>
                    </div>
                ) : null}
            </section>
        </div>
    );
};

export default CityEducationPage;
