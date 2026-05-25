import {Link} from "react-router";

export default function NotFoundPage() {
    return (
        <main aria-labelledby="not-found-title" className="mx-page mx-page--centered">
            <section className="mx-card mx-card--narrow">
                <p className="mx-eyebrow">404</p>
                <h1 id="not-found-title">Page not found</h1>
                <p>The page you are looking for does not exist or has been moved.</p>
                <Link to="/">Back to dashboard</Link>
            </section>
        </main>
    );
}
