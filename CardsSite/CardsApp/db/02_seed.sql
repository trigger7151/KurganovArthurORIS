-- Файл в UTF-8: явно задаём кодировку, чтобы psql в консоли Windows (WIN1251/CP866)
-- не испортил русский текст и не пропустил вставки.
SET client_encoding = 'UTF8';

-- Начальные данные. Световые конусы здесь тестовые: замените их своими
-- через админку (/admin) или дополните этот скрипт.

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

-- Пароли: admin / Admin123, editor / Editor123 (PBKDF2-SHA256, 100 000 итераций).
INSERT INTO users (login, passwordhash, displayname, role) VALUES
('admin',  'pbkdf2-sha256$100000$bGlnaHRjb25lLWFkbWluMQ==$vXSmdQ3xFHZyCy5WNy4avAyiIebALHZgZGKNexFJFMg=', 'Администратор', 'admin'),
('editor', 'pbkdf2-sha256$100000$bGlnaHRjb25lLWVkaXRvcg==$GyY/PGczprzhHc7yif2vENuPjySyP+Zs78ykFh4ZhZs=', 'Редактор',      'editor');

INSERT INTO lightcones (name, rarity, pathid, effectname, imageurl, descriptionhtml) VALUES
('Финал пути, где гаснут звёзды', 5, (SELECT id FROM paths WHERE slug = 'destruction'), 'Всем приготовиться', NULL,
 '<p>Стол уже накрыт...</p><p><a href="https://honkai-star-rail.fandom.com/ru/wiki/Химеко" target="_blank" rel="noopener noreferrer">Кто-то</a> хочет сначала выпить чашку чая.</p><p><a href="https://honkai-star-rail.fandom.com/ru/wiki/Март_7" target="_blank" rel="noopener noreferrer">Кто-то</a> хочет сначала сфотографироваться.</p><p><br></p><p>— Эй, ешьте, пока не остыло! <a href="https://honkai-star-rail.fandom.com/ru/wiki/Пом-Пом" target="_blank" rel="noopener noreferrer">Пом</a>!</p>'),
('Песнь о безмолвном прибое', 5, (SELECT id FROM paths WHERE slug = 'harmony'), 'Тихая волна', NULL,
 '<p>Тестовое описание. Замените его в админке.</p>'),
('Тихий вечер на станции', 4, (SELECT id FROM paths WHERE slug = 'abundance'), 'Свет в окне', NULL,
 '<p>Тестовое описание. Замените его в админке.</p>'),
('Охотник за кометами', 4, (SELECT id FROM paths WHERE slug = 'hunt'), 'Хвост кометы', NULL,
 '<p>Тестовое описание. Замените его в админке.</p>'),
('Эхо далёкого берега', 3, (SELECT id FROM paths WHERE slug = 'erudition'), 'Отголосок', NULL,
 '<p>Тестовое описание. Замените его в админке.</p>'),
('Утренний патруль', 3, (SELECT id FROM paths WHERE slug = 'preservation'), 'На посту', NULL,
 '<p>Тестовое описание. Замените его в админке.</p>');

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
