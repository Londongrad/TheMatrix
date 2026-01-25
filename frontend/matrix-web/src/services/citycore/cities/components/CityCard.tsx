//src/services/citycore/cities/components/CityCard.tsx
import Button from "@shared/ui/controls/Button/Button";
import type {CityListItemView} from "@services/citycore/cities/contracts/citiesContracts";

interface CityCardProps {
    city: CityListItemView;
    onOpen: (cityId: string) => void;
}

const CityCard = ({city, onOpen}: CityCardProps) => {
    return (
        <article className="city-card">
            <div>
                <h3 className="city-card__name">{city.name}</h3>
                <p className="city-card__meta">{city.cityId}</p>
            </div>
            <div className="city-card__footer">
                <span className="city-card__status">{city.status}</span>
                <Button size="sm" variant="primary" onClick={() => onOpen(city.cityId)}>
                    Open
                </Button>
            </div>
        </article>
    );
};

export default CityCard;
