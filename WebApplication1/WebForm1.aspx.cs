using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text;
using System.Text.RegularExpressions;
using Npgsql;

namespace WebApplication1
{
	public partial class WebForm1 : System.Web.UI.Page
	{
		private static NpgsqlConnection conn;
		private static String address;
		protected void Page_Load(object sender, EventArgs e)
		{
			address = "Server=127.0.0.1;Port=5432;User Id=postgres;Password=Cyagcataw;Database=geo_db;";

			conn = new NpgsqlConnection(address);
		}

		[System.Web.Services.WebMethod]
		public static Object GetGeoData(String searchCat, String numParameter, String busStopSearch, String fieldsStr, String lat, String lng)
		{
			StringBuilder JSON = new StringBuilder();
			JSON.Append('[');
			try
			{
				List<String> fieldsList = new List<String>(fieldsStr.Split(','));
				int searchCategory = Int32.Parse(searchCat);

				if (searchCategory == 0) {
					//-------------------FIRST PART----------------
					String query1 = getQuery_01(fieldsList, Int32.Parse(numParameter));
					String query2 = getQuery_02(fieldsList, Int32.Parse(numParameter));
					NpgsqlCommand cmd;
					NpgsqlDataReader dr;

					conn.Open();
					cmd = new NpgsqlCommand(query1, conn);
					dr = cmd.ExecuteReader();

					while (dr.Read())
						JSON.Append(addJSONTag(dr.GetString(0), dr.GetString(1), dr.GetString(2)) + ',');

					dr.Close();
					conn.Close();
					//-------------------SECOND PART----------------
					conn.Open();
					cmd = new NpgsqlCommand(query2, conn);
					dr = cmd.ExecuteReader();

					while (dr.Read())
						JSON.Append(addJSONTag("bus", dr.GetString(0), dr.GetString(1)) + ',');

					dr.Close();
					conn.Close();

					if (JSON.Length > 1) {
						JSON.Remove(JSON.Length - 1, 1);
						JSON.Append(']');
					}
					else {
						JSON.Append(']');
					}
				}
				else {
					int radius = Int32.Parse(numParameter);
					bool noBusSearch = Boolean.Parse(busStopSearch);

					if (noBusSearch) {
						String query = getQuery_05(fieldsList, radius, lng, lat);
						NpgsqlCommand cmd;
						NpgsqlDataReader dr;

						conn.Open();
						cmd = new NpgsqlCommand(query, conn);
						dr = cmd.ExecuteReader();

						while (dr.Read())
							JSON.Append(addJSONTag(dr.GetString(0), dr.GetString(1), dr.GetString(2)) + ',');

						dr.Close();
						conn.Close();
					}
					else {
						String query1 = getQuery_03(fieldsList, radius, lng, lat);
						String query2 = getQuery_04(fieldsList, radius, lng, lat);
						NpgsqlCommand cmd;
						NpgsqlDataReader dr;

						conn.Open();
						cmd = new NpgsqlCommand(query1, conn);
						dr = cmd.ExecuteReader();

						while (dr.Read())
							JSON.Append(addJSONTag(dr.GetString(0), dr.GetString(1), dr.GetString(2)) + ',');

						dr.Close();
						conn.Close();

						conn.Open();
						cmd = new NpgsqlCommand(query2, conn);
						dr = cmd.ExecuteReader();

						while (dr.Read())
							JSON.Append(addJSONTag("bus", dr.GetString(0), dr.GetString(1)) + ',');

						dr.Close();
						conn.Close();
					}

					if (JSON.Length > 1)
					{
						JSON.Remove(JSON.Length - 1, 1);
						JSON.Append(']');
					}
					else
					{
						JSON.Append(']');
					}
				}
				return JSON.ToString();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
				Console.WriteLine(ex.StackTrace);
			}
			finally
			{
				conn.Close();
			}
			return "[]";
		}
	
