import {useMemo, useState} from "react";
import {Link, useNavigate} from "react-router";
import CityList from "@services/simulationcore/scenarios/classic-city/components/CityList";
import SetupSessionList from "@services/simulationcore/scenarios/classic-city/components/SetupSessionList";
import {deleteClassicCitySetupSession,} from "@services/simulationcore/scenarios/classic-city/api/setupSessionsApi";
import {CitiesToolbar} from "@services/simulationcore/scenarios/classic-city/components/CitiesToolbar";
import type {CityListItemView} from "@services/simulationcore/scenarios/classic-city/contracts/citiesContracts";
import type {
    ClassicCitySetupSessionView
} from "@services/simulationcore/scenarios/classic-city/contracts/setupSessionContracts";
import {
    useClassicCitySetupSessionsQuery
} from "@services/simulationcore/scenarios/classic-city/hooks/useClassicCitySetupSessionsQuery";
import {useCitiesQuery} from "@services/simulationcore/scenarios/classic-city/hooks/useCitiesQuery";
import {
    useProvisioningCitiesQuery
} from "@services/simulationcore/scenarios/classic-city/hooks/useProvisioningCitiesQuery";
import {getCityStatusTone} from "@services/simulationcore/scenarios/classic-city/utils/presentation";
import {
    getClassicCityDetailsPath,
    getClassicCityProvisioningPath,
    getClassicCitySetupPath,
    getClassicCitySetupSessionPath,
    SIMULATIONCORE_SCENARIO_CATALOG_PATH,
} from "@services/simulationcore/scenarios/registry";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import {usePermissions} from "@shared/permissions/usePermissions";
import {useConfirm} from "@shared/ui/components/ConfirmDialog/confirmDialogContext";
import Button from "@shared/ui/controls/Button/Button";
import "@services/simulationcore/scenarios/classic-city/styles/cities.css";

function normalize(value: string): string {
    return value.trim().toLowerCase();
}

function getCityRank(city: CityListItemView): number {
    switch (getCityStatusTone(city.status)) {
        case "failed":
            return 0;
        case "provisioning":
            return 1;
        case "active":
            return 2;
        case "unknown":
            return 3;
        case "archived":
        default:
            return 4;
    }
}

