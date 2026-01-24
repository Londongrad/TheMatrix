import {Link} from "react-router-dom";

type Props = {
    title: string;
    cityId: string;
};

export function CityDetailsHeader({title, cityId}: Props) {
    return (
        <div className="city-details-headline">
            <div>
                <div className="cities-page__eyebrow">CityCore</div>
                <h1 className="page-title">{title}</h1>
                <div className="city-details-subtitle">{cityId}</div>
            </div>

            <Link className="city-link" to="/cities">
                ← Back to cities
            </Link>
        </div>
    );
}
