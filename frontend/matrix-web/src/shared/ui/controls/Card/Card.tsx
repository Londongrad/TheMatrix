import "./card.css";

type Props = {
  title: string;
  subtitle?: string;
  right?: React.ReactNode;
  children: React.ReactNode;
  className?: string;
};

export default function Card({
  title,
  subtitle,
  right,
  children,
  className = "",
}: Props) {
  return (
    <section className={`ui-card ${className}`.trim()}>
      <header className="ui-card__header">
        <div>
          <div className="ui-card__title">{title}</div>
          {subtitle ? <div className="ui-card__sub">{subtitle}</div> : null}
        </div>
        {right ? <div className="ui-card__right">{right}</div> : null}
      </header>
      <div className="ui-card__body">{children}</div>
    </section>
  );
}
