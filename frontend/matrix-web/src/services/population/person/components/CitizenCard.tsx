// src/services/population/person/components/CitizenCard.tsx
import React from "react";
import type { PersonDto } from "@services/population/person/api/personTypes";
import IconButton from "@shared/ui/controls/IconButton/IconButton";
import "@services/population/person/styles/citizen-card.css";

interface CitizenCardProps {
  person: PersonDto;
  onEdit?: (id: string) => void;
}

const CitizenCard = ({ person, onEdit }: CitizenCardProps) => {
  const handleOpenEditor = (event: React.MouseEvent<HTMLButtonElement>) => {
    event.stopPropagation();
    onEdit?.(person.id);
  };

  const isDeceased = person.lifeStatus === "Deceased";

  return (
    <article
      className={`card citizen-card ${
        isDeceased ? "citizen-card--deceased" : ""
      }`}
    >
      <header className="citizen-card-header">
        <div>
          <h3 className="citizen-card-title">{person.fullName}</h3>

          <p className="card-sub citizen-card-sub">
            {person.sex}, {person.age} y.o. ({person.ageGroup})
            {isDeceased ? (
              <span className="citizen-card-sub-status citizen-card-sub-status--deceased">
                DECEASED {person.deathDate}
              </span>
            ) : (
              <span className="citizen-card-sub-status citizen-card-sub-status--alive">
                {person.lifeStatus}
              </span>
            )}
          </p>
        </div>

        {onEdit && (
          <IconButton
            size="sm"
            aria-label="Open citizen details"
            onClick={handleOpenEditor}
          >
            ⋯
          </IconButton>
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
        </p>
      </section>
    </article>
  );
};

export default CitizenCard;
