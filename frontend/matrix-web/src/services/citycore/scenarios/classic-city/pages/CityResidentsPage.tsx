import {Navigate, useNavigate, useParams} from "react-router-dom";
import {getCityResidentsPage} from "@services/citycore/scenarios/classic-city/api/residentsApi";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
} from "@services/citycore/scenarios/registry";
import {getCityStatusTone, isArchivedCity,} from "@services/citycore/scenarios/classic-city/utils/presentation";
import type {PersonDto} from "@services/population/person/api/personTypes";
import CitizenCard from "@services/population/person/components/CitizenCard";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import {usePagedQuery} from "@shared/lib/paging/usePagedQuery";
import {getPageRange} from "@shared/lib/paging/pageRange";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-residents.css";

const PAGE_SIZE = 100;

const CityResidentsPage = () => {
    const params = useParams<{ cityId: string }>();
    const cityId = params.cityId ?? "";
    const navigate = useNavigate();
    const cityQuery = useCityDetails(cityId);

    const residentsQuery = usePagedQuery<PersonDto>(
        (pageNumber, pageSize) => getCityResidentsPage(cityId, pageNumber, pageSize),
        PAGE_SIZE,
        [cityId],
        {
            enabled: cityId.length > 0,
            errorMessage: "Failed to load city residents.",
        },
    );

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

    const residents = residentsQuery.data?.items ?? [];
    const total = residentsQuery.data?.totalCount ?? 0;
    const totalPages = residentsQuery.data?.totalPages ?? 1;
    const currentPage = residentsQuery.data?.pageNumber ?? residentsQuery.pageNumber;
    const pageSize = residentsQuery.data?.pageSize ?? PAGE_SIZE;
    const range = getPageRange(currentPage, pageSize, total);

    return (
        <div className="cities-page city-residents-page">
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
                                    onOpen={(selectedResident) => {
                                        navigate(getClassicCityResidentDossierPath(cityId, selectedResident.id));
                                    }}
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
        </div>
    );
};

export default CityResidentsPage;
