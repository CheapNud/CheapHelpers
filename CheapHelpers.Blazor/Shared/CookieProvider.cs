using Microsoft.AspNetCore.Components;
using System.Net;

namespace CheapHelpers.Blazor.Shared
{
	public class CookieProvider
	{
		public CookieProvider(NavigationManager nav)
		{
			_nav = nav;
		}

		private readonly NavigationManager _nav;
		public string Cookie { get; set; }

		public CookieContainer GetAuthenticationContainer()
		{
			var container = new CookieContainer();
			var cookie = new Cookie()
			{
				Name = ".AspNetCore.Identity.Application",
				Domain = _nav.ToAbsoluteUri(_nav.BaseUri).Host,
				Value = Cookie
			};

			container.Add(cookie);
			return container;
		}
	}
}
