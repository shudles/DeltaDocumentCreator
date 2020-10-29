using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;

namespace DeltaDocumentCreator
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");



            DoTheThing<CombinedRecord>(args[0], args[1]);
        }

        public static void DoTheThing<T>(string currentDatasetFile, string newDatasetFile) where T : IDeltableAgainst<T>
        {
            Console.WriteLine("Loading ...");
            var currentDatasetLookup = ExtractRecords<T>(currentDatasetFile).ToDictionary(t => t.Key, t => t);
            Console.WriteLine("Done.");

            var newDatasetRecords =  ExtractRecords<T>(newDatasetFile);

            var adds = new List<T>();
            var updates = new List<T>();

            Console.WriteLine("Deltering ...");
            foreach (var newRecord in newDatasetRecords)
            {
                if (currentDatasetLookup.Remove(newRecord.Key, out var currentRecord))
                {
                    if (currentRecord.IsEquivalentTo(newRecord))
                    {
                        // great, nothing to do!
                    }
                    else
                    {
                        // update
                        updates.Add(newRecord);
                    }
                }
                else
                {
                    adds.Add(newRecord);
                }
            }
            Console.WriteLine("Done.");

            // anything left is a remove,
            // todo - could copy it out to a list to allowed for GB to collect the records to free up memory for the csv writting
            var removes = currentDatasetLookup.Keys;

            var psvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
            psvConfig.Delimiter = "|";
            psvConfig.BadDataFound = target =>
            {
                Console.WriteLine($"Bad Field in row {target.Row} at index: {target.CurrentIndex}:\n\t {target.Field}");
            };

            Console.WriteLine("Writting ...");
            // todo write all 3 async
            using var addsStreamWritter = new StreamWriter("adds.psv");
            using var addsWritter = new CsvWriter(addsStreamWritter, psvConfig);
            
            addsWritter.WriteHeader<T>();
            foreach (var add in adds)
            {
                addsWritter.WriteRecord(add);
                addsWritter.NextRecord();
            }
            addsWritter.Flush();

            using var updatesStreamWritter = new StreamWriter("updates.psv");
            using var updatesWritter = new CsvWriter(updatesStreamWritter, psvConfig);
            updatesWritter.WriteHeader<T>();

            foreach (var update in updates)
            {
                updatesWritter.WriteRecord(update);
                updatesWritter.NextRecord();
            }
            updatesWritter.Flush();

            File.WriteAllLines("removes.psv", removes);
            
            Console.WriteLine("Done.");
        }


        public static IEnumerable<T> ExtractRecords<T>(string fileName)
        {
            var psvConfig = new CsvConfiguration(CultureInfo.InvariantCulture);
            psvConfig.Delimiter = "|";
            psvConfig.BadDataFound = target =>
            {
                Console.WriteLine($"Bad Field in row {target.Row} at index: {target.CurrentIndex}:\n\t {target.Field}");
            };
            
            var streamReader = new StreamReader(fileName);
            
            var csvReader = new CsvReader(streamReader, psvConfig);
            
            var combinedRecords = csvReader.GetRecords<T>();
            
            return combinedRecords;
        }
        
    }

    public interface IDeltableAgainst<T>
    {
        [Ignore]
        string Key { get; }

        bool IsEquivalentTo(T other) => false;
    }
}
