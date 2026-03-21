import Button from "@shared/ui/controls/Button/Button";
import type {CityListItemView} from "@services/simulationcore/scenarios/classic-city/contracts/citiesContracts";
import {
    describeCityLifecycle,
    formatCityShortId,
    formatCityStatusLabel,
    formatSimulationKindLabel,
    getCityStatusTone,
} from "@services/simulationcore/scenarios/classic-city/utils/presentation";

interface CityCardProps {
    city: CityListItemView;
    onOpen: (city: CityListItemView) => void;
}

const CityCard = ({city, onOpen}: CityCardProps) => {
    const statusTone = getCityStatusTone(city.status);
    const statusLabel = formatCityStatusLabel(city.status);
    const actionLabel = statusTone === "provisioning"
        ? "Open handoff"
        : statusTone === "failed"
            ? "Resolve handoff"
            : statusTone === "archived"
                ? "Review record"
                : "Open monitoring";

    return (
        <article className={`city-card city-card--${statusTone}`}>
            <div className="city-card__topline">
                <span className={`cities-status-pill cities-status-pill--${statusTone}`}>
                    {statusLabel}
                </span>
                <span className="city-card__id" title={city.cityId}>
                    {formatCityShortId(city.cityId)}
                </span>
            </div>

            <div className="city-card__body">
                <h3 className="city-card__name">{city.name}</h3>
                <p className="city-card__description">
                    {describeCityLifecycle(city.status)}
                </p>
                <p className="city-card__description">
                    Simulation type: {formatSimulationKindLabel(city.simulationKind)}
                </p>
            </div>

            <div className="city-card__footer">
                <div className="city-card__footer-copy">
                    <div className="city-card__footer-label">Registry state</div>
                    <div className="city-card__footer-value">{statusLabel}</div>
                </div>

                <Button
                    size="sm"
                    variant={statusTone === "archived" || statusTone === "unknown" ? "default" : "primary"}
                    onClick={() => onOpen(city)}
                >
                    {actionLabel}
                </Button>
            </div>
        </article>
    );
};

export default CityCard;