export default function CitiesPage() {
    const navigate = useNavigate();
    const confirm = useConfirm();
    const {can} = usePermissions();

    const [search, setSearch] = useState("");
    const [includeArchived, setIncludeArchived] = useState(false);
    const [setupSessionActionError, setSetupSessionActionError] = useState<string | null>(null);
    const [deletingSetupSessionId, setDeletingSetupSessionId] = useState<string | null>(null);
    const canCreateCity = can(PermissionKeys.SimulationCoreClassicCityCreate);

    const citiesQuery = useCitiesQuery(includeArchived);
    const setupSessionsQuery = useClassicCitySetupSessionsQuery();
    const provisioningQuery = useProvisioningCitiesQuery();

    const filteredSetupSessions = useMemo(() => {
        const query = normalize(search);

        if (!query) {
            return setupSessionsQuery.data;
        }

        return setupSessionsQuery.data.filter((session) => {
            const name = session.draft.name.toLowerCase();
            const sessionId = session.sessionId.toLowerCase();
            const status = session.status.toLowerCase();
            const step = session.currentStepId.toLowerCase();

            return name.includes(query) ||
                sessionId.includes(query) ||
                status.includes(query) ||
                step.includes(query);
        });
    }, [search, setupSessionsQuery.data]);

    const filteredCities = useMemo(() => {
        const query = normalize(search);

        if (!query) {
            return citiesQuery.data;
        }

        return citiesQuery.data.filter((city) => {
            const name = city.name.toLowerCase();
            const cityId = city.cityId.toLowerCase();
            const simulationKind = city.simulationKind.toLowerCase();
            const status = city.status.toLowerCase();

            return name.includes(query) ||
                cityId.includes(query) ||
                simulationKind.includes(query) ||
                status.includes(query);
        });
    }, [citiesQuery.data, search]);

    const orderedCities = useMemo(() => {
        return [...filteredCities].sort((left, right) => {
            const rankDelta = getCityRank(left) - getCityRank(right);
            if (rankDelta !== 0) {
                return rankDelta;
            }

            return left.name.localeCompare(right.name, undefined, {sensitivity: "base"});
        });
    }, [filteredCities]);

    const stats = useMemo(() => {
        const readyCount = citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "active").length;
        const archivedCount = citiesQuery.data.filter((city) => getCityStatusTone(city.status) === "archived").length;
        const provisioningCount = provisioningQuery.data.length;
        const draftCount = setupSessionsQuery.data.length;

        return {
            visible: orderedCities.length,
            ready: readyCount,
            provisioning: provisioningCount,
            archived: archivedCount,
            drafts: draftCount,
        };
    }, [citiesQuery.data, orderedCities.length, provisioningQuery.data, setupSessionsQuery.data]);

    function handleOpen(city: CityListItemView) {
        const tone = getCityStatusTone(city.status);
        navigate(
            tone === "provisioning" || tone === "failed"
                ? getClassicCityProvisioningPath(city.cityId)
                : getClassicCityDetailsPath(city.cityId),
        );
    }

    function handleOpenSetupSession(session: ClassicCitySetupSessionView) {
        navigate(getClassicCitySetupSessionPath(session.sessionId));
    }

    async function handleDeleteSetupSession(session: ClassicCitySetupSessionView) {
        const draftName = session.draft.name.trim() || "Untitled Classic City";
        const accepted = await confirm({
            title: `Delete draft "${draftName}"?`,
            description: "The saved setup session will be removed from the gateway and can no longer be resumed.",
            confirmText: "Delete draft",
            cancelText: "Keep draft",
            tone: "danger",
        });

        if (!accepted) {
            return;
        }

        setSetupSessionActionError(null);
        setDeletingSetupSessionId(session.sessionId);

        try {
            await deleteClassicCitySetupSession(session.sessionId);
            await setupSessionsQuery.refetch();
        } catch (error: unknown) {
            const message = error instanceof Error && error.message.trim().length > 0
                ? error.message
                : "Failed to delete setup draft.";
            setSetupSessionActionError(message);
        } finally {
            setDeletingSetupSessionId(null);
        }
    }

    return (
        <section className="cities-page">
            <header className="cities-page__header">
                <div>
                    <div className="cities-page__eyebrow">SimulationCore</div>
                    <h1 className="cities-page__title">Cities</h1>
                    <p className="cities-page__subtitle">
                        Operate the city registry, keep provisioning visible as a first-class handoff state, and launch
                        new worlds through the setup wizard instead of an inline sidebar form.
                    </p>
                </div>

                <div className="cities-page__header-actions">
                    {canCreateCity ? (
                        <Link className="cities-page__header-link cities-page__header-link--primary"
                              to={getClassicCitySetupPath()}>
                            Compose Classic City
                        </Link>
                    ) : null}

                    <Link className="cities-page__header-link" to={SIMULATIONCORE_SCENARIO_CATALOG_PATH}>
                        Scenario catalog
                    </Link>
                </div>
            </header>

            <div className="cities-metrics" aria-label="City registry summary">
                <article className="cities-metric-card">
                    <span className="cities-metric-card__label">Visible now</span>
                    <strong className="cities-metric-card__value">{stats.visible}</strong>
                    <span
                        className="cities-metric-card__hint">Matches the current search query and archive scope.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--active">
                    <span className="cities-metric-card__label">Ready</span>
                    <strong className="cities-metric-card__value">{stats.ready}</strong>
                    <span className="cities-metric-card__hint">Cities already handed off to live monitoring.</span>
                </article>

                <article className="cities-metric-card cities-metric-card--provisioning">
                    <span className="cities-metric-card__label">Provisioning</span>
                    <strong className="cities-metric-card__value">{stats.provisioning}</strong>
                    <span className="cities-metric-card__hint">
                        Includes in-flight launches and failed handoffs that still need attention.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--draft">
                    <span className="cities-metric-card__label">Drafts</span>
                    <strong className="cities-metric-card__value">{stats.drafts}</strong>
                    <span className="cities-metric-card__hint">
                        Saved setup sessions that can still be resumed from the wizard.
                    </span>
                </article>

                <article className="cities-metric-card cities-metric-card--archived">
                    <span className="cities-metric-card__label">Archived</span>
                    <strong className="cities-metric-card__value">{stats.archived}</strong>
                    <span className="cities-metric-card__hint">Inactive records retained for review or cleanup.</span>
                </article>
            </div>

            <div className="cities-card cities-card--registry">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Resume drafts</h2>
                        <p className="cities-card__subtitle">
                            Setup sessions live outside the city registry until launch succeeds. Reopen a saved draft
                            here to continue authoring from its last backend-saved step. Drafts auto-expire after one
                            hour of inactivity and can also be discarded explicitly.
                        </p>
                    </div>

                    <Button
                        type="button"
                        variant="default"
                        onClick={() => {
                            void setupSessionsQuery.refetch();
                        }}
                        disabled={setupSessionsQuery.isLoading}
                    >
                        {setupSessionsQuery.isLoading ? "Refreshing..." : "Refresh drafts"}
                    </Button>
                </div>

                {setupSessionsQuery.error ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Failed to load setup drafts</div>
                            <div>{setupSessionsQuery.error}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                void setupSessionsQuery.refetch();
                            }}
                        >
                            Retry
                        </Button>
                    </div>
                ) : null}

                {!setupSessionsQuery.error && setupSessionActionError ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Draft action failed</div>
                            <div>{setupSessionActionError}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                setSetupSessionActionError(null);
                            }}
                        >
                            Dismiss
                        </Button>
                    </div>
                ) : null}

                {!setupSessionsQuery.error && setupSessionsQuery.isLoading && setupSessionsQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Loading setup drafts</div>
                        <div className="cities-empty-state__text">
                            Fetching resumable Classic City setup sessions saved on the gateway.
                        </div>
                    </div>
                ) : null}

                {!setupSessionsQuery.error && !setupSessionsQuery.isLoading && filteredSetupSessions.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">No resumable drafts</div>
                        <div className="cities-empty-state__text">
                            Start a new Classic City and this card will keep the draft visible until it becomes a real
                            city or is discarded.
                        </div>
                    </div>
                ) : null}

                {!setupSessionsQuery.error && filteredSetupSessions.length > 0 ? (
                    <SetupSessionList
                        sessions={filteredSetupSessions}
                        deletingSessionId={deletingSetupSessionId}
                        onOpen={handleOpenSetupSession}
                        onDelete={handleDeleteSetupSession}
                    />
                ) : null}
            </div>

            <div className="cities-card cities-card--registry">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">Provisioning handoff queue</h2>
                        <p className="cities-card__subtitle">
                            New launches and failed bootstrap attempts stay here until they are either handed off to
                            monitoring or explicitly resolved.
                        </p>
                    </div>

                    <Button
                        type="button"
                        variant="default"
                        onClick={() => {
                            void provisioningQuery.refetch();
                        }}
                        disabled={provisioningQuery.isLoading}
                    >
                        {provisioningQuery.isLoading ? "Refreshing..." : "Refresh queue"}
                    </Button>
                </div>

                {provisioningQuery.error ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Failed to load provisioning queue</div>
                            <div>{provisioningQuery.error}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                void provisioningQuery.refetch();
                            }}
                        >
                            Retry
                        </Button>
                    </div>
                ) : null}

                {!provisioningQuery.error && provisioningQuery.isLoading && provisioningQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Loading provisioning queue</div>
                        <div className="cities-empty-state__text">
                            Fetching in-flight launches and failed handoffs that still need operator attention.
                        </div>
                    </div>
                ) : null}

                {!provisioningQuery.error && !provisioningQuery.isLoading && provisioningQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Provisioning queue is clear</div>
                        <div className="cities-empty-state__text">
                            There are no in-flight or failed launches right now. New scenario launches will appear here
                            until they are ready for live monitoring.
                        </div>
                    </div>
                ) : null}

                {!provisioningQuery.error && provisioningQuery.data.length > 0 ? (
                    <CityList cities={provisioningQuery.data} onOpen={handleOpen}/>
                ) : null}
            </div>

            <div className="cities-card cities-card--registry">
                <div className="cities-card__header">
                    <div>
                        <h2 className="cities-card__title">City registry</h2>
                        <p className="cities-card__subtitle">
                            Only ready cities and archived records live here. Provisioning hosts are surfaced in the
                            dedicated handoff queue instead of mixing unfinished launches into the main registry.
                        </p>
                    </div>
                </div>

                <CitiesToolbar
                    search={search}
                    includeArchived={includeArchived}
                    isRefreshing={citiesQuery.isLoading}
                    onSearchChange={setSearch}
                    onIncludeArchivedChange={setIncludeArchived}
                    onRefresh={() => {
                        void citiesQuery.refetch();
                    }}
                />

                {citiesQuery.error ? (
                    <div className="cities-error-banner" role="alert">
                        <div className="cities-error-banner__content">
                            <div className="cities-error-banner__title">Failed to load cities</div>
                            <div>{citiesQuery.error}</div>
                        </div>

                        <Button
                            type="button"
                            variant="primary"
                            onClick={() => {
                                void citiesQuery.refetch();
                            }}
                        >
                            Retry
                        </Button>
                    </div>
                ) : null}

                {!citiesQuery.error && citiesQuery.isLoading && citiesQuery.data.length === 0 ? (
                    <div className="cities-empty-state">
                        <div className="cities-empty-state__title">Loading city registry</div>
                        <div className="cities-empty-state__text">
                            Fetching current city records and lifecycle handoff states.
                        </div>
                    </div>
                ) : null}

                {!citiesQuery.error && !citiesQuery.isLoading ? (
                    <CityList cities={orderedCities} onOpen={handleOpen}/>
                ) : null}
            </div>
        </section>
    );
}
