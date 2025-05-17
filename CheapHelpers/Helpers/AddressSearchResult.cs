using Newtonsoft.Json;
using System.Collections.Generic;

namespace CheapHelpers
{
	public class Root
	{
		public Summary Summary { get; set; }
		public List<Result> Results { get; set; }
	}

	public class Address
	{
		public string StreetNumber { get; set; }
		public string StreetName { get; set; }
		public string Municipality { get; set; }
		public string CountrySecondarySubdivision { get; set; }
		public string CountrySubdivision { get; set; }
		public string PostalCode { get; set; }
		public string CountryCode { get; set; }
		public string Country { get; set; }
		public string CountryCodeISO3 { get; set; }
		public string FreeformAddress { get; set; }
		public string LocalName { get; set; }
		public string MunicipalitySubdivision { get; set; }
	}

	public class BoundingBox
	{
		public TopLeftPoint TopLeftPoint { get; set; }
		public BtmRightPoint BtmRightPoint { get; set; }
	}

	public class BtmRightPoint
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}

	public class CategorySet
	{
		public int Id { get; set; }
	}

	public class Classification
	{
		public string Code { get; set; }
		public List<Name> Names { get; set; }
	}

	public class DataSources
	{
		public Geometry Geometry { get; set; }
	}

	public class EntryPoint
	{
		public string Type { get; set; }
		public Position Position { get; set; }
	}

	public class Geometry
	{
		public string Id { get; set; }
	}

	public class Name
	{
		public string NameLocale { get; set; }

		[JsonProperty("name")]
		public string NameString { get; set; }
	}

	public class Poi
	{
		public string Name { get; set; }
		public List<CategorySet> CategorySet { get; set; }
		public string Url { get; set; }
		public List<string> Categories { get; set; }
		public List<Classification> Classifications { get; set; }
	}

	public class Position
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}

	public class Result
	{
		public string Type { get; set; }
		public string Id { get; set; }
		public double Score { get; set; }
		public string Info { get; set; }
		public Poi Poi { get; set; }
		public Address Address { get; set; }
		public Position Position { get; set; }
		public Viewport Viewport { get; set; }
		public List<EntryPoint> EntryPoints { get; set; }
		public DataSources DataSources { get; set; }
		public string EntityType { get; set; }
		public BoundingBox BoundingBox { get; set; }
	}

	public class Summary
	{
		public string Query { get; set; }
		public string QueryType { get; set; }
		public int QueryTime { get; set; }
		public int NumResults { get; set; }
		public int Offset { get; set; }
		public int TotalResults { get; set; }
		public int FuzzyLevel { get; set; }
		public List<object> QueryIntent { get; set; }
	}

	public class TopLeftPoint
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}

	public class Viewport
	{
		public TopLeftPoint TopLeftPoint { get; set; }
		public BtmRightPoint BtmRightPoint { get; set; }
	}
}
