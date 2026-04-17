import type {PermissionScope} from "../hooks/useAdminPermissions";

export default function RoleList({
                                     scopes,
                                     activeScopeId,
                                     onSelect,
                                 }: {
    scopes: PermissionScope[];
    activeScopeId: string | null;
    onSelect: (id: string) => void;
}) {
    return (
        <div className="mx-admin-perm__roles">
            <div className="mx-admin-perm__sideTitle">Scope</div>
            <div className="mx-admin-perm__roleList">
                {scopes.map((scope) => (
                    <button
                        key={scope.id}
                        type="button"
                        className={`mx-admin-perm__roleBtn${
                            scope.id === activeScopeId ? " is-active" : ""
                        }${
                            scope.kind === "default-user-access" ? " is-baseline" : ""
                        }`}
                        onClick={() => onSelect(scope.id)}
                    >
                        <div className="mx-admin-perm__roleLine">
                            <div className="mx-admin-perm__roleNameWrap">
                                <span
                                    className={`mx-admin-perm__roleGlyph ${
                                        scope.kind === "default-user-access"
                                            ? "mx-admin-perm__roleGlyph--baseline"
                                            : ""
                                    }`}
                                    aria-hidden="true"
                                >
                                    {scope.kind === "default-user-access"
                                        ? "D"
                                        : scope.name.charAt(0).toUpperCase()}
                                </span>
                                <span className="mx-admin-perm__roleName">{scope.name}</span>
                            </div>
                            {!scope.editable ? (
                                <span className="mx-admin-perm__roleLock">Read-only</span>
                            ) : null}
                        </div>
                        <div className="mx-admin-perm__roleMeta">{scope.meta}</div>
                    </button>
                ))}
            </div>
        </div>
    );
}
