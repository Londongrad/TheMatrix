import {useEffect, useMemo, useRef, useState} from "react";
import {Link, Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {
    fireCityResident,
    getCityEmploymentCatalog,
    getCityResidentsPage,
    hireCityResident,
    retireCityResident,
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
    CityEmploymentCatalogDto,
    CityEmploymentOperationResultDto,
    CityResidentDetailsDto,
    PersonDto,
} from "@services/population/person/api/personTypes";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import Button from "@shared/ui/controls/Button/Button";
import {getPageRange} from "@shared/lib/paging/pageRange";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-employment.css";

const PAGE_SIZE = 100;
const JOB_TITLES_DATALIST_ID = "classic-city-employment-job-titles";

function getErrorMessage(error: unknown, fallback: string) {
    return error instanceof Error && error.message.trim().length > 0
        ? error.message
        : fallback;
}

function formatActionLabel(action: string): string {
    switch (action) {
        case "ResidentFired":
            return "Resident fired";
        case "ResidentRetired":
            return "Resident retired";
        default:
            return "Employment assigned";
    }
}

function formatTimestamp(value: string): string {
    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

function formatWorkplaceLabel(workplaceId?: string | null) {
    if (!workplaceId) {
        return "No current workplace";
    }

    return `Workplace ${workplaceId.slice(0, 8)}`;
}

type SelectedResidentCardProps = {
    cityId: string;
    residentId: string;
    resident: CityResidentDetailsDto | null;
    isLoading: boolean;
    onClear: () => void;
};

function SelectedResidentCard({
    cityId,
    residentId,
    resident,
    isLoading,
    onClear,
}: SelectedResidentCardProps) {
    return (
        <section className="city-employment__selected-card">
            <div className="city-employment__selected-header">
                <div>
                    <span className="city-employment__selected-label">Resident</span>
                    <h3 className="city-employment__selected-name">
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
                <div className="city-employment__selected-body">
                    <div className="city-employment__tag-row">
                        <span className="city-employment__tag">
                            {resident?.employmentStatus ?? "Employment pending"}
                        </span>
                        {resident?.lifeStatus ? (
                            <span className="city-employment__tag city-employment__tag--muted">
                                {resident.lifeStatus}
                            </span>
                        ) : null}
                        {resident?.ageGroup ? (
                            <span className="city-employment__tag city-employment__tag--muted">
                                {resident.ageGroup}
                            </span>
                        ) : null}
                    </div>

                    <p className="city-employment__selected-copy">
                        Current job:
                        {" "}
                        <strong>{resident?.jobTitle?.trim() ? resident.jobTitle : "No assigned title"}</strong>
                    </p>

                    <p className="city-employment__selected-copy">
                        Current workplace:
                        {" "}
                        {resident?.currentWorkplace ? (
                            <span
                                className="city-employment__entity-token"
                                title={resident.currentWorkplace.workplaceId}
                            >
                                {formatWorkplaceLabel(resident.currentWorkplace.workplaceId)}
                            </span>
                        ) : (
                            <strong>No current workplace</strong>
                        )}
                    </p>

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
                    Pick a resident from the registry, then assign employment, fire them, or retire them through this
                    classic-city service workspace.
                </p>
            )}
        </section>
    );
}

