import {Link} from "react-router-dom";
import {CLASSIC_CITY_LIST_PATH} from "@services/citycore/scenarios/registry";
import {
    formatCityShortId,
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/citycore/scenarios/classic-city/utils/presentation";

type Props = {
    title: string;
    cityId: string;
    simulationKind?: string;
    status?: string;
    archivedAtUtc?: string | null;
};

export function CityDetailsHeader({title, cityId, simulationKind, status, archivedAtUtc}: Props) {
    const statusTone = getCityStatusTone(status, archivedAtUtc);
    const statusLabel = formatCityStatusLabel(status, archivedAtUtc);

    return (
        <header className="city-hero">
            <div className="city-hero__content">
                <div className="cities-page__eyebrow">CityCore</div>

                <div className="city-hero__title-row">
                    <h1 className="city-hero__title">{title}</h1>
                    <span className={`cities-status-pill cities-status-pill--${statusTone}`}>
                        {statusLabel}
                    </span>
                </div>

                <div className="city-hero__meta-row">
                    <span className="city-hero__id-chip" title={cityId}>
                        ID {formatCityShortId(cityId, 10, 6)}
                    </span>
                    <span className="city-hero__id-chip">
                        {formatSimulationKindLabel(simulationKind)}
                    </span>
                    <span className="city-hero__caption">
                        {statusTone === "archived"
                            ? "Archived cities remain visible but all simulation mutations are locked."
                            : "Active city workspace with lifecycle controls and simulation management."}
                    </span>
                </div>
            </div>

            <Link className="city-link" to={CLASSIC_CITY_LIST_PATH}>
                Back to cities
            </Link>
        </header>
    );
}
