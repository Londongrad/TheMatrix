import {Link} from "react-router";
import {CLASSIC_CITY_LIST_PATH} from "@services/simulationcore/scenarios/registry";
import {
    formatCityStatusLabel,
    getCityStatusTone,
} from "@services/simulationcore/scenarios/classic-city/utils/presentation";

type Props = {
    title: string;
    cityId?: string;
    simulationKind?: string;
    status?: string;
    archivedAtUtc?: string | null;
    links?: ReadonlyArray<{
        to: string;
        label: string;
    }>;
};

export function CityDetailsHeader({title, status, archivedAtUtc, links}: Props) {
    const statusTone = getCityStatusTone(status, archivedAtUtc);
    const statusLabel = formatCityStatusLabel(status, archivedAtUtc);
    const headerLinks = links?.length
        ? links
        : [{to: CLASSIC_CITY_LIST_PATH, label: "Back to cities"}];

    return (
        <header className="city-hero city-hero--compact">
            <div className="city-hero__content">
                <div className="city-hero__title-row">
                    <h1 className="city-hero__title">{title}</h1>
                    <span className={`cities-status-pill cities-status-pill--${statusTone}`}>
                        {statusLabel}
                    </span>
                </div>
            </div>

            <div className="city-hero__actions">
                {headerLinks.map((link) => (
                    <Link key={`${link.to}:${link.label}`} className="city-link" to={link.to}>
                        {link.label}
                    </Link>
                ))}
            </div>
        </header>
    );
}
