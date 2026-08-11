using System;
using System.Collections.Generic;
using System.Linq;

namespace Helpers
{
	public static class EnumerableExtension
	{
		/// <summary>
		/// Возвращает пачку с номером <paramref name="batchNumber"/>.
		/// Нумерация пачек начинается с нуля.
		/// </summary>
		public static IEnumerable<TSource> GetBatchByNumber<TSource>(
			this IEnumerable<TSource> source,
			int batchSize,
			int batchNumber)
		{
			ArgumentNullException.ThrowIfNull(source);

			if (batchSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(batchSize),
					"Размер пачки должен быть больше нуля.");

			if (batchNumber < 0)
				throw new ArgumentOutOfRangeException(nameof(batchNumber),
					"Номер пачки не может быть отрицательным.");

			var itemsToSkip = checked(batchSize * batchNumber);
			return source.Skip(itemsToSkip).Take(batchSize);
		}
	}
}
