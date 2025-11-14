import React, { useState } from "react";
import type { PersonDto } from "../../../api/population/types";

interface CitizenCardProps {
  person: PersonDto;

  // Экшены делаем опциональными — на дашборде можно их не передавать
  onKill?: (id: string) => void;
  onEdit?: (id: string) => void;
  onChangeHappiness?: (id: string, delta: number) => void;

  // compact-режим для дашборда (без нижних кнопок)
  compact?: boolean;
}

const CitizenCard: React.FC<CitizenCardProps> = ({
  person,
  onKill,
  onEdit,
  onChangeHappiness,
  compact = false,
}) => {
  const [menuOpen, setMenuOpen] = useState(false);

  const toggleMenu = (e: React.MouseEvent<HTMLButtonElement>) => {
    e.stopPropagation();
    setMenuOpen((prev) => !prev);
  };

  const handleKill = () => onKill?.(person.id);
  const handleEdit = () => onEdit?.(person.id);

  return (
    <article
      className={`card citizen-card ${compact ? "citizen-card-compact" : ""}`}
    >
      <header className="citizen-card-header">
        <div>
          <h3 className="card-title">{person.fullName}</h3>
          <p className="card-sub">
            {person.sex}, {person.age} y.o. ({person.ageGroup})
          </p>
        </div>

        {(onKill || onEdit || onChangeHappiness) && (
          <div className="citizen-card-menu-wrapper">
            <button className="icon-btn" onClick={toggleMenu}>
              ⋯
            </button>

            {menuOpen && (
              <div className="citizen-card-menu">
                {onKill && (
                  <button className="menu-item danger" onClick={handleKill}>
                    Kill citizen
                  </button>
                )}
                {onEdit && (
                  <button className="menu-item" onClick={handleEdit}>
                    Open full editor
                  </button>
                )}
                {onChangeHappiness && (
                  <>
                    <button
                      className="menu-item"
                      onClick={() => onChangeHappiness(person.id, +10)}
                    >
                      +10 happiness
                    </button>
                    <button
                      className="menu-item"
                      onClick={() => onChangeHappiness(person.id, -10)}
                    >
                      -10 happiness
                    </button>
                  </>
                )}
              </div>
            )}
          </div>
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
      </section>

      {(onKill || onEdit) && !compact && (
        <footer className="citizen-card-footer">
          {onKill && (
            <button className="btn btn-danger btn-sm" onClick={handleKill}>
              Kill
            </button>
          )}
          {onEdit && (
            <button className="btn btn-secondary btn-sm" onClick={handleEdit}>
              Edit
            </button>
          )}
        </footer>
      )}
    </article>
  );
};

export default CitizenCard;
