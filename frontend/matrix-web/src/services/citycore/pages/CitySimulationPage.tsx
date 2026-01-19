import { useState } from "react";
import { useAuth } from "@services/identity/api/self/auth/AuthContext";
import CityClockCard from "@services/citycore/features/clock/components/CityClockCard";
import SimulationControls from "@services/citycore/features/clock/components/SimulationControls";
import SpeedControl from "@services/citycore/features/clock/components/SpeedControl";
import JumpControl from "@services/citycore/features/clock/components/JumpControl";
import { useCityClockQuery } from "@services/citycore/hooks/useCityClockQuery";
import { useCityClockMutations } from "@services/citycore/hooks/useCityClockMutations";
import Button from "@shared/ui/controls/Button/Button";
import "@services/citycore/features/clock/styles/city-simulation.css";

const CitySimulationPage = () => {
    const { token } = useAuth();

    const [cityId, setCityId] = useState("");
    const [autoRefreshEnabled, setAutoRefreshEnabled] = useState(true);

    const canControlClock = Boolean(token) && cityId.length > 0;

    const clockQuery = useCityClockQuery(cityId, token, {
        enabled: cityId.length > 0,
        refetchIntervalMs: autoRefreshEnabled ? 1500 : 0,
    });

    const mutations = useCityClockMutations(cityId, token, {
        onSuccess: clockQuery.refetch,
        onBootstrapSuccess: (newCityId) => {
            setCityId(newCityId);
        },
    });

    const hasClock = Boolean(clockQuery.data);

    return (
        <div className="citycore-page">
            <div className="citycore-page__header">
                <h1 className="page-title">City Simulation</h1>
                <p className="card-sub">Bootstrap creates a new city and immediately starts simulation time.</p>
            </div>

            <section className="citycore-toolbar">
                <div className="citycore-toolbar__field">
                    <span className="card-sub">Current City ID</span>
                    <div className="citycore-city-id-badge">{cityId || "Not bootstrapped yet"}</div>
                </div>

                <label className="citycore-toolbar__switch" htmlFor="autorefresh-input">
                    <input
                        id="autorefresh-input"
                        type="checkbox"
                        checked={autoRefreshEnabled}
                        onChange={(e) => setAutoRefreshEnabled(e.target.checked)}
                    />
                    Auto-refresh
                </label>

                <Button onClick={() => void clockQuery.refetch()} disabled={!canControlClock || clockQuery.isLoading}>
                    Refresh now
                </Button>
            </section>

            {!cityId && <p className="card-sub">Click Bootstrap to create a city and start the simulation clock.</p>}

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
                    canBootstrap={Boolean(token)}
                    canControlClock={canControlClock}
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
