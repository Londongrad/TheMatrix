//src/services/citycore/cities/components/CityList.tsx
import CityCard from "@services/citycore/cities/components/CityCard";
import type {CityListItemView} from "@services/citycore/cities/contracts/citiesContracts";

interface CityListProps {
    cities: CityListItemView[];
    onOpen: (cityId: string) => void;
}

const CityList = ({cities, onOpen}: CityListProps) => {
    if (cities.length === 0) {
        return <p className="card-sub">No cities found for current filters.</p>;
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
