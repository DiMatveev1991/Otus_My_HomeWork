-- Применяется к базе ToDoList, созданной в предыдущих домашних работах.
-- Скрипт можно выполнять повторно.

BEGIN;

CREATE TABLE IF NOT EXISTS "Notification"
(
	"Id"          UUID        NOT NULL,
	"UserId"      UUID        NOT NULL,
	"Type"        TEXT        NOT NULL,
	"Text"        TEXT        NOT NULL,
	"ScheduledAt" TIMESTAMPTZ NOT NULL,
	"IsNotified"  BOOLEAN     NOT NULL DEFAULT FALSE,
	"NotifiedAt"  TIMESTAMPTZ NULL,
	CONSTRAINT "PK_Notification" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_Notification_ToDoUser_UserId"
		FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId"),
	CONSTRAINT "CK_Notification_NotifiedAt"
		CHECK (NOT "IsNotified" OR "NotifiedAt" IS NOT NULL)
);

CREATE INDEX IF NOT EXISTS "IX_Notification_UserId"
	ON "Notification" ("UserId");

CREATE UNIQUE INDEX IF NOT EXISTS "UX_Notification_UserId_Type"
	ON "Notification" ("UserId", "Type");

COMMIT;
