-- База данных создаётся один раз до выполнения этого файла:
-- CREATE DATABASE "ToDoList";
-- После создания подключитесь к базе "ToDoList" и выполните скрипт целиком.

BEGIN;

CREATE TABLE "ToDoUser"
(
	"UserId"           UUID        NOT NULL,
	"TelegramUserId"   BIGINT      NOT NULL,
	"TelegramUserName" TEXT        NOT NULL,
	"RegisteredAt"     TIMESTAMPTZ NOT NULL,
	CONSTRAINT "PK_ToDoUser" PRIMARY KEY ("UserId")
);

CREATE TABLE "ToDoList"
(
	"Id"        UUID        NOT NULL,
	"UserId"    UUID        NOT NULL,
	"Name"      TEXT        NOT NULL,
	"CreatedAt" TIMESTAMPTZ NOT NULL,
	CONSTRAINT "PK_ToDoList" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_ToDoList_ToDoUser_UserId"
		FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId")
);

CREATE TABLE "ToDoItem"
(
	"Id"             UUID        NOT NULL,
	"UserId"         UUID        NOT NULL,
	"Name"           TEXT        NOT NULL,
	"CreatedAt"      TIMESTAMPTZ NOT NULL,
	"Deadline"       TIMESTAMPTZ NOT NULL,
	"ListId"         UUID        NULL,
	"State"          INTEGER     NOT NULL,
	"StateChangedAt" TIMESTAMPTZ NULL,
	CONSTRAINT "PK_ToDoItem" PRIMARY KEY ("Id"),
	CONSTRAINT "FK_ToDoItem_ToDoUser_UserId"
		FOREIGN KEY ("UserId") REFERENCES "ToDoUser" ("UserId"),
	CONSTRAINT "FK_ToDoItem_ToDoList_ListId"
		FOREIGN KEY ("ListId") REFERENCES "ToDoList" ("Id"),
	CONSTRAINT "CK_ToDoItem_State" CHECK ("State" IN (0, 1))
);

CREATE INDEX "IX_ToDoList_UserId"
	ON "ToDoList" ("UserId");

CREATE INDEX "IX_ToDoItem_UserId"
	ON "ToDoItem" ("UserId");

CREATE INDEX "IX_ToDoItem_ListId"
	ON "ToDoItem" ("ListId");

CREATE UNIQUE INDEX "UX_ToDoUser_TelegramUserId"
	ON "ToDoUser" ("TelegramUserId");

COMMIT;
