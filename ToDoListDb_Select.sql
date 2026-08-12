-- Параметры в запросах записаны в формате Npgsql:
-- @UserId, @Id, @Name, @From, @To, @ScheduledBefore.

-- IToDoRepository.GetAllByUserIdAsync(Guid userId, CancellationToken ct)
SELECT
	"Id",
	"UserId",
	"Name",
	"CreatedAt",
	"Deadline",
	"ListId",
	"State",
	"StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId
ORDER BY "CreatedAt", "Id";

-- IToDoRepository.GetActiveByUserIdAsync(Guid userId, CancellationToken ct)
-- ToDoItemState.Active = 0.
SELECT
	"Id",
	"UserId",
	"Name",
	"CreatedAt",
	"Deadline",
	"ListId",
	"State",
	"StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId
	AND "State" = 0
ORDER BY "CreatedAt", "Id";

-- IToDoRepository.GetAsync(Guid id, CancellationToken ct)
SELECT
	"Id",
	"UserId",
	"Name",
	"CreatedAt",
	"Deadline",
	"ListId",
	"State",
	"StateChangedAt"
FROM "ToDoItem"
WHERE "Id" = @Id;

-- IToDoRepository.ExistsByNameAsync(Guid userId, string name, CancellationToken ct)
SELECT EXISTS
(
	SELECT 1
	FROM "ToDoItem"
	WHERE "UserId" = @UserId
		AND LOWER("Name") = LOWER(@Name)
);

-- IToDoRepository.CountActiveAsync(Guid userId, CancellationToken ct)
SELECT COUNT(*)::INTEGER
FROM "ToDoItem"
WHERE "UserId" = @UserId
	AND "State" = 0;

-- IToDoRepository.FindAsync(Guid userId, Func<ToDoItem, bool> predicate, CancellationToken ct)
-- Произвольный Func<ToDoItem, bool> нельзя преобразовать в универсальный SQL-запрос.
-- Запрос получает задачи пользователя, после чего predicate применяется к ним в C#.
SELECT
	"Id",
	"UserId",
	"Name",
	"CreatedAt",
	"Deadline",
	"ListId",
	"State",
	"StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId
ORDER BY "CreatedAt", "Id";

-- IToDoRepository.GetActiveWithDeadline(Guid userId, DateTime from, DateTime to, CancellationToken ct)
SELECT
	"Id",
	"UserId",
	"Name",
	"CreatedAt",
	"Deadline",
	"ListId",
	"State",
	"StateChangedAt"
FROM "ToDoItem"
WHERE "UserId" = @UserId
	AND "State" = 0
	AND "Deadline" >= @From
	AND "Deadline" < @To
ORDER BY "Deadline", "Id";

-- IUserRepository.GetUsers(CancellationToken ct)
SELECT
	"UserId",
	"TelegramUserId",
	"TelegramUserName",
	"RegisteredAt"
FROM "ToDoUser"
ORDER BY "RegisteredAt", "UserId";

-- INotificationService.GetScheduledNotification(DateTime scheduledBefore, CancellationToken ct)
SELECT
	"Id",
	"UserId",
	"Type",
	"Text",
	"ScheduledAt",
	"IsNotified",
	"NotifiedAt"
FROM "Notification"
WHERE NOT "IsNotified"
	AND "ScheduledAt" <= @ScheduledBefore
ORDER BY "ScheduledAt", "Id";
