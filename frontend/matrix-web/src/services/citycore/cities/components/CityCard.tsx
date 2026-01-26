import Button from "@shared/ui/controls/Button/Button";
import type {CityListItemView} from "@services/citycore/cities/contracts/citiesContracts";
import {
    formatCityShortId,
    formatCityStatusLabel,
    getCityStatusTone,
} from "@services/citycore/cities/utils/presentation";

interface CityCardProps {
    city: CityListItemView;
    onOpen: (cityId: string) => void;
}

const CityCard = ({city, onOpen}: CityCardProps) => {
    const statusTone = getCityStatusTone(city.status);
    const statusLabel = formatCityStatusLabel(city.status);

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
                    {statusTone === "archived"
                        ? "Read-only record. Simulation controls stay locked while the city remains archived."
                        : "Live city workspace with active simulation controls and timeline management."}
                </p>
            </div>

            <div className="city-card__footer">
                <div className="city-card__footer-copy">
                    <div className="city-card__footer-label">Registry state</div>
                    <div className="city-card__footer-value">{statusLabel}</div>
                </div>

                <Button
                    size="sm"
                    variant={statusTone === "archived" ? "default" : "primary"}
                    onClick={() => onOpen(city.cityId)}
                >
                    Open city
                </Button>
            </div>
        </article>
    );
};

export default CityCard;