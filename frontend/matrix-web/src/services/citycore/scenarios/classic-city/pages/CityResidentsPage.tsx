import {useMemo, useState} from "react";
import {Navigate, useParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {getCityResidentsPage} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
} from "@services/citycore/scenarios/registry";
import {
    getCityStatusTone,
    isArchivedCity,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import type {PersonDto} from "@services/population/person/api/personTypes";
import CitizenCard from "@services/population/person/components/CitizenCard";
import CitizenDetailsModal from "@services/population/person/components/CitizenDetailsModal";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import {getPageRange} from "@shared/lib/paging/pageRange";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-residents.css";

const PAGE_SIZE = 60;

const CityResidentsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const cityQuery = useCityDetails(cityId);
    const {can} = usePermissions();
    const [refreshNonce, setRefreshNonce] = useState(0);
    const [selectedPerson, setSelectedPerson] = useState<PersonDto | null>(null);

    const residentsQuery = usePagedQuery<PersonDto>(
        (pageNumber, pageSize) => getCityResidentsPage(cityId, pageNumber, pageSize),
        PAGE_SIZE,
        [cityId, refreshNonce],
        {
            enabled: cityId.length > 0,
            errorMessage: "Failed to load city residents.",
        },
    );

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const canUpdate = can(PermissionKeys.PopulationPersonUpdate) && !isArchived;
    const canKill = can(PermissionKeys.PopulationPersonKill) && !isArchived;
    const canResurrect = can(PermissionKeys.PopulationPersonResurrect) && !isArchived;

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

    const residents = residentsQuery.data?.items ?? [];
    const total = residentsQuery.data?.totalCount ?? 0;
    const totalPages = residentsQuery.data?.totalPages ?? 1;
    const currentPage = residentsQuery.data?.pageNumber ?? residentsQuery.pageNumber;
    const pageSize = residentsQuery.data?.pageSize ?? PAGE_SIZE;
    const range = getPageRange(currentPage, pageSize, total);

    const headerLinks = useMemo(() => {
        if (!cityId) {
            return [{to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"}];
        }

        return [
            {to: getClassicCityDetailsPath(cityId), label: "Back to city"},
            {to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"},
        ];
    }, [cityId]);

    const pageTitle = cityQuery.data?.name
        ? `${cityQuery.data.name} residents`
        : "City residents";

    return (
        <div className="cities-page city-residents-page">
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
                        <h2 className="cities-card__title">Resident registry</h2>
                        <p className="cities-card__subtitle">
                            Browse and inspect only the people assigned to this simulation host.
                        </p>
                    </div>
                </div>

                <div className="city-residents-page__toolbar">
                    <div className="city-residents-page__summary">
                        {residentsQuery.isLoading ? (
                            <span className="card-sub">Loading residents...</span>
                        ) : null}

                        {!residentsQuery.isLoading && !residentsQuery.error && total > 0 ? (
                            <span className="card-sub">
                                Showing {range.start}-{range.end} of {total} residents
                            </span>
                        ) : null}

                        {isArchived ? (
                            <span className="card-sub">
                                This city is archived. Resident actions are read-only.
                            </span>
                        ) : null}
                    </div>

                    <Pagination
                        page={residentsQuery.pageNumber}
                        totalPages={Math.max(1, totalPages)}
                        onChange={residentsQuery.setPageNumber}
                        disabled={residentsQuery.isLoading}
                    />
                </div>

                {residentsQuery.error ? (
                    <div className="citycore-error-banner" role="alert">
                        <span>{residentsQuery.error}</span>
                    </div>
                ) : null}

                {residents.length > 0 ? (
                    <>
                        <div className="city-residents-page__cards">
                            {residents.map((resident) => (
                                <CitizenCard
                                    key={resident.id}
                                    person={resident}
                                    onOpen={setSelectedPerson}
                                />
                            ))}
                        </div>

                        <Pagination
                            page={residentsQuery.pageNumber}
                            totalPages={Math.max(1, totalPages)}
                            onChange={residentsQuery.setPageNumber}
                            disabled={residentsQuery.isLoading}
                        />
                    </>
                ) : null}

                {!residentsQuery.isLoading && !residentsQuery.error && total === 0 ? (
                    <div className="city-state-banner city-state-banner--active">
                        <div className="city-state-banner__title">No residents yet</div>
                        <div className="city-state-banner__text">
                            This city currently has no registered residents. If it is still bootstrapping,
                            return to provisioning. Otherwise inspect the population bootstrap result.
                        </div>
                    </div>
                ) : null}
            </section>

            <CitizenDetailsModal
                person={selectedPerson}
                isOpen={selectedPerson !== null}
                onClose={() => setSelectedPerson(null)}
                canUpdate={canUpdate}
                canKill={canKill}
                canResurrect={canResurrect}
                readOnlyMessage={
                    isArchived
                        ? "Archived cities are read-only snapshots. Resident actions are disabled."
                        : null
                }
                onPersonUpdated={(updatedPerson) => {
                    setSelectedPerson(updatedPerson);
                    setRefreshNonce((value) => value + 1);
                }}
            />
        </div>
    );
};

export default CityResidentsPage;
