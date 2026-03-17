import {useEffect, useState} from "react";
import {Link, Navigate, useNavigate, useParams, useSearchParams} from "react-router-dom";
import {
    getCityResidentsPage,
    registerCityDivorce,
    registerCityMarriage,
} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
} from "@services/citycore/scenarios/registry";
import {getCityStatusTone, isArchivedCity,} from "@services/citycore/scenarios/classic-city/utils/presentation";
import type {CityCivilRegistryOperationResultDto, PersonDto} from "@services/population/person/api/personTypes";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import Button from "@shared/ui/controls/Button/Button";
import {getPageRange} from "@shared/lib/paging/pageRange";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-civil-registry.css";

const PAGE_SIZE = 100;

function formatActionLabel(action: string): string {
    return action === "DivorceRegistered"
        ? "Divorce recorded"
        : "Marriage recorded";
}

function formatTimestamp(value: string): string {
    const parsed = new Date(value);

    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
}

type SelectedResidentCardProps = {
    slotLabel: string;
    residentId: string;
    isLoading: boolean;
    residentName: string;
    residentStatus: string;
    residentHousing?: { householdId: string; housingStatus: string } | null;
    residentSpouse?: { id: string; fullName: string } | null;
    residentLifecycle?: string;
    onClear: () => void;
    cityId: string;
};

function formatHouseholdLabel(householdId?: string | null) {
    if (!householdId) {
        return "Unknown household";
    }

    return `Household ${householdId.slice(0, 8)}`;
}

function SelectedResidentCard({
                                  slotLabel,
                                  residentId,
                                  isLoading,
                                  residentName,
                                  residentStatus,
                                  residentHousing,
                                  residentSpouse,
                                  residentLifecycle,
                                  onClear,
                                  cityId,
                              }: SelectedResidentCardProps) {
    return (
        <section className="city-civil-registry__selected-card">
            <div className="city-civil-registry__selected-header">
                <div>
                    <span className="city-civil-registry__selected-label">{slotLabel}</span>
                    <h3 className="city-civil-registry__selected-name">
                        {residentId ? residentName : "Select a resident from the registry below"}
                    </h3>
                </div>

                {residentId ? (
                    <Button type="button" size="sm" onClick={onClear}>
                        Clear
                    </Button>
                ) : null}
            </div>

            {residentId ? (
                <div className="city-civil-registry__selected-body">
                    <div className="city-civil-registry__tag-row">
                        <span className="city-civil-registry__tag">{residentStatus}</span>
                        {residentLifecycle ? (
                            <span className="city-civil-registry__tag city-civil-registry__tag--muted">
                                {residentLifecycle}
                            </span>
                        ) : null}
                    </div>

                    {residentSpouse ? (
                        <p className="city-civil-registry__selected-spouse">
                            Current spouse:
                            {" "}
                            <Link
                                className="city-resident-dossier__inline-link"
                                to={getClassicCityResidentDossierPath(cityId, residentSpouse.id)}
                            >
                                {residentSpouse.fullName}
                            </Link>
                        </p>
                    ) : null}

                    {residentHousing ? (
                        <p className="city-civil-registry__selected-spouse">
                            {formatHouseholdLabel(residentHousing.householdId)}
                            {" · "}
                            {residentHousing.housingStatus}
                        </p>
                    ) : null}

                    {isLoading ? (
                        <p className="card-sub">Refreshing resident snapshot...</p>
                    ) : null}
                </div>
            ) : (
                <p className="card-sub">
                    Pick residents A and B, then register a marriage or divorce through this city service.
                </p>
            )}
        </section>
    );
}

