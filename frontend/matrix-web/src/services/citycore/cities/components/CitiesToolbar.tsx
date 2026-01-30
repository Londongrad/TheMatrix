import Button from "@shared/ui/controls/Button/Button";

type Props = {
    search: string;
    includeArchived: boolean;
    isRefreshing: boolean;
    onSearchChange: (value: string) => void;
    onIncludeArchivedChange: (value: boolean) => void;
    onRefresh: () => void;
};

export function CitiesToolbar({
                                  search,
                                  includeArchived,
                                  isRefreshing,
                                  onSearchChange,
                                  onIncludeArchivedChange,
                                  onRefresh,
                              }: Props) {
    return (
        <div className="cities-toolbar">
            <div className="cities-toolbar__left">
                <label className="cities-toolbar__search-wrap" htmlFor="citycore-city-search">
                    <span className="cities-toolbar__label">Search registry</span>
                    <input
                        id="citycore-city-search"
                        className="cities-input cities-toolbar__search"
                        type="text"
                        value={search}
                        placeholder="Search by city name, id, or lifecycle..."
                        onChange={(event) => onSearchChange(event.target.value)}
                    />
                </label>
            </div>

            <div className="cities-toolbar__right">
                <label className="cities-filter-toggle">
                    <input
                        className="cities-filter-toggle__input"
                        type="checkbox"
                        checked={includeArchived}
                        onChange={(event) => onIncludeArchivedChange(event.target.checked)}
                    />
                    <span className="cities-filter-toggle__indicator" aria-hidden="true"/>
                    <span className="cities-filter-toggle__copy">
                        <span className="cities-filter-toggle__title">Show archived cities</span>
                        <span className="cities-filter-toggle__text">
                            Include inactive records in the registry view.
                        </span>
                    </span>
                </label>

                <Button
                    type="button"
                    variant="primary"
                    onClick={onRefresh}
                    disabled={isRefreshing}
                >
                    {isRefreshing ? "Refreshing..." : "Refresh registry"}
                </Button>
            </div>
        </div>
    );
}
