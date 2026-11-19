-- Файл в UTF-8: явно задаём кодировку, чтобы psql в консоли Windows (WIN1251/CP866)
-- не испортил русский текст и не пропустил вставки.
SET client_encoding = 'UTF8';

-- Схема базы данных Light Cone Wiki (PostgreSQL).
-- Имена таблиц и столбцов без кавычек (в нижнем регистре): ORM сопоставляет
-- свойства моделей со столбцами без учёта регистра (ImageUrl -> imageurl).

DROP TABLE IF EXISTS superimpositions;
DROP TABLE IF EXISTS lightconestats;
DROP TABLE IF EXISTS lightcones;
DROP TABLE IF EXISTS paths;
DROP TABLE IF EXISTS users;

CREATE TABLE paths
(
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(40)  NOT NULL UNIQUE,
    slug        VARCHAR(20)  NOT NULL UNIQUE CHECK (slug ~ '^[a-z]{3,20}$'),
    description TEXT         NOT NULL DEFAULT ''
);

CREATE TABLE lightcones
(
    id              SERIAL PRIMARY KEY,
    name            VARCHAR(80)  NOT NULL UNIQUE,
    rarity          INTEGER      NOT NULL CHECK (rarity BETWEEN 3 AND 5),
    pathid          INTEGER      NOT NULL REFERENCES paths (id) ON DELETE RESTRICT,
    effectname      VARCHAR(60)  NOT NULL,
    imageurl        VARCHAR(300) NULL,
    descriptionhtml TEXT         NOT NULL DEFAULT '',
    createdat       TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE TABLE lightconestats
(
    id          SERIAL PRIMARY KEY,
    lightconeid INTEGER NOT NULL REFERENCES lightcones (id) ON DELETE CASCADE,
    level       INTEGER NOT NULL CHECK (level BETWEEN 1 AND 80),
    hp          INTEGER NOT NULL CHECK (hp >= 0),
    atk         INTEGER NOT NULL CHECK (atk >= 0),
    def         INTEGER NOT NULL CHECK (def >= 0),
    UNIQUE (lightconeid, level)
);

CREATE TABLE superimpositions
(
    id          SERIAL PRIMARY KEY,
    lightconeid INTEGER NOT NULL REFERENCES lightcones (id) ON DELETE CASCADE,
    level       INTEGER NOT NULL CHECK (level BETWEEN 1 AND 5),
    text        TEXT    NOT NULL,
    UNIQUE (lightconeid, level)
);

CREATE TABLE users
(
    id           SERIAL PRIMARY KEY,
    login        VARCHAR(20)  NOT NULL UNIQUE,
    passwordhash VARCHAR(200) NOT NULL,
    displayname  VARCHAR(60)  NOT NULL,
    role         VARCHAR(10)  NOT NULL CHECK (role IN ('admin', 'editor'))
);

CREATE INDEX ix_lightcones_pathid ON lightcones (pathid);
CREATE INDEX ix_lightconestats_cone ON lightconestats (lightconeid);
CREATE INDEX ix_superimpositions_cone ON superimpositions (lightconeid);
