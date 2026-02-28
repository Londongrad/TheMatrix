import {useEffect, useState} from "react";
import {Link, useSearchParams} from "react-router-dom";
import {confirmEmailChange} from "@services/identity/api/self/auth/authApi";
import {useAuth} from "@services/identity/api/self/auth/AuthContext";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

type ConfirmStatus = "pending" | "success" | "error";

export const ConfirmEmailChangePage = () => {
    const [searchParams] = useSearchParams();
    const {reloadMe} = useAuth();

    const [status, setStatus] = useState<ConfirmStatus>("pending");
    const [message, setMessage] = useState("Confirming your new email...");

    useEffect(() => {
        const userId = searchParams.get("userId");
        const token = searchParams.get("token");

        if (!userId || !token) {
            setStatus("error");
            setMessage("This email change link is incomplete or invalid.");
            return;
        }

        let cancelled = false;

        void (async () => {
            try {
                await confirmEmailChange({userId, token});
                if (cancelled) return;

                setStatus("success");
                setMessage("Your new email has been confirmed and is now active.");
                void reloadMe();
            } catch (err: any) {
                if (cancelled) return;

                setStatus("error");
                setMessage(err?.message || "Failed to confirm the new email.");
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [reloadMe, searchParams]);

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Confirm your next <span>Matrix</span> contact route
                        </h2>
                        <p className="auth-text">
                            Email replacement stays inactive until this confirmation step succeeds.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Email change confirmation</h1>
                <p className="auth-subtitle">{message}</p>

                {status === "pending" && (
                    <button className="auth-button" type="button" disabled>
                        <span className="auth-spinner" aria-hidden="true"/>
                        <span>Confirming...</span>
                    </button>
                )}

                {status !== "pending" && (
                    <div className="auth-switch">
                        <Link to="/login">Back to login</Link>
                        {" · "}
                        <Link to="/userSettings/security">Security settings</Link>
                    </div>
                )}
            </AuthCard>
        </AuthShell>
    );
};