const CityEmploymentPage = () => {
    const params = useParams<{ cityId: string }>();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const cityId = params.cityId ?? "";
    const focusResidentId = searchParams.get("residentId") ?? "";

    const [selectedResidentId, setSelectedResidentId] = useState(focusResidentId);
    const [jobTitle, setJobTitle] = useState("");
    const [refreshNonce, setRefreshNonce] = useState(0);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [operationError, setOperationError] = useState<string | null>(null);
    const [operationResult, setOperationResult] = useState<CityEmploymentOperationResultDto | null>(null);
    const [catalog, setCatalog] = useState<CityEmploymentCatalogDto | null>(null);
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
            errorMessage: "Failed to load city residents for employment service.",
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

                const response = await getCityEmploymentCatalog(cityId, abortController.signal);
                setCatalog(response);
            } catch (error: unknown) {
                if (abortController.signal.aborted) {
                    return;
                }

                setCatalogError(getErrorMessage(error, "Failed to load classic city job catalog."));
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
    }, [cityId]);

    useEffect(() => {
        if (!selectedResidentId) {
            setJobTitle("");
            return;
        }

        setJobTitle(residentQuery.data?.jobTitle ?? "");
    }, [residentQuery.data?.jobTitle, selectedResidentId]);

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
    const trimmedJobTitle = jobTitle.trim();

    const canAssignEmployment =
        selectedResidentId.length > 0 &&
        trimmedJobTitle.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.ageGroup === "Adult" &&
        !isArchived &&
        !isSubmitting;

    const canFireResident =
        selectedResidentId.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.employmentStatus === "Employed" &&
        !isArchived &&
        !isSubmitting;

    const canRetireResident =
        selectedResidentId.length > 0 &&
        selectedResident?.lifeStatus === "Alive" &&
        selectedResident?.ageGroup === "Senior" &&
        selectedResident?.employmentStatus !== "Retired" &&
        selectedResident?.employmentStatus !== "None" &&
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
        ? `${cityQuery.data.name} employment service`
        : "Employment service";

    async function refreshSnapshots() {
        await residentQuery.refetch();
        setRefreshNonce((value) => value + 1);
    }

    async function runOperation(action: "hire" | "fire" | "retire") {
        if (!selectedResidentId) {
            return;
        }

        try {
            setIsSubmitting(true);
            setOperationError(null);

            let result: CityEmploymentOperationResultDto;

            if (action === "fire") {
                result = await fireCityResident(cityId, {
                    residentId: selectedResidentId,
                });
            } else if (action === "retire") {
                result = await retireCityResident(cityId, {
                    residentId: selectedResidentId,
                });
            } else {
                result = await hireCityResident(cityId, {
                    residentId: selectedResidentId,
                    jobTitle: trimmedJobTitle,
                });
            }

            setOperationResult(result);
            setJobTitle(result.resident.jobTitle ?? "");
            await refreshSnapshots();
        } catch (error: unknown) {
            setOperationError(getErrorMessage(error, "Failed to run employment operation."));
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
        <div className="cities-page city-employment-page">
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
                        <h2 className="cities-card__title">Employment service</h2>
                        <p className="cities-card__subtitle">
                            Manage hiring, firing, and retirement through a dedicated classic-city service instead of
                            patching employment fields directly on the resident.
                        </p>
                    </div>
                </div>

                {isArchived ? (
                    <div className="citycore-error-banner" role="status">
                        <span>Archived cities are read-only snapshots. Employment operations are disabled.</span>
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

                <div className="city-employment__workspace-grid">
                    <SelectedResidentCard
                        cityId={cityId}
                        residentId={selectedResidentId}
                        resident={selectedResident}
                        isLoading={residentQuery.isLoading}
                        onClear={() => setSelectedResidentId("")}
                    />

                    <section className="city-employment__action-card">
                        <div className="city-employment__selected-header">
                            <div>
                                <span className="city-employment__selected-label">Service action</span>
                                <h3 className="city-employment__selected-name">Employment controls</h3>
                            </div>
                        </div>

                        <label className="city-employment__field">
                            <span>Job title</span>
                            <input
                                type="text"
                                value={jobTitle}
                                list={JOB_TITLES_DATALIST_ID}
                                placeholder="Choose or enter a title"
                                onChange={(event) => setJobTitle(event.target.value)}
                                disabled={isSubmitting || isArchived || !selectedResidentId}
                            />
                        </label>

                        <datalist id={JOB_TITLES_DATALIST_ID}>
                            {(catalog?.jobTitles ?? []).map((title) => (
                                <option key={title} value={title}/>
                            ))}
                        </datalist>

                        <div className="city-employment__meta">
                            <span>
                                {isCatalogLoading
                                    ? "Loading classic-city profession suggestions..."
                                    : `${catalog?.jobTitles.length ?? 0} suggested titles ready`}
                            </span>
                            <span>
                                Adults can be assigned work. Seniors can be retired through this service.
                            </span>
                        </div>

                        <div className="city-employment__action-row">
                            <Button
                                type="button"
                                variant="success"
                                disabled={!canAssignEmployment}
                                onClick={() => {
                                    void runOperation("hire");
                                }}
                            >
                                {selectedResident?.employmentStatus === "Employed" ? "Reassign employment" : "Assign employment"}
                            </Button>

                            <Button
                                type="button"
                                variant="danger"
                                disabled={!canFireResident}
                                onClick={() => {
                                    void runOperation("fire");
                                }}
                            >
                                Fire resident
                            </Button>

                            <Button
                                type="button"
                                variant="primary"
                                disabled={!canRetireResident}
                                onClick={() => {
                                    void runOperation("retire");
                                }}
                            >
                                Retire resident
                            </Button>

                            <Button
                                type="button"
                                disabled={isSubmitting || (!selectedResidentId && jobTitle.length === 0)}
                                onClick={() => {
                                    setSelectedResidentId(focusResidentId);
                                    setJobTitle("");
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
                            Pick a resident from the city registry, then run the employment service above.
                        </p>
                    </div>
                </div>

                <div className="city-employment__toolbar">
                    <div className="city-employment__summary">
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
                        <div className="city-employment__resident-grid">
                            {residents.map((resident) => {
                                const isSelected = selectedResidentId === resident.id;

                                return (
                                    <article
                                        key={resident.id}
                                        className={`city-employment__resident-card${isSelected ? " city-employment__resident-card--selected" : ""}`}
                                    >
                                        <div className="city-employment__resident-copy">
                                            <div className="city-employment__resident-topline">
                                                <h3>{resident.fullName}</h3>
                                                {isSelected ? (
                                                    <span className="city-employment__tag">Selected</span>
                                                ) : null}
                                            </div>

                                            <p className="card-sub">
                                                {resident.sex}, {resident.age} y.o. ({resident.ageGroup})
                                            </p>

                                            <dl className="city-employment__resident-facts">
                                                <div>
                                                    <dt>Life status</dt>
                                                    <dd>{resident.lifeStatus}</dd>
                                                </div>
                                                <div>
                                                    <dt>Employment</dt>
                                                    <dd>
                                                        {resident.employmentStatus}
                                                        {resident.jobTitle ? ` (${resident.jobTitle})` : ""}
                                                    </dd>
                                                </div>
                                                <div>
                                                    <dt>Education</dt>
                                                    <dd>{resident.educationLevel}</dd>
                                                </div>
                                            </dl>
                                        </div>

                                        <div className="city-employment__resident-actions">
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

                        <div className="city-employment__pagination-bottom">
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
                            Employment services need at least one resident inside this city registry.
                        </div>
                    </div>
                ) : null}
            </section>
        </div>
    );
};

export default CityEmploymentPage;
