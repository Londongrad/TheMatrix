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
                <input
                    className="cities-input cities-toolbar__search"
                    type="text"
                    value={search}
                    placeholder="Search by name, id or status..."
                    onChange={(e) => onSearchChange(e.target.value)}
                />
            </div>

            <div className="cities-toolbar__right">
                <label className="cities-toolbar__checkbox">
                    <input
                        type="checkbox"
                        checked={includeArchived}
                        onChange={(e) => onIncludeArchivedChange(e.target.checked)}
                    />
                    <span>Include archived</span>
                </label>

                <button
                    type="button"
                    className="matrix-button"
                    onClick={onRefresh}
                    disabled={isRefreshing}
                >
                    {isRefreshing ? "Refreshing..." : "Refresh"}
                </button>
            </div>
        </div>
    );
}
