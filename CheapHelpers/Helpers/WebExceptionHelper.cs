using System.Diagnostics;
using System.Net;

namespace CheapHelpers
{
	public static class WebExceptionHelper
	{
		/// <summary>
		/// Reads the statuscode of a webexception and produces a string to show on UI, pretty lame actually? remove? it is old...
		/// </summary>
		/// <param name="we"></param>
		/// <returns></returns>
		public static string ProcessWebException(WebException we)
		{
			if (we == null)
			{
				return "Onbekende Exceptie";
			}

			if (we.Message != null)
			{
				Debug.WriteLine(we.Message);
			}

			if (we.Response == null)
			{
				return "Server kon niet gevonden worden";
			}

			var status = ((HttpWebResponse)we.Response).StatusCode;

			if (status == HttpStatusCode.Unauthorized)
			{
				return "Foute gebruikersnaam/wachtwoord";
			}

			if (status == HttpStatusCode.NotFound)
			{
				return "Gegevens niet gevonden";
			}

			if (status == HttpStatusCode.BadRequest)
			{
				return "Parameterfout in webrequest";
			}

			if (status == HttpStatusCode.Conflict)
			{
				return "Conflict";
			}

			if (status == HttpStatusCode.InternalServerError)
			{
				return "Server Probleem";
			}

			return status + " Fout Server Antwoord";
		}
	}
}