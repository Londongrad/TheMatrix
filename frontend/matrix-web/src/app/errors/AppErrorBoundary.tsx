import {Component, type ErrorInfo, type PropsWithChildren, type ReactNode} from "react";

type AppErrorBoundaryProps = PropsWithChildren<{
    resetKey?: string;
}>;

type AppErrorBoundaryState = {
    error: Error | null;
};

export class AppErrorBoundary extends Component<AppErrorBoundaryProps, AppErrorBoundaryState> {
    public state: AppErrorBoundaryState = {
        error: null,
    };

    public static getDerivedStateFromError(error: Error): AppErrorBoundaryState {
        return {
            error,
        };
    }

    public componentDidCatch(error: Error, errorInfo: ErrorInfo): void {
        console.error("Application route render failed.", error, errorInfo);
    }

    public componentDidUpdate(previousProps: AppErrorBoundaryProps): void {
        if (previousProps.resetKey !== this.props.resetKey && this.state.error !== null) {
            this.setState({
                error: null,
            });
        }
    }

    public render(): ReactNode {
        if (this.state.error !== null) {
            return (
                <main aria-labelledby="app-error-title" className="mx-page mx-page--centered">
                    <section className="mx-card mx-card--narrow">
                        <p className="mx-eyebrow">Application error</p>
                        <h1 id="app-error-title">Something went wrong</h1>
                        <p>
                            The page could not be rendered. Try refreshing the page or navigating
                            back to a previous section.
                        </p>
                    </section>
                </main>
            );
        }

        return this.props.children;
    }
}
