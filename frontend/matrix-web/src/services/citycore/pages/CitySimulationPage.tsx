import { useMemo, useState } from "react";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import CityClockCard from "@services/citycore/features/clock/components/CityClockCard";
import SimulationControls from "@services/citycore/features/clock/components/SimulationControls";
import SpeedControl from "@services/citycore/features/clock/components/SpeedControl";
import JumpControl from "@services/citycore/features/clock/components/JumpControl";
import { useCityClockQuery } from "@services/citycore/hooks/useCityClockQuery";
import { useCityClockMutations } from "@services/citycore/hooks/useCityClockMutations";
import { generateGuid, isEmptyGuid, isGuid } from "@services/citycore/utils/cityCoreFormatters";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/features/clock/styles/city-simulation.css";

const DEFAULT_CITY_ID = "";

const CitySimulationPage = () => {
    const { token } = useAuth();

    const [cityIdInput, setCityIdInput] = useState(DEFAULT_CITY_ID);
    const [autoRefreshEnabled, setAutoRefreshEnabled] = useState(true);

    const cityId = useMemo(() => cityIdInput.trim(), [cityIdInput]);
    const isCityIdEmpty = isEmptyGuid(cityId);
    const isCityIdValid = cityId.length > 0 && isGuid(cityId) && !isCityIdEmpty;
    const canRunActions = Boolean(token) && isCityIdValid;

    const clockQuery = useCityClockQuery(cityId, token, {
        enabled: isCityIdValid,
        refetchIntervalMs: autoRefreshEnabled ? 1500 : 0,
    });

    const mutations = useCityClockMutations(cityId, token, {
        onSuccess: clockQuery.refetch,
    });

    const hasClock = Boolean(clockQuery.data);

    return (
        <div className="citycore-page">
            <div className="citycore-page__header">
                <h1 className="page-title">City Simulation</h1>
                <p className="card-sub">Manage CityCore simulation time through Gateway BFF.</p>
            </div>

            <section className="citycore-toolbar">
                <label className="citycore-toolbar__field" htmlFor="cityIdInput">
                    <span className="card-sub">City ID</span>
                    <div className="citycore-city-id-input-wrap">
                        <input
                            id="cityIdInput"
                            className="text-input citycore-city-id-input"
                            placeholder="e.g. 4de4f8c5-9efe-45a3-bec4-e57f4bf4f95b"
                            value={cityIdInput}
                            onChange={(e) => setCityIdInput(e.target.value)}
                        />
                        {!cityId && (
                            <button
                                type="button"
                                className="citycore-generate-guid-btn"
                                onClick={() => setCityIdInput(generateGuid())}
                                aria-label="Generate random City GUID"
                            >
                                Generate GUID
                            </button>
                        )}
                    </div>
                </label>

                <label className="citycore-toolbar__switch" htmlFor="autorefresh-input">
                    <input
                        id="autorefresh-input"
                        type="checkbox"
                        checked={autoRefreshEnabled}
                        onChange={(e) => setAutoRefreshEnabled(e.target.checked)}
                    />
                    Auto-refresh
                </label>

                <Button onClick={() => void clockQuery.refetch()} disabled={!isCityIdValid || clockQuery.isLoading}>
                    Refresh now
                </Button>
            </section>

            {cityId.length > 0 && !isCityIdValid && (
                <p className="error-text">
                    {isCityIdEmpty
                        ? "City ID cannot be 00000000-0000-0000-0000-000000000000 (Guid.Empty)."
                        : "City ID must be a valid GUID."}
                </p>
            )}

            {cityId.length === 0 && (
                <p className="card-sub">Enter a City ID to enable simulation actions.</p>
            )}

            {mutations.actionError && <p className="error-text">{mutations.actionError}</p>}

            <div className="citycore-grid">
                <CityClockCard
                    clock={clockQuery.data}
                    isLoading={clockQuery.isLoading}
                    error={clockQuery.error}
                    lastUpdatedAt={clockQuery.lastUpdatedAt}
                />

                <SimulationControls
                    state={clockQuery.data?.state ?? null}
                    hasClock={hasClock}
                    canRunActions={canRunActions}
                    isBootstrapping={mutations.isBootstrapping}
                    isPausing={mutations.isPausing}
                    isResuming={mutations.isResuming}
                    onBootstrap={mutations.bootstrap}
                    onPause={mutations.pauseClock}
                    onResume={mutations.resumeClock}
                />

                <SpeedControl
                    disabled={!hasClock}
                    isSubmitting={mutations.isSettingSpeed}
                    onSubmit={mutations.setClockSpeed}
                />

                <JumpControl
                    disabled={!hasClock}
                    isSubmitting={mutations.isJumping}
                    onSubmit={mutations.jumpClock}
                />
            </div>
        </div>
    );
};

export default CitySimulationPage;
