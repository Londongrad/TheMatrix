import type {ReactNode} from "react";
import "@services/identity/self/account/shared/styles/user-settings-page.css";
import "@services/identity/self/account/shared/styles/user-settings-shared.css";

type Props = {
    title: string;
    subtitle: string;
    children: ReactNode;
    layout?: "single" | "grid";
    showHeader?: boolean;
};

export default function UserSettingsSection({
                                                title,
                                                subtitle,
                                                children,
                                                layout = "single",
                                                showHeader = false,
                                            }: Props) {
    return (
        <div className="user-settings-page">
            {showHeader ? (
                <div className="user-settings-header">
                    <div>
                        <h1 className="user-settings-title">{title}</h1>
                        <p className="user-settings-subtitle">{subtitle}</p>
                    </div>
                </div>
            ) : null}

            <div
                className={`user-settings-grid${
                    layout === "single" ? " user-settings-grid--single" : ""
                }`}
            >
                {children}
            </div>
        </div>
    );
}