		public static String addJSONTag(String type, String name, String geoJSON)
		{
			String retVal = "{" +
						"\"type\": \"Feature\", " +
						"\"geometry\": " + geoJSON + "," +
						getJSONTagProperties(type, name) +
						"}";
			return retVal;
		}
		public static String getJSONTagProperties(String type, String name)
		{
			String title = @"{""title"":""" + (String.IsNullOrEmpty(name) ? "Unknown name" : name) + "\"";
			String description = @",""description"":""" + (String.IsNullOrEmpty(type) ? "Unknown type" : type) + "\"";

			String marker_size = @",""marker-size"": ""medium""";
			String marker_symbol = "", marker_color = "";

			if (String.Compare(type, "electronics") == 0) {
				marker_symbol = @",""marker-symbol"": ""lighthouse""";
				marker_color = @",""marker-color"": ""#b49082""";
			}
			else if (String.Compare(type, "doityourself") == 0) {
				marker_symbol = @",""marker-symbol"": ""logging""";
				marker_color = @",""marker-color"": ""#bfc0c0""";
			}
			else if (String.Compare(type, "computer") == 0)	{
				marker_symbol = @",""marker-symbol"": ""heliport""";
				marker_color = @",""marker-color"": ""#c5d86d""";
			}
			else if (String.Compare(type, "hardware") == 0)	{
				marker_symbol = @",""marker-symbol"": ""dam""";
				marker_color = @",""marker-color"": ""#2e294e""";
			}
			else if (String.Compare(type, "bus") == 0) {
				marker_size = @",""marker-size"": ""large""";
				marker_symbol = @",""marker-symbol"": ""bus""";
				marker_color = @",""marker-color"": ""#e71d36""";
			}

			String retVal = "\"properties\":" +
						title + description +
						marker_color + marker_size + marker_symbol +
						"}";
			return retVal;
		}

		private static String getQuery_01(List<String> fieldsList, int distance)
		{
			String retVal = getSubQuery_01() +
						"," + getSubQuery_02(fieldsList) +
						"SELECT DISTINCT SS.shop_type, SS.shop_name, SS.shop_coords FROM (" +
						"SELECT BS.name AS stop_name, ST_ASGEOJSON(ST_TRANSFORM(BS.coords, 4326)) AS stop_coords," +
						"S.shop AS shop_type, S.name AS shop_name, ST_ASGEOJSON(ST_TRANSFORM(S.coords, 4326)) AS shop_coords " +
						"FROM bus_stops BS CROSS JOIN shops S " +
						"WHERE BS.RowID = 1 " +
						"AND ST_DWITHIN(ST_TRANSFORM(BS.coords, 4326)::geography, ST_TRANSFORM(S.coords, 4326)::geography, " + distance + ")" +
						") AS SS";
			return retVal;
		}
		private static String getQuery_02(List<String> fieldsList, int distance)
		{
			String retVal = getSubQuery_01() +
						"," + getSubQuery_02(fieldsList) +
						"SELECT DISTINCT SS.stop_name, SS.stop_coords FROM (" +
						"SELECT BS.name AS stop_name, ST_ASGEOJSON(ST_TRANSFORM(BS.coords, 4326)) AS stop_coords, " +
						"S.shop AS shop_type, S.name AS shop_name, ST_ASGEOJSON(ST_TRANSFORM(S.coords, 4326)) AS shop_coords " +
						"FROM bus_stops BS CROSS JOIN shops S " +
						"WHERE BS.RowID = 1 " +
						"AND ST_DWITHIN(ST_TRANSFORM(BS.coords, 4326)::geography, ST_TRANSFORM(S.coords, 4326)::geography, " + distance + ")" +
						") AS SS";
			return retVal;
		}
		private static String getQuery_03(List<String> fieldsList, int radius, String lng, String lat)
		{
			String retVal = getSubQuery_01() +
						", " + getSubQuery_02(fieldsList) +
						", " + getSubQuery_03(lng, lat) +
						"SELECT DISTINCT SS.shop_type, SS.shop_name, SS.shop_coords FROM ( " +
						"SELECT BS.name AS stop_name, ST_ASGEOJSON(ST_TRANSFORM(BS.coords, 4326)) AS stop_coords, " +
						"S.shop AS shop_type, S.name AS shop_name, ST_ASGEOJSON(ST_TRANSFORM(S.coords, 4326)) AS shop_coords " +
						"FROM bus_stops BS CROSS JOIN shops S CROSS JOIN marker_coord MC " +
						"WHERE RowID = 1 " +
						"AND ST_DWITHIN(MC.coords::geography, ST_TRANSFORM(BS.coords, 4326)::geography, " + radius + ") " +
						"AND ST_DWITHIN(MC.coords::geography, ST_TRANSFORM(S.coords, 4326)::geography, " + radius + ") " +
						") AS SS";
			return retVal;
		}
		private static String getQuery_04(List<String> fieldsList, int radius, String lng, String lat)
		{
			String retVal = getSubQuery_01() +
						", " + getSubQuery_02(fieldsList) +
						", " + getSubQuery_03(lng, lat) +
						"SELECT DISTINCT SS.stop_name, SS.stop_coords FROM ( " +
						"SELECT BS.name AS stop_name, ST_ASGEOJSON(ST_TRANSFORM(BS.coords, 4326)) AS stop_coords, " +
						"S.shop AS shop_type, S.name AS shop_name, ST_ASGEOJSON(ST_TRANSFORM(S.coords, 4326)) AS shop_coords " +
						"FROM bus_stops BS CROSS JOIN shops S CROSS JOIN marker_coord MC " +
						"WHERE RowID = 1 " +
						"AND ST_DWITHIN(MC.coords::geography, ST_TRANSFORM(BS.coords, 4326)::geography, " + radius + ") " +
						"AND ST_DWITHIN(MC.coords::geography, ST_TRANSFORM(S.coords, 4326)::geography, " + radius + ") " +
						") AS SS";
			return retVal;
		}
		private static String getQuery_05(List<String> fieldsList, int radius, String lng, String lat)
		{
			String retVal = "WITH " + getSubQuery_02(fieldsList) +
						"," + getSubQuery_03(lng, lat) +
						"SELECT S.shop AS shop_type, S.name AS shop_name, ST_ASGEOJSON(ST_TRANSFORM(S.coords, 4326)) AS shop_coords " +
						"FROM shops S CROSS JOIN marker_coord MC " +
						"WHERE ST_DWITHIN(MC.coords::geography, ST_TRANSFORM(S.coords, 4326)::geography, " + radius + ")";
			return retVal;
		}

