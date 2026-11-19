SET client_encoding = 'UTF8';


INSERT INTO paths (name, slug, description) VALUES
('Разрушение', 'destruction', ''),
('Охота',      'hunt',        ''),
('Эрудиция',   'erudition',   ''),
('Гармония',   'harmony',     ''),
('Небытие',    'nihility',    ''),
('Сохранение', 'preservation',''),
('Изобилие',   'abundance',   ''),
('Память',     'remembrance', ''),
('Радость',    'elation',     '');

INSERT INTO users (login, passwordhash, displayname, role) VALUES
('admin',  'pbkdf2-sha256$100000$bGlnaHRjb25lLWFkbWluMQ==$vXSmdQ3xFHZyCy5WNy4avAyiIebALHZgZGKNexFJFMg=', 'Администратор', 'admin'),
('editor', 'pbkdf2-sha256$100000$bGlnaHRjb25lLWVkaXRvcg==$GyY/PGczprzhHc7yif2vENuPjySyP+Zs78ykFh4ZhZs=', 'Редактор',      'editor');


-- Характеристики на Ур. 1 и Ур. 80 (тестовые значения).
INSERT INTO lightconestats (lightconeid, level, hp, atk, def)
SELECT c.id, v.level, v.hp, v.atk, v.def
FROM lightcones c
JOIN (VALUES
    (5, 1, 48, 26, 21), (5, 80, 1058, 582, 463),
    (4, 1, 43, 21, 15), (4, 80, 952, 476, 330),
    (3, 1, 38, 14, 12), (3, 80, 846, 317, 264)
) AS v(rarity, level, hp, atk, def) ON v.rarity = c.rarity;

-- Тексты эффектов S1–S5: значения растут с уровнем наложения.
INSERT INTO superimpositions (lightconeid, level, text)
SELECT c.id, s.level,
       format('Наносимый владельцем урон повышается на %s%%. За каждого побеждённого противника сила атаки владельца повышается на %s%%. Этот эффект может складываться до 3 раз.',
              base.dmg + (s.level - 1) * base.step, 8 + (s.level - 1) * 2)
FROM lightcones c
JOIN (VALUES (5, 24, 4), (4, 16, 4), (3, 12, 3)) AS base(rarity, dmg, step) ON base.rarity = c.rarity
CROSS JOIN generate_series(1, 5) AS s(level);
