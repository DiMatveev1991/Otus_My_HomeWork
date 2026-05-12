using System;
using System.Net;
using System.Net.Http;

namespace TelegramBot
{
	/// <summary>
	/// Создаёт HttpClient для TelegramBotClient с учётом переменной окружения TELEGRAM_BOT_PROXY.
	///
	/// Поддерживаемые форматы:
	///   http://host:port
	///   http://user:pass@host:port
	///   https://host:port
	///   socks5://host:port
	///   socks5://user:pass@host:port
	///
	/// Если переменная не задана или пуста — возвращает null (TelegramBotClient использует HttpClient по умолчанию).
	///
	/// SOCKS5 поддерживается встроенными средствами .NET 6+ через WebProxy:
	/// https://learn.microsoft.com/dotnet/fundamentals/networking/sockets/socks-proxy
	/// </summary>
	public static class ProxyFactory
	{
		public static HttpClient? CreateHttpClient(out string? safeDescription)
		{
			safeDescription = null;
			var raw = Environment.GetEnvironmentVariable("TELEGRAM_BOT_PROXY");
			if (string.IsNullOrWhiteSpace(raw))
				return null;

			if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
				throw new ArgumentException(
					"Неверный формат TELEGRAM_BOT_PROXY. " +
					"Ожидается схема://host:port (http, https или socks5).");

			var scheme = uri.Scheme.ToLowerInvariant();
			if (scheme is not ("http" or "https" or "socks5"))
				throw new ArgumentException(
					$"Неподдерживаемая схема прокси \"{scheme}\". Доступны: http, https, socks5.");

			// WebProxy сам понимает схему socks5://… на .NET 6+,
			// но Credentials передаются отдельно от Uri (в Uri они игнорируются).
			var proxyUri = new UriBuilder(uri) { UserName = "", Password = "" }.Uri;
			var proxy = new WebProxy(proxyUri);

			if (!string.IsNullOrEmpty(uri.UserInfo))
			{
				var userInfo = uri.UserInfo.Split(':', 2);
				var user = Uri.UnescapeDataString(userInfo[0]);
				var pass = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
				proxy.Credentials = new NetworkCredential(user, pass);
			}

			var handler = new HttpClientHandler
			{
				Proxy = proxy,
				UseProxy = true
			};

			// Безопасное описание без креденшелов — для лога
			safeDescription = $"{scheme}://{uri.Host}:{uri.Port}";
			return new HttpClient(handler, disposeHandler: true);
		}
	}
}
