import {useEffect, useState} from "react";
import {Link, useSearchParams} from "react-router-dom";
import {confirmAccountRecovery} from "@services/identity/api/self/auth/authApi";
import AuthShell from "@shared/ui/layouts/auth-shell/AuthShell";
import AuthCard from "@services/identity/self/auth/components/AuthCard";
import AuthLogo from "@services/identity/self/auth/components/AuthLogo";

type ConfirmStatus = "pending" | "success" | "error";

export const ConfirmAccountRecoveryPage = () => {
    const [searchParams] = useSearchParams();
    const [status, setStatus] = useState<ConfirmStatus>("pending");
    const [message, setMessage] = useState("Restoring your account...");

    useEffect(() => {
        const userId = searchParams.get("userId");
        const token = searchParams.get("token");

        if (!userId || !token) {
            setStatus("error");
            setMessage("This recovery link is incomplete or invalid.");
            return;
        }

        let cancelled = false;

        void (async () => {
            try {
                await confirmAccountRecovery({userId, token});
                if (cancelled) return;

                setStatus("success");
                setMessage("Your account was restored. You can sign in again.");
            } catch (error: any) {
                if (cancelled) return;

                setStatus("error");
                setMessage(error?.message || "Failed to restore account.");
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [searchParams]);

    return (
        <AuthShell>
            <AuthCard
                side={
                    <>
                        <AuthLogo/>
                        <h2 className="auth-heading">
                            Bring your <span>Matrix</span> account back online
                        </h2>
                        <p className="auth-text">
                            Recovery confirms that the operator still controls the mailbox tied to
                            the deleted account.
                        </p>
                    </>
                }
            >
                <h1 className="auth-title">Account recovery</h1>
                <p className="auth-subtitle">{message}</p>

                {status === "pending" && (
                    <button className="auth-button" type="button" disabled>
                        <span className="auth-spinner" aria-hidden="true"/>
                        <span>Restoring...</span>
                    </button>
                )}

                {status !== "pending" && (
                    <div className="auth-switch">
                        <Link to="/login">Back to login</Link>
                        {" · "}
                        <Link to="/recover-account">Request another recovery link</Link>
                    </div>
                )}
            </AuthCard>
        </AuthShell>
    );
}
