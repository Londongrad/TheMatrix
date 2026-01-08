import "./AdminCard.css";

export default function AdminCard({
  title,
  subtitle,
  right,
  children,
}: {
  title: string;
  subtitle?: string;
  right?: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <section className="mx-admin-card">
      <header className="mx-admin-card__header">
        <div>
          <div className="mx-admin-card__title">{title}</div>
          {subtitle ? (
            <div className="mx-admin-card__sub">{subtitle}</div>
          ) : null}
        </div>
        {right ? <div className="mx-admin-card__right">{right}</div> : null}
      </header>
      <div className="mx-admin-card__body">{children}</div>
    </section>
  );
}
