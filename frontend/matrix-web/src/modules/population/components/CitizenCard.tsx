import React from "react";
import type { PersonDto } from "../../../api/population/types";

interface CitizenCardProps {
  person: PersonDto;
  /** Открыть полный редактор / модалку */
  onEdit?: (id: string) => void;
  /** Compact пока оставим на будущее, если захочешь другой стиль */
  compact?: boolean;
}

const CitizenCard: React.FC<CitizenCardProps> = ({
  person,
  onEdit,
  compact = false,
}) => {
  const handleOpenEditor = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.stopPropagation();
    onEdit?.(person.id);
  };

  const isDeceased =
    person.lifeStatus.toLowerCase() === "deceased" ||
    (person.deathDate && person.deathDate.trim().length > 0);

  return (
    <article
      className={`card citizen-card ${compact ? "citizen-card-compact" : ""} ${
        isDeceased ? "citizen-card--deceased" : ""
      }`}
    >
      <header className="citizen-card-header">
        <div>
          <h3 className="card-title">{person.fullName}</h3>

          <p className="card-sub citizen-card-sub">
            {person.sex}, {person.age} y.o. ({person.ageGroup})
            {isDeceased && (
              <span className="citizen-card-sub-status citizen-card-sub-status--deceased">
                DECEASED
              </span>
            )}
          </p>
        </div>

        {onEdit && (
          <button className="icon-btn" onClick={handleOpenEditor}>
            ⋯
          </button>
        )}
      </header>

      <section className="citizen-card-body">
        <p>
          <strong>Marital:</strong> {person.maritalStatus}
        </p>
        <p>
          <strong>Education:</strong> {person.educationLevel}
        </p>
        <p>
          <strong>Employment:</strong> {person.employmentStatus}
          {person.jobTitle ? ` (${person.jobTitle})` : ""}
        </p>
        <p>
          <strong>Happiness:</strong> {person.happiness}
        </p>
        <p>
          <strong>Birth date:</strong> {person.birthDate}
          {isDeceased && person.deathDate && <> – {person.deathDate}</>}
        </p>
      </section>
    </article>
  );
};

export default CitizenCard;
