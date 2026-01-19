import Card from "@shared/ui/controls/Card/Card";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import type { CityClockResponseDto } from "@services/citycore/api/cityCoreTypes";
import { formatUtcIso } from "@services/citycore/utils/cityCoreFormatters";
import KeyValueRow from "@services/citycore/features/clock/components/KeyValueRow";

interface CityClockCardProps {
    clock: CityClockResponseDto | null;
    isLoading: boolean;
    error: string | null;
    lastUpdatedAt: Date | null;
}

const CityClockCard = ({
                           clock,
                           isLoading,
                           error,
                           lastUpdatedAt,
                       }: CityClockCardProps) => {
    return (
        <Card
            title="Simulation clock"
            subtitle={
                lastUpdatedAt
                    ? `Last update: ${lastUpdatedAt.toLocaleTimeString()}`
                    : "No updates yet"
            }
        >
            {isLoading && <LoadingIndicator label="Loading clock..." />}

            {!isLoading && error && <p className="error-text">{error}</p>}

            {!isLoading && !error && !clock && (
                <p className="card-sub">Clock data is not available yet.</p>
            )}

            {!isLoading && !error && clock && (
                <div className="citycore-kv-grid">
                    <KeyValueRow label="City ID" value={clock.cityId} />
                    <KeyValueRow label="Sim time (UTC)" value={formatUtcIso(clock.simTimeUtc)} />
                    <KeyValueRow label="Tick ID" value={clock.tickId.toLocaleString()} />
                    <KeyValueRow label="Speed" value={`${clock.speed}x`} />
                    <KeyValueRow label="State" value={clock.state} />
                </div>
            )}
        </Card>
    );
};

export default CityClockCard;
