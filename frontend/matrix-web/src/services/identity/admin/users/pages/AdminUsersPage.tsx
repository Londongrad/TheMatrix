import {useState} from "react";
import Card from "@shared/ui/controls/Card/Card";
import Button from "@shared/ui/controls/Button/Button";
import Pagination from "@shared/ui/components/Pagination/Pagination";
import LoadingIndicator from "@shared/ui/components/LoadingIndicator/LoadingIndicator";
import {IconRefresh} from "@shared/ui/icons/icons";
import {useAdminUsers} from "../hooks/useAdminUsers";
import UserCard from "../components/UserCard";
import UserAccessModal from "../components/UserAccessModal";
import {RequirePermission} from "@shared/permissions/RequirePermission";
import {PermissionKeys} from "@shared/permissions/permissionKeys";
import "../styles/admin-users-page.css";

export default function AdminUsersPage() {
    const [selectedUserId, setSelectedUserId] = useState<string | null>(null);
    const {
        data,
        items,
        pageNumber,
        setPageNumber,
        isLoading,
        error,
        totalPages,
        refresh,
        toggleLock,
        restore,
    } = useAdminUsers();

    const activeCount = items.filter((user) => !user.isDeleted).length;
    const lockedCount = items.filter((user) => user.isLocked).length;
    const deletedCount = items.filter((user) => user.isDeleted).length;
    const emailPendingCount = items.filter((user) => !user.isEmailConfirmed).length;

    return (
        <div className="mx-admin-page">
            <Card
                title="Users"
                subtitle="Directory & access"
                right={
                    <div className="mx-admin-users__headerRight">
                        <RequirePermission
                            perm={PermissionKeys.IdentityUsersRead}
                            displayMode="disable"
                        >
                            <Button onClick={refresh} disabled={isLoading}>
                                <IconRefresh/> Refresh
                            </Button>
                        </RequirePermission>
                        <RequirePermission
                            perm={PermissionKeys.IdentityUsersRead}
                            displayMode="disable"
                        >
                            <Button variant="primary" type="button" disabled>
                                + Add user
                            </Button>
                        </RequirePermission>
                    </div>
                }
            >
                {error ? <div className="mx-admin-users__error">{error}</div> : null}

                {isLoading && items.length === 0 ? (
                    <div className="mx-admin-users__loading">
                        <LoadingIndicator label="Loading users"/>
                    </div>
                ) : null}

                <div className="mx-admin-users__overview">
                    <div className="mx-admin-users__overviewHero">
                        <div className="mx-admin-users__overviewEyebrow">
                            Identity directory
                        </div>
                        <div className="mx-admin-users__overviewTitle">
                            Access watch across live accounts
                        </div>
                        <div className="mx-admin-users__overviewText">
                            Review account posture, access drift and recovery actions
                            from one lane before drilling into per-user overrides.
                        </div>
                    </div>

                    <div className="mx-admin-users__overviewStats">
                        <div className="mx-admin-users__overviewStat">
                            <span className="mx-admin-users__overviewValue">
                                {data?.totalCount ?? items.length}
                            </span>
                            <span className="mx-admin-users__overviewLabel">
                                Total users
                            </span>
                        </div>
                        <div className="mx-admin-users__overviewStat">
                            <span className="mx-admin-users__overviewValue">
                                {activeCount}
                            </span>
                            <span className="mx-admin-users__overviewLabel">
                                Active on page
                            </span>
                        </div>
                        <div className="mx-admin-users__overviewStat">
                            <span className="mx-admin-users__overviewValue">
                                {lockedCount}
                            </span>
                            <span className="mx-admin-users__overviewLabel">
                                Locked
                            </span>
                        </div>
                        <div className="mx-admin-users__overviewStat">
                            <span className="mx-admin-users__overviewValue">
                                {emailPendingCount}
                            </span>
                            <span className="mx-admin-users__overviewLabel">
                                Email pending
                            </span>
                        </div>
                    </div>
                </div>

                <div className="mx-admin-users__deckMeta">
                    <span className="mx-admin-users__deckPill">
                        Page {pageNumber} / {totalPages}
                    </span>
                    <span className="mx-admin-users__deckPill">
                        {deletedCount} deleted on page
                    </span>
                    {isLoading && items.length > 0 ? (
                        <span className="mx-admin-users__deckPill mx-admin-users__deckPill--live">
                            Refreshing roster
                        </span>
                    ) : null}
                </div>

                <div className="mx-admin-users__grid" role="list">
                    {items.map((user) => (
                        <UserCard
                            key={user.id}
                            user={user}
                            onOpenAccess={setSelectedUserId}
                            onToggleLock={toggleLock}
                            onRestore={restore}
                            isLoading={isLoading}
                        />
                    ))}
                </div>

                {data ? (
                    <div className="mx-admin-users__pager">
                        <div className="mx-admin-users__muted">
                            Page <b>{pageNumber}</b> / {totalPages} - {data.totalCount} total
                        </div>
                        <Pagination
                            page={pageNumber}
                            totalPages={totalPages}
                            onChange={setPageNumber}
                            disabled={isLoading}
                        />
                    </div>
                ) : null}
            </Card>

            {selectedUserId ? (
                <UserAccessModal
                    userId={selectedUserId}
                    onClose={() => setSelectedUserId(null)}
                />
            ) : null}
        </div>
    );
}
