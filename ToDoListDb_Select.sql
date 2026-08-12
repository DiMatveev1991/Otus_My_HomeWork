-- Параметры в запросах записаны в формате Npgsql: @UserId, @Id, @Name.

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
