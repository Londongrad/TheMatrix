import {useEffect, useRef, useState} from "react";
import {Link, Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {
    enrollCityResident,
    getCityEducationCatalog,
    getCityResidentsPage,
    graduateCityResident,
    withdrawCityResidentFromStudy,
} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
} from "@services/citycore/scenarios/registry";
import {getCityStatusTone, isArchivedCity,} from "@services/citycore/scenarios/classic-city/utils/presentation";
import type {
    CityEducationCatalogDto,
    CityEducationInstitutionDto,
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
const MAX_VISIBLE_INSTITUTIONS = 18;

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

function formatInstitutionLabel(institutionId?: string | null) {
    if (!institutionId) {
        return "No current institution";
    }

    return `Institution ${institutionId.slice(0, 8)}`;
}

function formatInstitutionOccupancy(count: number) {
    return `${count} resident${count === 1 ? "" : "s"}`;
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
                                  onClear
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

                    <p className="city-education__selected-copy">
                        Current institution:
                        {" "}
                        {resident?.currentEducationInstitution ? (
                            <span
                                className="city-education__entity-token"
                                title={resident.currentEducationInstitution.institutionId}
                            >
                                {formatInstitutionLabel(resident.currentEducationInstitution.institutionId)}
                            </span>
                        ) : (
                            <strong>No assigned institution</strong>
                        )}
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
    const [selectedInstitutionId, setSelectedInstitutionId] = useState("");
    const [refreshNonce, setRefreshNonce] = useState(0);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [operationError, setOperationError] = useState<string | null>(null);
    const [operationResult, setOperationResult] = useState<CityEducationOperationResultDto | null>(null);
    const [catalog, setCatalog] = useState<CityEducationCatalogDto | null>(null);
    const [catalogError, setCatalogError] = useState<string | null>(null);
    const [isCatalogLoading, setIsCatalogLoading] = useState(false);
    const catalogAbortRef = useRef<AbortController | null>(null);

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

    useEffect(() => {
        if (!cityId) {
            setCatalog(null);
            setCatalogError(null);
            setIsCatalogLoading(false);
            return;
        }

        catalogAbortRef.current?.abort();

        const abortController = new AbortController();
        catalogAbortRef.current = abortController;

        async function loadCatalog() {
            try {
                setIsCatalogLoading(true);
                setCatalogError(null);

                const response = await getCityEducationCatalog(cityId, abortController.signal);
                setCatalog(response);
            } catch (error: unknown) {
                if (abortController.signal.aborted) {
                    return;
                }

                setCatalogError(getErrorMessage(error, "Failed to load classic city education institutions."));
            } finally {
                if (!abortController.signal.aborted) {
                    setIsCatalogLoading(false);
                }
            }
        }

        void loadCatalog();

        return () => {
            abortController.abort();
        };
    }, [cityId, refreshNonce]);

    useEffect(() => {
        if (!selectedResidentId) {
            setSelectedTargetEducationLevel("");
            setSelectedInstitutionId("");
            return;
        }

        setSelectedInstitutionId(residentQuery.data?.currentEducationInstitution?.institutionId ?? "");
    }, [residentQuery.data?.currentEducationInstitution?.institutionId, selectedResidentId]);

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);

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
    const currentInstitutions = catalog?.currentInstitutions ?? [];
    const relevantEducationLevel = selectedResident
        ? selectedResident.employmentStatus === "Student"
            ? selectedTargetEducationLevel || nextLevels[0] || ""
            : selectedResident.educationLevel
        : "";
    const relevantInstitutions = relevantEducationLevel
        ? currentInstitutions.filter((institution) => institution.educationLevel === relevantEducationLevel)
        : currentInstitutions;
    const visibleInstitutions = relevantInstitutions.slice(0, MAX_VISIBLE_INSTITUTIONS);
    const selectedInstitution = selectedInstitutionId
        ? currentInstitutions.find((institution) => institution.institutionId === selectedInstitutionId) ?? null
        : null;

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

    useEffect(() => {
        if (!selectedInstitutionId) {
            return;
        }

        const stillValid = currentInstitutions.some((institution) => (
            institution.institutionId === selectedInstitutionId &&
            (!relevantEducationLevel || institution.educationLevel === relevantEducationLevel)
        ));

        if (!stillValid) {
            setSelectedInstitutionId("");
        }
    }, [currentInstitutions, relevantEducationLevel, selectedInstitutionId]);

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

    if (!cityId) {
        return <Navigate to={CLASSIC_CITY_LIST_PATH} replace/>;
    }

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

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
                    institutionId: selectedInstitutionId || null,
                });
            } else if (action === "withdraw") {
                result = await withdrawCityResidentFromStudy(cityId, {
                    residentId: selectedResidentId,
                });
            } else {
                result = await graduateCityResident(cityId, {
                    residentId: selectedResidentId,
                    targetEducationLevel: selectedTargetEducationLevel,
                    institutionId: selectedInstitutionId || null,
                });
            }

            setOperationResult(result);
            setSelectedInstitutionId(result.resident.currentEducationInstitution?.institutionId ?? "");
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

    function handleUseInstitution(institution: CityEducationInstitutionDto) {
        setSelectedInstitutionId(institution.institutionId);
        setOperationError(null);
        setOperationResult(null);
    }

    return (
        <div className="cities-page city-education-page">
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
                            Manage current study state and place of study through a dedicated classic-city service
                            instead of patching resident records directly.
                        </p>
                    </div>
                </div>

                {isArchived ? (
                    <div className="citycore-error-banner" role="status">
                        <span>Archived cities are read-only snapshots. Education operations are disabled.</span>
                    </div>
                ) : null}

                {catalogError ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{catalogError}</span>
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
                            <span>
                                {isCatalogLoading
                                    ? "Loading classic-city study institutions..."
                                    : `${currentInstitutions.length} active institutions ready`}
                            </span>
                            <span>
                                {selectedInstitution
                                    ? `The selected institution will be reused for ${formatEducationLevel(selectedInstitution.educationLevel)}.`
                                    : relevantEducationLevel
                                        ? `Leave institution empty to create a new ${formatEducationLevel(relevantEducationLevel)} institution.`
                                        : "Select a resident to choose or create a place of study."}
                            </span>
                            <span>
                                Children, youths, and adults can study. Seniors and retired residents cannot enroll.
                            </span>
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
                                disabled={isSubmitting || (!selectedResidentId && !selectedInstitutionId)}
                                onClick={() => {
                                    setSelectedResidentId(focusResidentId);
                                    setSelectedTargetEducationLevel("");
                                    setSelectedInstitutionId("");
                                    setOperationError(null);
                                    setOperationResult(null);
                                }}
                            >
                                Reset selection
                            </Button>
                        </div>
                    </section>
                </div>

                <section className="city-education__institutions-card">
                    <div className="city-education__selected-header">
                        <div>
                            <span className="city-education__selected-label">Study institutions</span>
                            <h3 className="city-education__selected-name">Current institution network</h3>
                        </div>
                    </div>

                    {relevantInstitutions.length > 0 ? (
                        <>
                            <p className="card-sub">
                                {relevantEducationLevel
                                    ? `Showing ${Math.min(relevantInstitutions.length, MAX_VISIBLE_INSTITUTIONS)} of ${relevantInstitutions.length} institutions for ${formatEducationLevel(relevantEducationLevel)}.`
                                    : `Showing ${Math.min(relevantInstitutions.length, MAX_VISIBLE_INSTITUTIONS)} of ${relevantInstitutions.length} institutions in this city.`}
                                {" "}Choose one to reuse it instead of creating a new place of study.
                            </p>

                            <div className="city-education__institutions-grid">
                                {visibleInstitutions.map((institution) => {
                                    const isSelectedInstitution = selectedInstitutionId === institution.institutionId;

                                    return (
                                        <article
                                            key={institution.institutionId}
                                            className={`city-education__institution-card${isSelectedInstitution ? " city-education__institution-card--selected" : ""}`}
                                        >
                                            <div className="city-education__institution-copy">
                                                <strong>{formatEducationLevel(institution.educationLevel)}</strong>
                                                <span
                                                    className="city-education__entity-token"
                                                    title={institution.institutionId}
                                                >
                                                    {formatInstitutionLabel(institution.institutionId)}
                                                </span>
                                                <span className="card-sub">
                                                    {formatInstitutionOccupancy(institution.residentCount)}
                                                </span>
                                            </div>

                                            <Button
                                                type="button"
                                                size="sm"
                                                variant={isSelectedInstitution ? "success" : "default"}
                                                disabled={isSubmitting || isArchived || !selectedResidentId || isSelectedInstitution}
                                                onClick={() => handleUseInstitution(institution)}
                                            >
                                                {isSelectedInstitution ? "Selected institution" : "Use institution"}
                                            </Button>
                                        </article>
                                    );
                                })}
                            </div>

                            {relevantInstitutions.length > MAX_VISIBLE_INSTITUTIONS ? (
                                <p className="card-sub">
                                    {relevantInstitutions.length - MAX_VISIBLE_INSTITUTIONS} more institutions stay
                                    hidden
                                    for now so this workspace stays readable.
                                </p>
                            ) : null}
                        </>
                    ) : (
                        <div className="city-state-banner city-state-banner--active">
                            <div className="city-state-banner__title">No shared study institutions yet</div>
                            <div className="city-state-banner__text">
                                Enrolling or graduating a resident can create the first institution for this education
                                level, and later residents will be able to reuse it from this network.
                            </div>
                        </div>
                    )}
                </section>
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
