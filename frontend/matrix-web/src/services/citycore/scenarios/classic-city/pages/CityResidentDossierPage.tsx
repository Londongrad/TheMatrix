import {useMemo, useState} from "react";
import {Link, Navigate, useNavigate, useParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityCivilRegistryPath,
    getClassicCityDetailsPath,
    getClassicCityEmploymentPath,
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
    getClassicCityResidentsPath,
} from "@services/citycore/scenarios/registry";
import {
    getCityStatusTone,
    isArchivedCity,
} from "@services/citycore/scenarios/classic-city/utils/presentation";
import Button from "@shared/ui/controls/Button/Button";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-resident-dossier.css";

type ResidentDossierTabKey = "overview" | "relationships" | "career" | "education" | "health";

const DOSSIER_TABS: ReadonlyArray<{ key: ResidentDossierTabKey; label: string; helper: string }> = [
    {key: "overview", label: "Overview", helper: "Current snapshot of this resident inside the city."},
    {key: "relationships", label: "Relationships", helper: "Current marital state and spouse link."},
    {key: "career", label: "Career", helper: "Current employment status until work services land."},
    {key: "education", label: "Education", helper: "Current level until education history arrives."},
    {key: "health", label: "Health", helper: "Live wellbeing and pressure metrics."},
];

function renderLifecycleHint(text: string) {
    return <div className="city-resident-dossier__hint">{text}</div>;
}

