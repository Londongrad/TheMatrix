import CityCard from "@services/citycore/scenarios/classic-city/components/CityCard";
import type {CityListItemView} from "@services/citycore/scenarios/classic-city/contracts/citiesContracts";

interface CityListProps {
    cities: CityListItemView[];
    onOpen: (cityId: string) => void;
}

const CityList = ({cities, onOpen}: CityListProps) => {
    if (cities.length === 0) {
        return (
            <div className="cities-empty-state" role="status">
                <div className="cities-empty-state__title">No cities match the current filters</div>
                <div className="cities-empty-state__text">
                    Try clearing the search query or include archived cities to widen the registry.
                </div>
            </div>
        );
    }

    return (
        <div className="city-list-grid">
            {cities.map((city) => (
                <CityCard key={city.cityId} city={city} onOpen={onOpen}/>
            ))}
        </div>
    );
};

export default CityList;