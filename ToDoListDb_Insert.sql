BEGIN;

INSERT INTO "ToDoUser"
	("UserId", "TelegramUserId", "TelegramUserName", "RegisteredAt")
VALUES
	('11111111-1111-1111-1111-111111111111', 100000001, 'dmitry_test',
	 TIMESTAMPTZ '2026-08-01 09:00:00+00'),
	('22222222-2222-2222-2222-222222222222', 100000002, 'alex_test',
	 TIMESTAMPTZ '2026-08-01 10:00:00+00');

INSERT INTO "ToDoList"
	("Id", "UserId", "Name", "CreatedAt")
VALUES
	('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
	 '11111111-1111-1111-1111-111111111111',
	 'Запчасти для ТО',
	 TIMESTAMPTZ '2026-08-02 09:00:00+00'),
	('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
	 '22222222-2222-2222-2222-222222222222',
	 'Ремонт автомобиля',
	 TIMESTAMPTZ '2026-08-02 10:00:00+00');

INSERT INTO "ToDoItem"
	("Id", "UserId", "Name", "CreatedAt", "Deadline", "ListId", "State", "StateChangedAt")
VALUES
	('00000000-0000-0000-0000-000000000001',
	 '11111111-1111-1111-1111-111111111111',
	 'Купить масляный фильтр',
	 TIMESTAMPTZ '2026-08-03 09:00:00+00',
	 TIMESTAMPTZ '2026-08-20 18:00:00+00',
	 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
	 0,
	 NULL),
	('00000000-0000-0000-0000-000000000002',
	 '11111111-1111-1111-1111-111111111111',
	 'Проверить уровень масла',
	 TIMESTAMPTZ '2026-08-03 10:00:00+00',
	 TIMESTAMPTZ '2026-08-10 18:00:00+00',
	 NULL,
	 1,
	 TIMESTAMPTZ '2026-08-09 12:30:00+00'),
	('00000000-0000-0000-0000-000000000003',
	 '22222222-2222-2222-2222-222222222222',
	 'Заказать тормозные колодки',
	 TIMESTAMPTZ '2026-08-04 09:00:00+00',
	 TIMESTAMPTZ '2026-08-25 18:00:00+00',
	 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
	 0,
	 NULL),
	('00000000-0000-0000-0000-000000000004',
	 '22222222-2222-2222-2222-222222222222',
	 'Заменить лампу фары',
	 TIMESTAMPTZ '2026-08-04 10:00:00+00',
	 TIMESTAMPTZ '2026-08-12 18:00:00+00',
	 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
	 1,
	 TIMESTAMPTZ '2026-08-11 16:00:00+00');

COMMIT;
