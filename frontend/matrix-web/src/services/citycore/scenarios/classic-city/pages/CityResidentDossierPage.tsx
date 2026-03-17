import {useEffect, useMemo} from "react";
import {Link, Navigate, useParams, useSearchParams} from "react-router-dom";
import {CityDetailsHeader} from "@services/citycore/scenarios/classic-city/components/CityDetailsHeader";
import {useCityDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityDetails";
import {useCityResidentDetails} from "@services/citycore/scenarios/classic-city/hooks/useCityResidentDetails";
import {
    CLASSIC_CITY_LIST_PATH,
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCityResidentDossierPath,
    getClassicCityResidentsPath,
} from "@services/citycore/scenarios/registry";
import {getCityStatusTone, isArchivedCity,} from "@services/citycore/scenarios/classic-city/utils/presentation";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/scenarios/classic-city/styles/cities.css";
import "@services/citycore/scenarios/classic-city/styles/city-details.css";
import "@services/citycore/scenarios/classic-city/styles/city-resident-dossier.css";

type ResidentDossierTabKey = "overview" | "relationships" | "career" | "education" | "health";

const DOSSIER_TABS: ReadonlyArray<{ key: ResidentDossierTabKey; label: string; helper: string }> = [
    {key: "overview", label: "Overview", helper: "Current snapshot of this resident inside the city."},
    {key: "relationships", label: "Relationships", helper: "Current spouse, parents, children, and household context."},
    {key: "career", label: "Career", helper: "Current employment and workplace context."},
    {key: "education", label: "Education", helper: "Current level until education history arrives."},
    {key: "health", label: "Health", helper: "Live wellbeing and pressure metrics."},
];

function isResidentDossierTabKey(value: string | null): value is ResidentDossierTabKey {
    return DOSSIER_TABS.some((tab) => tab.key === value);
}

function renderLifecycleHint(text: string) {
    return <div className="city-resident-dossier__hint">{text}</div>;
}

function formatHouseholdLabel(householdId?: string | null) {
    if (!householdId) {
        return "Unknown household";
    }

    return `Household ${householdId.slice(0, 8)}`;
}

function formatWorkplaceLabel(workplaceId?: string | null) {
    if (!workplaceId) {
        return "No current workplace";
    }

    return `Workplace ${workplaceId.slice(0, 8)}`;
}

function formatEducationLevel(level: string) {
    return level.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatLabel(value: string) {
    return value.replace(/([a-z])([A-Z])/g, "$1 $2");
}

function formatInstitutionLabel(institutionId?: string | null) {
    if (!institutionId) {
        return "No current institution";
    }

    return `Institution ${institutionId.slice(0, 8)}`;
}

function renderResidentReference(
    cityId: string,
    resident?: { id: string; fullName: string } | null,
    fallback = "Unknown"
) {
    if (!resident) {
        return fallback;
    }

    return (
        <Link
            className="city-resident-dossier__inline-link"
            to={getClassicCityResidentDossierPath(cityId, resident.id)}
        >
            {resident.fullName}
        </Link>
    );
}

function renderResidentReferenceList(
    cityId: string,
    residents: Array<{ id: string; fullName: string }>,
    emptyLabel: string,
) {
    if (residents.length === 0) {
        return <span>{emptyLabel}</span>;
    }

    return (
        <div className="city-resident-dossier__reference-list">
            {residents.map((resident) => (
                <Link
                    key={resident.id}
                    className="city-resident-dossier__reference-token"
                    to={getClassicCityResidentDossierPath(cityId, resident.id)}
                    title={resident.fullName}
                >
                    {resident.fullName}
                </Link>
            ))}
        </div>
    );
}

const CityResidentDossierPage = () => {
    const params = useParams<{ cityId: string; residentId: string }>();
    const [searchParams, setSearchParams] = useSearchParams();
    const cityId = params.cityId ?? "";
    const residentId = params.residentId ?? "";
    const cityQuery = useCityDetails(cityId);
    const residentQuery = useCityResidentDetails(cityId, residentId, cityId.length > 0 && residentId.length > 0);
    const rawTab = searchParams.get("tab");
    const activeTab: ResidentDossierTabKey = isResidentDossierTabKey(rawTab)
        ? rawTab
        : "overview";
    const activeTabMeta = useMemo(
        () => DOSSIER_TABS.find((tab) => tab.key === activeTab) ?? DOSSIER_TABS[0],
        [activeTab],
    );

    const isArchived = isArchivedCity(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const statusTone = getCityStatusTone(cityQuery.data?.status, cityQuery.data?.archivedAtUtc);
    const resident = residentQuery.data;

    useEffect(() => {
        if (rawTab === activeTab) {
            return;
        }

        const next = new URLSearchParams(searchParams);
        next.set("tab", activeTab);
        setSearchParams(next, {replace: true});
    }, [activeTab, rawTab, searchParams, setSearchParams]);

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

                {residentQuery.isLoading && !resident ? (
                    <div className="city-state-banner city-state-banner--active">
                        <div className="city-state-banner__title">Loading resident dossier</div>
                        <div className="city-state-banner__text">
                            Pulling the latest city-scoped resident snapshot, family links, and household context.
                        </div>
                    </div>
                ) : null}

                {resident ? (
                    <div className="city-resident-dossier__panel" role="tabpanel">
                        <div className="city-resident-dossier__hint">{activeTabMeta.helper}</div>
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
                                                {resident.currentSpouse
                                                    ? renderResidentReference(cityId, resident.currentSpouse)
                                                    : resident.maritalStatus === "Married"
                                                        ? "Spouse record unavailable"
                                                        : "None"}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt>Mother</dt>
                                            <dd>{renderResidentReference(cityId, resident.mother, "Not recorded")}</dd>
                                        </div>
                                        <div>
                                            <dt>Father</dt>
                                            <dd>{renderResidentReference(cityId, resident.father, "Not recorded")}</dd>
                                        </div>
                                        <div>
                                            <dt>Children</dt>
                                            <dd>{resident.children.length > 0 ? `${resident.children.length} linked` : "No linked children"}</dd>
                                        </div>
                                        {resident.lastChildbirthDate ? (
                                            <div>
                                                <dt>Last childbirth</dt>
                                                <dd>{resident.lastChildbirthDate}</dd>
                                            </div>
                                        ) : null}
                                        <div>
                                            <dt>Household</dt>
                                            <dd>{formatHouseholdLabel(resident.currentHousing.householdId)}</dd>
                                        </div>
                                        <div>
                                            <dt>Housing</dt>
                                            <dd>{resident.currentHousing.housingStatus}</dd>
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
                                            <dt>Current workplace</dt>
                                            <dd>
                                                {resident.currentWorkplace ? (
                                                    <span
                                                        className="city-resident-dossier__entity-token"
                                                        title={resident.currentWorkplace.workplaceId}
                                                    >
                                                        {formatWorkplaceLabel(resident.currentWorkplace.workplaceId)}
                                                    </span>
                                                ) : "No current workplace"}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt>Education</dt>
                                            <dd>{formatEducationLevel(resident.educationLevel)}</dd>
                                        </div>
                                        <div>
                                            <dt>Current institution</dt>
                                            <dd>
                                                {resident.currentEducationInstitution ? (
                                                    <span
                                                        className="city-resident-dossier__entity-token"
                                                        title={resident.currentEducationInstitution.institutionId}
                                                    >
                                                        {formatInstitutionLabel(resident.currentEducationInstitution.institutionId)}
                                                    </span>
                                                ) : "No current institution"}
                                            </dd>
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
                                        <div>
                                            <dt>Current illness</dt>
                                            <dd>
                                                {resident.currentIllness
                                                    ? `${formatLabel(resident.currentIllness.kind)} (${resident.currentIllness.severity.toLowerCase()})`
                                                    : "No active illness"}
                                            </dd>
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
                                                {resident.currentSpouse
                                                    ? renderResidentReference(cityId, resident.currentSpouse)
                                                    : resident.maritalStatus === "Married"
                                                        ? "Spouse record unavailable"
                                                        : "No current spouse"}
                                            </dd>
                                        </div>
                                        <div>
                                            <dt>Current household</dt>
                                            <dd>{formatHouseholdLabel(resident.currentHousing.householdId)}</dd>
                                        </div>
                                        <div>
                                            <dt>Housing status</dt>
                                            <dd>{resident.currentHousing.housingStatus}</dd>
                                        </div>
                                    </dl>
                                </section>
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Family links</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Mother</dt>
                                            <dd>{renderResidentReference(cityId, resident.mother, "Not recorded")}</dd>
                                        </div>
                                        <div>
                                            <dt>Father</dt>
                                            <dd>{renderResidentReference(cityId, resident.father, "Not recorded")}</dd>
                                        </div>
                                        <div>
                                            <dt>Children</dt>
                                            <dd>
                                                {renderResidentReferenceList(
                                                    cityId,
                                                    resident.children,
                                                    "No linked children",
                                                )}
                                            </dd>
                                        </div>
                                        {resident.lastChildbirthDate ? (
                                            <div>
                                                <dt>Last childbirth</dt>
                                                <dd>{resident.lastChildbirthDate}</dd>
                                            </div>
                                        ) : null}
                                    </dl>
                                </section>
                                {renderLifecycleHint("Marriage, divorce, widowhood, births, and wider family history can keep growing in this tab once relationship services start recording deeper timelines.")}
                            </div>
                        ) : null}

                        {activeTab === "career" ? (
                            <div className="city-resident-dossier__stack">
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
                                        <div>
                                            <dt>Current workplace</dt>
                                            <dd>
                                                {resident.currentWorkplace ? (
                                                    <span
                                                        className="city-resident-dossier__entity-token"
                                                        title={resident.currentWorkplace.workplaceId}
                                                    >
                                                        {formatWorkplaceLabel(resident.currentWorkplace.workplaceId)}
                                                    </span>
                                                ) : "No current workplace"}
                                            </dd>
                                        </div>
                                    </dl>
                                </section>
                                {renderLifecycleHint("Career history, workplace transfers, and hire/fire event streams can land here later without changing the current classic-city employment workspace.")}
                            </div>
                        ) : null}

                        {activeTab === "education" ? (
                            <div className="city-resident-dossier__stack">
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current education</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Education level</dt>
                                            <dd>{formatEducationLevel(resident.educationLevel)}</dd>
                                        </div>
                                        <div>
                                            <dt>Study status</dt>
                                            <dd>{resident.employmentStatus === "Student" ? "Currently studying" : "Not currently studying"}</dd>
                                        </div>
                                        <div>
                                            <dt>Current institution</dt>
                                            <dd>
                                                {resident.currentEducationInstitution ? (
                                                    <span
                                                        className="city-resident-dossier__entity-token"
                                                        title={resident.currentEducationInstitution.institutionId}
                                                    >
                                                        {formatInstitutionLabel(resident.currentEducationInstitution.institutionId)}
                                                    </span>
                                                ) : "No current institution"}
                                            </dd>
                                        </div>
                                    </dl>
                                </section>
                                {renderLifecycleHint("Enrollment, graduation, and transfer history will appear here once education services start emitting resident events.")}
                            </div>
                        ) : null}

                        {activeTab === "health" ? (
                            <div className="city-resident-dossier__stack">
                                <section className="city-resident-dossier__section-card">
                                    <h3 className="city-resident-dossier__section-title">Current condition</h3>
                                    <dl className="city-resident-dossier__facts">
                                        <div>
                                            <dt>Active illness</dt>
                                            <dd>
                                                {resident.currentIllness ? (
                                                    <span className="city-resident-dossier__entity-token">
                                                        {formatLabel(resident.currentIllness.kind)} / {resident.currentIllness.severity}
                                                    </span>
                                                ) : "No active illness"}
                                            </dd>
                                        </div>
                                        {resident.currentIllness ? (
                                            <div>
                                                <dt>Diagnosed on</dt>
                                                <dd>{resident.currentIllness.diagnosedOn}</dd>
                                            </div>
                                        ) : null}
                                        {resident.lastIllnessRecoveredOn ? (
                                            <div>
                                                <dt>Last recovery</dt>
                                                <dd>{resident.lastIllnessRecoveredOn}</dd>
                                            </div>
                                        ) : null}
                                    </dl>
                                </section>
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
                                {renderLifecycleHint("Illnesses now surface as part of the live resident snapshot. Treatment history, recurring conditions, and longer medical episodes can keep growing here later without changing the rest of the dossier layout.")}
                            </div>
                        ) : null}
                    </div>
                ) : null}
            </section>
        </div>
    );
};

export default CityResidentDossierPage;