		private static String getSubQuery_01()
		{
			String retVal =
				"WITH bus_stops AS (" +
				"SELECT DISTINCT name, highway, way AS coords, ROW_NUMBER() OVER (PARTITION BY name ORDER BY name) AS RowID " +
				"FROM planet_osm_point " +
				"WHERE highway LIKE '%bus%'" +
				")";
			return retVal;
		}
		private static String getSubQuery_02(List<String> categories)
		{
			String retVal =
					"shops AS (" +
					"SELECT shop, name, way AS coords " +
					"FROM planet_osm_point " +
					"WHERE shop IS NOT NULL AND name IS NOT NULL " +
					getSubQuery_04(categories) +
					")";
			return retVal;
		}
		private static String getSubQuery_03(String lng, String lat)
		{
			String retVal = "marker_coord AS (" +
				"SELECT ST_MAKEPOINT(" + lng + "," + lat + ") AS coords" +
				")";
			return retVal;
		}
		private static String getSubQuery_04(List<String> categories)
		{
			StringBuilder sb = new StringBuilder();

			sb.Append("AND (");
			for(int i = 0; i < categories.Count; i++) {
				sb.Append("shop LIKE '%" + categories.ElementAt(i) + "%'");

				if (i < categories.Count - 1)
					sb.Append(" OR ");
			}
			sb.Append(')');

			return sb.ToString();
		}
	}
}

/*
if (String.Compare(type, "seafood") == 0) {
				marker_symbol = @",""marker-symbol"": ""restaurant""";
				marker_color = @",""marker-color"": ""#fc4353""";
			}
			else if (String.Compare(type, "doityourself") == 0) {
				marker_symbol = @",""marker-symbol"": ""logging""";
				marker_color = @",""marker-color"": ""#bfc0c0""";
			}
			else if (String.Compare(type, "clothes") == 0)	{
				marker_symbol = @",""marker-symbol"": ""clothing-store""";
				marker_color = @",""marker-color"": ""#4f5d75""";
			}
			else if (String.Compare(type, "bus") == 0) {
				marker_size = @",""marker-size"": ""large""";
				marker_symbol = @",""marker-symbol"": ""bus""";
				marker_color = @",""marker-color"": ""#e71d36""";
			}
*/