const CityCivilRegistryPage = () => {
    const params = useParams<{ cityId: string }>();
    const navigate = useNavigate();
    const [searchParams] = useSearchParams();
    const cityId = params.cityId ?? "";
    const focusResidentId = searchParams.get("residentId") ?? "";

    const [selectedFirstResidentId, setSelectedFirstResidentId] = useState(focusResidentId);
    const [selectedSecondResidentId, setSelectedSecondResidentId] = useState("");
    const [refreshNonce, setRefreshNonce] = useState(0);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [operationError, setOperationError] = useState<string | null>(null);
    const [operationResult, setOperationResult] = useState<CityCivilRegistryOperationResultDto | null>(null);

    const cityQuery = useCityDetails(cityId);
    const firstResidentQuery = useCityResidentDetails(cityId, selectedFirstResidentId, selectedFirstResidentId.length > 0);
    const secondResidentQuery = useCityResidentDetails(cityId, selectedSecondResidentId, selectedSecondResidentId.length > 0);

    const residentsQuery = usePagedQuery<PersonDto>(
        (pageNumber, pageSize) => getCityResidentsPage(cityId, pageNumber, pageSize),
        PAGE_SIZE,
        [cityId, refreshNonce],
        {
            enabled: cityId.length > 0,
            errorMessage: "Failed to load city residents for civil registry.",
        },
    );

    useEffect(() => {
        if (!focusResidentId) {
            return;
        }

        setSelectedFirstResidentId(focusResidentId);
        setSelectedSecondResidentId((current) => current === focusResidentId ? "" : current);
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

    const canSubmitOperation =
        selectedFirstResidentId.length > 0 &&
        selectedSecondResidentId.length > 0 &&
        selectedFirstResidentId !== selectedSecondResidentId &&
        !isSubmitting &&
        !isArchived;

    async function refreshSnapshots() {
        await Promise.all([
            firstResidentQuery.refetch(),
            secondResidentQuery.refetch(),
        ]);

        setRefreshNonce((value) => value + 1);
    }

    async function handleRegisterMarriage() {
        if (!canSubmitOperation) {
            return;
        }

        try {
            setIsSubmitting(true);
            setOperationError(null);

            const result = await registerCityMarriage(cityId, {
                firstResidentId: selectedFirstResidentId,
                secondResidentId: selectedSecondResidentId,
            });

            setOperationResult(result);
            await refreshSnapshots();
        } catch (error: unknown) {
            setOperationError(error instanceof Error ? error.message : "Failed to register marriage.");
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleRegisterDivorce() {
        if (!canSubmitOperation) {
            return;
        }

        try {
            setIsSubmitting(true);
            setOperationError(null);

            const result = await registerCityDivorce(cityId, {
                firstResidentId: selectedFirstResidentId,
                secondResidentId: selectedSecondResidentId,
            });

            setOperationResult(result);
            await refreshSnapshots();
        } catch (error: unknown) {
            setOperationError(error instanceof Error ? error.message : "Failed to register divorce.");
        } finally {
            setIsSubmitting(false);
        }
    }

    function handlePickFirstResident(personId: string) {
        setSelectedFirstResidentId(personId);

        if (selectedSecondResidentId === personId) {
            setSelectedSecondResidentId("");
        }

        setOperationError(null);
        setOperationResult(null);
    }

    function handlePickSecondResident(personId: string) {
        if (selectedFirstResidentId === personId) {
            return;
        }

        setSelectedSecondResidentId(personId);
        setOperationError(null);
        setOperationResult(null);
    }

    return (
        <div className="cities-page city-civil-registry-page">
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
                        <h2 className="cities-card__title">Civil registry</h2>
                        <p className="cities-card__subtitle">
                            Register marriages and divorces through a dedicated city service instead of patching
                            resident records directly.
                        </p>
                    </div>
                </div>

                {isArchived ? (
                    <div className="citycore-error-banner" role="status">
                        <span>Archived cities are read-only snapshots. Civil registry operations are disabled.</span>
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
                            {operationResult.firstResident.fullName}
                            {" "}and{" "}
                            {operationResult.secondResident.fullName}
                            {" "}were processed on {formatTimestamp(operationResult.recordedAtUtc)}.
                        </div>
                    </div>
                ) : null}

                <div className="city-civil-registry__selected-grid">
                    <SelectedResidentCard
                        slotLabel="Resident A"
                        residentId={selectedFirstResidentId}
                        isLoading={firstResidentQuery.isLoading}
                        residentName={firstResidentQuery.data?.fullName ?? "Resident A"}
                        residentStatus={firstResidentQuery.data?.maritalStatus ?? "Snapshot pending"}
                        residentHousing={firstResidentQuery.data?.currentHousing ?? null}
                        residentSpouse={firstResidentQuery.data?.currentSpouse}
                        residentLifecycle={firstResidentQuery.data?.lifeStatus}
                        onClear={() => setSelectedFirstResidentId("")}
                        cityId={cityId}
                    />

                    <SelectedResidentCard
                        slotLabel="Resident B"
                        residentId={selectedSecondResidentId}
                        isLoading={secondResidentQuery.isLoading}
                        residentName={secondResidentQuery.data?.fullName ?? "Resident B"}
                        residentStatus={secondResidentQuery.data?.maritalStatus ?? "Snapshot pending"}
                        residentHousing={secondResidentQuery.data?.currentHousing ?? null}
                        residentSpouse={secondResidentQuery.data?.currentSpouse}
                        residentLifecycle={secondResidentQuery.data?.lifeStatus}
                        onClear={() => setSelectedSecondResidentId("")}
                        cityId={cityId}
                    />
                </div>

                <div className="city-civil-registry__action-row">
                    <Button
                        type="button"
                        variant="success"
                        disabled={!canSubmitOperation}
                        onClick={() => {
                            void handleRegisterMarriage();
                        }}
                    >
                        Register marriage
                    </Button>

                    <Button
                        type="button"
                        variant="danger"
                        disabled={!canSubmitOperation}
                        onClick={() => {
                            void handleRegisterDivorce();
                        }}
                    >
                        Register divorce
                    </Button>

                    <Button
                        type="button"
                        disabled={isSubmitting || (!selectedFirstResidentId && !selectedSecondResidentId)}
                        onClick={() => {
                            setSelectedFirstResidentId(focusResidentId);
                            setSelectedSecondResidentId("");
                            setOperationError(null);
                            setOperationResult(null);
                        }}
                    >
                        Reset selection
                    </Button>
                </div>
            </section>

            <section className="cities-card">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Resident selection</h2>
                        <p className="cities-card__subtitle">
                            Pick residents from the city registry, then run the civil registry action above.
                        </p>
                    </div>
                </div>

                <div className="city-civil-registry__toolbar">
                    <div className="city-civil-registry__summary">
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
                        <div className="city-civil-registry__resident-grid">
                            {residents.map((resident) => {
                                const isSelectedFirst = selectedFirstResidentId === resident.id;
                                const isSelectedSecond = selectedSecondResidentId === resident.id;

                                return (
                                    <article
                                        key={resident.id}
                                        className={`city-civil-registry__resident-card${
                                            isSelectedFirst || isSelectedSecond
                                                ? " city-civil-registry__resident-card--selected"
                                                : ""
                                        }`}
                                    >
                                        <div className="city-civil-registry__resident-copy">
                                            <div className="city-civil-registry__resident-topline">
                                                <h3>{resident.fullName}</h3>
                                                <div className="city-civil-registry__tag-row">
                                                    {isSelectedFirst ? (
                                                        <span className="city-civil-registry__tag">Resident A</span>
                                                    ) : null}
                                                    {isSelectedSecond ? (
                                                        <span
                                                            className="city-civil-registry__tag city-civil-registry__tag--muted">
                                                            Resident B
                                                        </span>
                                                    ) : null}
                                                </div>
                                            </div>

                                            <p className="card-sub">
                                                {resident.sex}, {resident.age} y.o. ({resident.ageGroup})
                                            </p>

                                            <dl className="city-civil-registry__resident-facts">
                                                <div>
                                                    <dt>Life status</dt>
                                                    <dd>{resident.lifeStatus}</dd>
                                                </div>
                                                <div>
                                                    <dt>Marital status</dt>
                                                    <dd>{resident.maritalStatus}</dd>
                                                </div>
                                                <div>
                                                    <dt>Employment</dt>
                                                    <dd>
                                                        {resident.employmentStatus}
                                                        {resident.jobTitle ? ` (${resident.jobTitle})` : ""}
                                                    </dd>
                                                </div>
                                            </dl>
                                        </div>

                                        <div className="city-civil-registry__resident-actions">
                                            <Button
                                                type="button"
                                                size="sm"
                                                variant={isSelectedFirst ? "success" : "default"}
                                                disabled={isSelectedFirst || selectedSecondResidentId === resident.id || isSubmitting}
                                                onClick={() => handlePickFirstResident(resident.id)}
                                            >
                                                {isSelectedFirst ? "Resident A selected" : "Use as resident A"}
                                            </Button>

                                            <Button
                                                type="button"
                                                size="sm"
                                                variant={isSelectedSecond ? "primary" : "default"}
                                                disabled={isSelectedSecond || selectedFirstResidentId === resident.id || isSubmitting}
                                                onClick={() => handlePickSecondResident(resident.id)}
                                            >
                                                {isSelectedSecond ? "Resident B selected" : "Use as resident B"}
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

                        <div className="city-civil-registry__pagination-bottom">
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
                            Civil registry operations need at least two residents in this city.
                        </div>
                    </div>
                ) : null}
            </section>
        </div>
    );
};

export default CityCivilRegistryPage;
