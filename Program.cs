using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using DataLoaderUtils.IO;
using DataLoadUtils.Delta;
using DataLoadUtils.IO;
using Newtonsoft.Json;

namespace DeltaDocumentCreator
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            using var psvReader = new PsvReader();
            var baseline = psvReader.ExtractRecords<FileSystem, CombinedRecord>(args[0], onError: e => Console.WriteLine(e));
            var current =  psvReader.ExtractRecords<FileSystem, CombinedRecord>(args[1], onError: e => Console.WriteLine(e));

            var deltaActions = DeltaCalculator.CalculateDeltaActions<CombinedRecord>(baseline, current);

            var createsTask = PsvWriting.WriteAsync<FileSystem, CombinedRecord>("creates.psv", deltaActions.RecordsToCreate);
            var replaceTask = PsvWriting.WriteAsync<FileSystem, CombinedRecord>("replaces.psv", deltaActions.RecordsToReplace);
            var removesTask = PsvWriting.WriteAsync<FileSystem, CombinedRecord>("removes.psv", deltaActions.RecordsToRemove);

            await Task.WhenAll(createsTask, replaceTask, removesTask);

            await File.WriteAllTextAsync("index.html", GenerateHtmlReport<CombinedRecord>(deltaActions));
            await File.WriteAllTextAsync("summary.geojson", GenerateGeoJson(deltaActions));
        }

        public static string GenerateHtmlReport<T>(IDeltaActions<T> deltaActions) where T : IDeltaRecord<T>
        {
            const string css = "<style>table {  font-family: arial, sans-serif;  border-collapse: collapse;  width: 100%;}td, th {  border: 1px solid #dddddd;  text-align: left;  padding: 8px;}tr:nth-child(even) {  background-color: #dddddd;}</style>";
            return $"<html>{css}<body><table><tr><th>Action</th><th>Count</th></tr><tr><td>New Addresses</td><th>{deltaActions.RecordsToCreate.Count()}</td></tr><tr><td>Retired Addresses</td><th>{deltaActions.RecordsToRemove.Count()}</td></tr><tr><td>Updated Addresses</td><th>{deltaActions.RecordsToReplace.Count()}</td></tr></table></body></html>";
        }

        public static string GenerateGeoJson(IDeltaActions<CombinedRecord> deltaActions)
        {
            return JsonConvert.SerializeObject(GeoJsonFeatureCollection.FromCombinedRecords(deltaActions));
        }
    }

    public class GeoJsonFeatureCollection
    {
        [JsonProperty("type")]
        public string Type => "FeatureCollection";

        [JsonProperty("features")]
        public List<GeoJsonFeature> Featrues { get; set; }

        public static GeoJsonFeatureCollection FromCombinedRecords(IDeltaActions<CombinedRecord> actions)
        {
            return new GeoJsonFeatureCollection
            {
                Featrues = actions.RecordsToCreate.Select(
                        r => GeoJsonFeature.FromCombinedRecord(r, "#3cc62a", "New")
                    ).Union(actions.RecordsToRemove.Select(
                        r => GeoJsonFeature.FromCombinedRecord(r, "#c62a2a", "Retired"))
                    ).Union(actions.RecordsToReplace.Select(
                        r => GeoJsonFeature.FromCombinedRecord(r, "#2a75c6", "Updated"))
                        ).ToList()
            };
        }
    }

    public class GeoJsonFeature
    {
        [JsonProperty("type")]
        public string Type => "Feature";


        [JsonProperty("properties")]
        public Dictionary<string, string> Properties { get; set; }

        [JsonProperty("geometry")]
        public GeoJsonPoint Geometry { get; set; }

        public static GeoJsonFeature FromCombinedRecord(CombinedRecord record, string colour, string action)
        {
            return new GeoJsonFeature
            {
                Properties = new Dictionary<string, string> {
                    {"marker-color", colour},
                    {"marker-size", "medium"},
                    {"address", record.DisplayAddress},
                    {"action", action}
                },
                Geometry = GeoJsonPoint.FromCombinedRecord(record)
            };
        }
    }

    public class GeoJsonPoint
    {
        [JsonProperty("type")]
        public string Type => "Point";

        [JsonProperty("coordinates")]
        public List<double> Coordinates { get; set; }

        public static GeoJsonPoint FromCombinedRecord(CombinedRecord record)
        {
            var lon = 0;
            var lat = 0;
            double.TryParse(record.Longitude, out lon);
            double.TryParse(record.Latitude, out lat);
            return new GeoJsonPoint
            {
                Coordinates = new List<double> { lon, lat },

            };
        }
    }
}