const CityResidentDossierPage = () => {
    const params = useParams<{ cityId: string; residentId: string }>();
    const navigate = useNavigate();
    const {can} = usePermissions();
    const cityId = params.cityId ?? "";
    const residentId = params.residentId ?? "";

    const [activeTab, setActiveTab] = useState<ResidentDossierTabKey>("overview");
    const cityQuery = useCityDetails(cityId);
    const residentQuery = useCityResidentDetails(cityId, residentId, cityId.length > 0 && residentId.length > 0);

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const resident = residentQuery.data;
    const canManageCivilRegistry = can(PermissionKeys.PopulationCivilRegistryManage) && !isArchived;
    const canManageEmployment = can(PermissionKeys.PopulationEmploymentManage) && !isArchived;

    if (!cityId || !residentId) {
        return <Navigate to={CLASSIC_CITY_LIST_PATH} replace/>;
    }

    if (cityQuery.data && (statusTone === "provisioning" || statusTone === "failed")) {
        return <Navigate to={getClassicCityProvisioningPath(cityQuery.data.cityId)} replace/>;
    }

    const headerLinks = useMemo(
        () => [
            {to: getClassicCityResidentsPath(cityId), label: "Back to residents"},
            {to: getClassicCityDetailsPath(cityId), label: "Back to city"},
            {to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"},
        ],
        [cityId],
    );

    const pageTitle = resident?.fullName ?? "Resident dossier";

    return (
        <div className="cities-page city-resident-dossier-page">
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

            {residentQuery.error ? (
                <div className="citycore-error-banner" role="alert">
                    <span>{residentQuery.error}</span>
                    <Button
                        type="button"
                        variant="primary"
                        onClick={() => {
                            void residentQuery.refetch();
                        }}
                    >
                        Retry
                    </Button>
                </div>
            ) : null}

            <section className="cities-card">
                <div className="cities-card__header city-resident-dossier__header">
                    <div>
                        <h2 className="cities-card__title">Resident dossier</h2>
                        <p className="cities-card__subtitle">
                            Keep the quick modal for inspection, then use this workspace to navigate a fuller snapshot
                            of the resident until dedicated city services start writing real histories.
                        </p>
                    </div>

                    <div className="city-resident-dossier__summary-chips">
                        <span className="city-resident-dossier__chip">
                            {resident?.lifeStatus ?? "Unknown status"}
                        </span>
                        <span className="city-resident-dossier__chip">
                            {resident ? `${resident.age} y.o. (${resident.ageGroup})` : "Snapshot pending"}
                        </span>
                        {isArchived ? (
                            <span className="city-resident-dossier__chip city-resident-dossier__chip--muted">
                                Archived city snapshot
                            </span>
                        ) : null}
                    </div>
                </div>

                <div className="city-resident-dossier__tablist" role="tablist" aria-label="Resident dossier sections">
                    {DOSSIER_TABS.map((tab) => {
                        const isSelected = tab.key === activeTab;

                        return (
                            <button
                                key={tab.key}
                                type="button"
                                role="tab"
                                aria-selected={isSelected}
                                className={`city-resident-dossier__tab${isSelected ? " city-resident-dossier__tab--active" : ""}`}
                                onClick={() => setActiveTab(tab.key)}
                            >
                                <span className="city-resident-dossier__tab-label">{tab.label}</span>
                                <span className="city-resident-dossier__tab-helper">{tab.helper}</span>
                            </button>
                        );
                    })}
                </div>

                {residentQuery.isLoading && !resident ? (
                    <div className="city-state-banner city-state-banner--active">
                        <div className="city-state-banner__title">Loading resident dossier</div>
                        <div className="city-state-banner__text">
                            Pulling the latest city-scoped resident snapshot and spouse reference.
                        </div>
                    </div>
                ) : null}

                {resident ? (
                    <div className="city-resident-dossier__panel" role="tabpanel">
                        {activeTab === "overview" ? (
                            <div className="city-resident-dossier__grid">
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Identity</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Birth date</dt>
                                            <dd>{resident.birthDate}</dd>
                                        </div>
                                        <div>
                                            <dt>Sex</dt>
                                            <dd>{resident.sex}</dd>
                                        </div>
                                        <div>
                                            <dt>Life status</dt>
                                            <dd>{resident.lifeStatus}</dd>
                                        </div>
                                        <div>
                                            <dt>Marital status</dt>
                                            <dd>{resident.maritalStatus}</dd>
                                        </div>
                                        <div>
                                            <dt>Current spouse</dt>
                                            <dd>
                                                {resident.currentSpouse ? (
                                                    <Link
                                                        className="city-resident-dossier__inline-link"
                                                        to={getClassicCityResidentDossierPath(cityId, resident.currentSpouse.id)}
                                                    >
                                                        {resident.currentSpouse.fullName}
                                                    </Link>
                                                ) : resident.maritalStatus === "Married" ? "Spouse record unavailable" : "None"}
                                            </dd>
                                        </div>
                                    </dl>
                                </section>

                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current snapshot</h3>
                                    <dl className="city-resident-dossier__facts">
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
                                        <div>
                                            <dt>Health / Happiness</dt>
                                            <dd>{resident.health} / {resident.happiness}</dd>
                                        </div>
                                        <div>
                                            <dt>Energy / Stress</dt>
                                            <dd>{resident.energy} / {resident.stress}</dd>
                                        </div>
                                        <div>
                                            <dt>Social need</dt>
                                            <dd>{resident.socialNeed}</dd>
                                        </div>
                                        {resident.deathDate ? (
                                            <div>
                                                <dt>Death date</dt>
                                                <dd>{resident.deathDate}</dd>
                                            </div>
                                        ) : null}
                                    </dl>
                                </section>
                            </div>
                        ) : null}

                        {activeTab === "relationships" ? (
                            <div className="city-resident-dossier__stack">
                                {canManageCivilRegistry && resident ? (
                                    <div className="city-resident-dossier__actions">
                                        <Button
                                            type="button"
                                            variant="primary"
                                            onClick={() => navigate(getClassicCityCivilRegistryPath(cityId, resident.id))}
                                        >
                                            Open civil registry
                                        </Button>
                                    </div>
                                ) : null}
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current relationship state</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Marital status</dt>
                                            <dd>{resident.maritalStatus}</dd>
                                        </div>
                                        <div>
                                            <dt>Current spouse</dt>
                                            <dd>
                                                {resident.currentSpouse ? (
                                                    <Link
                                                        className="city-resident-dossier__inline-link"
                                                        to={getClassicCityResidentDossierPath(cityId, resident.currentSpouse.id)}
                                                    >
                                                        {resident.currentSpouse.fullName}
                                                    </Link>
                                                ) : resident.maritalStatus === "Married" ? "Spouse record unavailable" : "No current spouse"}
                                            </dd>
                                        </div>
                                    </dl>
                                </section>
                                {renderLifecycleHint("Marriage, divorce, widowhood, and household change history will appear here once dedicated city relationship services start recording events.")}
                            </div>
                        ) : null}

                        {activeTab === "career" ? (
                            <div className="city-resident-dossier__stack">
                                {canManageEmployment && resident ? (
                                    <div className="city-resident-dossier__actions">
                                        <Button
                                            type="button"
                                            variant="primary"
                                            onClick={() => navigate(getClassicCityEmploymentPath(cityId, resident.id))}
                                        >
                                            Open employment service
                                        </Button>
                                    </div>
                                ) : null}
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current employment</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Status</dt>
                                            <dd>{resident.employmentStatus}</dd>
                                        </div>
                                        <div>
                                            <dt>Job title</dt>
                                            <dd>{resident.jobTitle?.trim() ? resident.jobTitle : "No current job title"}</dd>
                                        </div>
                                    </dl>
                                </section>
                                {renderLifecycleHint("Career history, workplace transfers, and hire/fire events will land here once employment services are in place.")}
                            </div>
                        ) : null}

                        {activeTab === "education" ? (
                            <div className="city-resident-dossier__stack">
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current education</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Education level</dt>
                                            <dd>{resident.educationLevel}</dd>
                                        </div>
                                    </dl>
                                </section>
                                {renderLifecycleHint("Enrollment, graduation, and transfer history will appear here once education services start emitting resident events.")}
                            </div>
                        ) : null}

                        {activeTab === "health" ? (
                            <div className="city-resident-dossier__stack">
                                <section className="city-resident-dossier__metric-grid">
                                    <div className="city-resident-dossier__metric-card">
                                        <span>Health</span>
                                        <strong>{resident.health}</strong>
                                    </div>
                                    <div className="city-resident-dossier__metric-card">
                                        <span>Happiness</span>
                                        <strong>{resident.happiness}</strong>
                                    </div>
                                    <div className="city-resident-dossier__metric-card">
                                        <span>Energy</span>
                                        <strong>{resident.energy}</strong>
                                    </div>
                                    <div className="city-resident-dossier__metric-card">
                                        <span>Stress</span>
                                        <strong>{resident.stress}</strong>
                                    </div>
                                    <div className="city-resident-dossier__metric-card">
                                        <span>Social need</span>
                                        <strong>{resident.socialNeed}</strong>
                                    </div>
                                </section>
                                {renderLifecycleHint("Illnesses, treatment history, and longer health episodes can slot into this tab later without changing the rest of the dossier layout.")}
                            </div>
                        ) : null}
                    </div>
                ) : null}
            </section>
        </div>
    );
};

export default CityResidentDossierPage;
