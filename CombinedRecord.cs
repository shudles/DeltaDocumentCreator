using System.Collections.Generic;
using System.Text;
using CsvHelper.Configuration.Attributes;
using DataLoadUtils.Delta;
using static DeltaDocumentCreator.Extensions;

namespace DeltaDocumentCreator
{
    public class CombinedRecord : IDeltaRecord<CombinedRecord>
    {
        public CombinedRecord()
        {
            DataSources = new List<string>();
        }

        public CombinedRecord DeepCopyAttributes()
        {
            return new CombinedRecord
            {
                ComplexLevelType = ComplexLevelType,
                ComplexLevelTypeDescription = ComplexLevelTypeDescription,
                ComplexLevelNumber = ComplexLevelNumber,
                ComplexUnitType = ComplexUnitType,
                ComplexUnitTypeDescription = ComplexUnitTypeDescription,
                ComplexUnitIdentifier = ComplexUnitIdentifier,
                SiteName = SiteName,
                LotIdentifier = LotIdentifier,
                StreetNumber1 = StreetNumber1,
                StreetNumber2 = StreetNumber2,
                StreetName = StreetName,
                StreetType = StreetType,
                StreetTypeDescription = StreetTypeDescription,
                StreetSuffix = StreetSuffix,
                LocalityName = LocalityName,
                StateTerritory = StateTerritory,
                Postcode = Postcode
            };        
        }
        
        public static string Header => "display_address|address_identifier|merge_key|complex_unit_type|complex_unit_identifier|complex_level_type|complex_level_number|lot_identifier|street_number_1|number_first|street_number_2|number_last|street_name|street_type|street_suffix|locality_name|state_territory|postcode|postal_delivery_number|postal_delivery_type|latitude|longitude|cadastral_identifier|geo_feature|location_descriptor|site_name|complex_unit_type_description|complex_level_type_description|alias_principal|street_aliases|locality_aliases|locality_neighbours|primary_secondary|gnaf_street_locality_pid|gnaf_locality_pid|street_type_description|origin|data_sources";

        public override string ToString()
        {
            return $"{DisplayAddress}|{AddressIdentifier}|{ComplexUnitType}|{ComplexUnitIdentifier}|{ComplexLevelType}|{ComplexLevelNumber}|{LotIdentifier}|{StreetNumber1}|{NumberFirst}|{StreetNumber2}|{NumberLast}|{StreetName}|{StreetType}|{StreetSuffix}|{LocalityName}|{StateTerritory}|{Postcode}|{PostalDeliveryNumber}|{PostalDeliveryType}|{Latitude}|{Longitude}|{CadastralIdentifier}|{GeoFeature}|{LocationDescriptor}|{SiteName}|{ComplexUnitTypeDescription}|{ComplexLevelTypeDescription}|{AliasPrincipal}|{StreetAliases}|{LocalityAliases}|{LocalityNeighbours}|{PrimarySecondary}|{GnafStreetLocalityPid}|{GnafLocalityPid}|{StreetTypeDescription}|{Origin}|({string.Join(",", DataSources)})";
        }


        public string GenerateMergeKey()
        {
            var mergeKey = $"{StateTerritory}|{LocalityName}|{Postcode}|";
            if (PostalDeliveryType.HasValue())
            {
                if (PostalDeliveryNumber.HasValue())
                {
                    mergeKey += $"|||{PostalDeliveryType} {PostalDeliveryNumber}||||||";
                }
                else
                {
                    mergeKey += $"|||{PostalDeliveryType}||||||";
                }
            }
            else
            {
                if (StreetNumber1.HasValue())
                {
                    mergeKey += $"{StreetName}|{StreetType}|{StreetSuffix}|{StreetNumber1}|";
                }
                else
                {
                    mergeKey += $"{StreetName}|{StreetType}|{StreetSuffix}|LOT {LotIdentifier}|";
                }
                mergeKey += $"{StreetNumber2}|{SiteName}|{ComplexUnitType}|{ComplexUnitIdentifier}|{ComplexLevelType}|{ComplexLevelNumber}";
            }
            return mergeKey;
        }

        public string GenerateDisplayAddress()
        {
        // full_address_string = (postal_address / street_address) locality_part
            var displayAddressBuilder = new StringBuilder();

            if (PostalDeliveryType.HasValue()) {
                BuildPostalAddressPart(displayAddressBuilder);

            } else {
                BuildStreetAddress(displayAddressBuilder);
            }

            BuildLocalityPart(displayAddressBuilder);

            return displayAddressBuilder.ToString();
        }

        public string[] GenerateCompletionAddresses(string locality, List<string> addresses)
        {
            addresses.Add(GenerateCompletionAddress(locality, true));
            if (SiteName.HasValue())
                addresses.Add(GenerateCompletionAddress(locality, false));
            if (ComplexUnitType.HasValue())
            {
                var addressWithoutUnitType = DeepCopyAttributes();
                addressWithoutUnitType.ComplexUnitType = null;
                addresses.Add(addressWithoutUnitType.GenerateCompletionAddress(locality, true));
                if (SiteName.HasValue())
                    addresses.Add(addressWithoutUnitType.GenerateCompletionAddress(locality, false));
            }
            return addresses.ToArray();
        } 

        public string GenerateCompletionAddress(string locality, bool includeSiteName) 
        {
        // full_address_string = (postal_address / street_address) locality_part
            var displayAddressBuilder = new StringBuilder();

            if (PostalDeliveryType.HasValue()) {
                BuildPostalAddressPart(displayAddressBuilder);

            } else {
                BuildStreetAddress(displayAddressBuilder, includeSiteName);
            }

            BuildLocalityPart(displayAddressBuilder, locality);

            return displayAddressBuilder.ToString();
        }

        private void BuildPostalAddressPart(StringBuilder builder)
        {
        // postal_address = postal_type ( / SP postal_number_part)
        //     postal_number_part = ( / postal_number_prefix) postal_number ( / postal_number_suffix)
            builder.Append(PostalDeliveryType);

            if (PostalDeliveryNumber.HasValue()) {
                builder.Append(Sp);
                builder.Append(PostalDeliveryNumber);
            }
        }

        private void BuildStreetAddress(StringBuilder builder, bool includeSiteName = true)
        {
        // street_address = name_part number_part street_part
        //     name_part = (address_site_name SP / building_part)
        //         building_part = ( / building_name SP)
        //     street_part = street_name ( / SP street_type) ( / SP street_suffix)
            
            // name_part
            if (includeSiteName && SiteName.HasValue()) {
                builder.Append(SiteName);
                builder.Append(Sp);
                // need building_name from datasets
            }

            number_part(builder);

            // street_part
            builder.Append(StreetName);
            if (StreetType.HasValue()) {
                builder.Append(Sp);
                builder.Append(StreetType);
            }
            if (StreetSuffix.HasValue()) {
                builder.Append(Sp);
                builder.Append(StreetSuffix);
            }
        }

        private void number_part(StringBuilder builder)
        {
        // number_part = (unit_part level_part road_number_part / level_part sub_building_number road_number_part)
        //     unit_part = unit_type SP unit_number_part SP
        //         unit_number_part = ( / unit_prefix) ( / unit_number) ( / unit_suffix)
        //     level_part = ( / level_type SP ( / level_number_part SP))
        //         level_number_part = ( / level_prefix) ( / level_number) ( / level_suffix)
        //     sub_building_number = ( / unit_number_part "/")
        //     road_number_part = (number_first_part ("-" number_last_part) SP / lot_number_part)
        //         number_first_part = ( / number_first_prefix) number_first ( / number_first_suffix)
        //         number_last_part = ( / number_last_prefix) number_last ( / number_last_suffix)
        //         lot_number_part = ( / "LOT" SP ( / lot_number_prefix) ( / lot_number_first) ( / lot_number_suffix) SP)

            if (ComplexUnitType.HasValue()) {
            // unit_part level_part
                builder.Append(ComplexUnitType).Append(Sp);

                if (ComplexUnitIdentifier.HasValue()) {
                    builder.Append(ComplexUnitIdentifier).Append(Sp);
                }

                level_part(builder);

            } else {
            // level_part sub_building_number
                level_part(builder);

                if (ComplexUnitIdentifier.HasValue()) {
                    builder.Append(ComplexUnitIdentifier).Append('/');
                }
            }

            // road_number_part
            if (StreetNumber1.HasValue()) {
            // number_first_part ("-" number_last_part) SP
                builder.Append(StreetNumber1);

                if (StreetNumber2.HasValue()) {
                    builder.Append('-');
                    builder.Append(StreetNumber2);
                }

            } else {
            // lot_number_part
                builder.Append("LOT");
                builder.Append(Sp);
                builder.Append(LotIdentifier);
            }
            builder.Append(Sp);
        }

        private void level_part(StringBuilder builder)
        {
            if (ComplexLevelType.HasValue()) {
                builder.Append(ComplexLevelType).Append(Sp);
                if (ComplexLevelNumber.HasValue()) {
                    builder.Append(ComplexLevelNumber).Append(Sp);
                }
            }
        }

        private void BuildLocalityPart(StringBuilder builder)
        {
        // locality_part = "," SP locality_name SP state_abbreviation SP postcode
        // state_abbreviation = (ACT / NSW / NT / OT / QLD / SA / TAS / VIC / WA)
            builder.Append(',');
            builder.Append(Sp);
            builder.Append(LocalityName);
            builder.Append(Sp);
            builder.Append(StateTerritory);
            // temporary fix for https://psma-ts.atlassian.net/browse/PAVAPI-661 until we validate source data
            if (Postcode.HasValue()) {
                builder.Append(Sp);
                builder.Append(Postcode);
            }
        }

        private void BuildLocalityPart(StringBuilder builder, string locality)
        {
        // locality_part = "," SP locality_name SP state_abbreviation SP postcode
        // state_abbreviation = (ACT / NSW / NT / OT / QLD / SA / TAS / VIC / WA)
            builder.Append(Sp);
            builder.Append(locality);
            builder.Append(Sp);
            builder.Append(StateTerritory);
            // temporary fix for https://psma-ts.atlassian.net/browse/PAVAPI-661 until we validate source data
            if (Postcode.HasValue()) {
                builder.Append(Sp);
                builder.Append(Postcode);
            }
        }

        private const char Sp = ' ';

        // fields combined from combined.combined
        [Default(null), Name("display_address")]
        public string DisplayAddress { get; set; }

        [Default(null), Name("address_identifier")]
        public string AddressIdentifier { get; set; }

        [Default(null), Name("complex_unit_type")]
        public string ComplexUnitType { get; set; }

        [Default(null), Name("complex_unit_identifier")]
        public string ComplexUnitIdentifier { get; set; }

        [Default(null), Name("complex_level_type")]
        public string ComplexLevelType { get; set; }

        [Default(null), Name("complex_level_number")]
        public string ComplexLevelNumber { get; set; }

        [Default(null), Name("lot_identifier")]
        public string LotIdentifier { get; set; }

        [Default(null), Name("street_number_1")]
        public string StreetNumber1 { get; set; }

        [Default(null), Name("number_first")]
        public string NumberFirst { get; set; }

        [Default(null), Name("street_number_2")]
        public string StreetNumber2 { get; set; }

        [Default(null), Name("number_last")]
        public string NumberLast { get; set; }

        [Default(null), Name("street_name")]
        public string StreetName { get; set; }

        [Default(null), Name("street_type")]
        public string StreetType { get; set; }

        [Default(null), Name("street_suffix")]
        public string StreetSuffix { get; set; }

        [Default(null), Name("locality_name")]
        public string LocalityName { get; set; }

        [Default(null), Name("state_territory")]
        public string StateTerritory { get; set; }

        [Default(null), Name("postcode")]
        public string Postcode { get; set; }

        [Default(null), Name("postal_delivery_number")]
        public string PostalDeliveryNumber { get; set; }

        [Default(null), Name("postal_delivery_type")]
        public string PostalDeliveryType { get; set; }

        [Default(null), Name("latitude")]
        public string Latitude { get; set; }

        [Default(null), Name("longitude")]
        public string Longitude { get; set; }

        [Default(null), Name("cadastral_identifier")]
        public string CadastralIdentifier { get; set; }

        [Default(null), Name("geo_feature")]
        public string GeoFeature { get; set; }

        [Default(null), Name("location_descriptor")]
        public string LocationDescriptor { get; set; }

        [Default(null), Name("site_name")]
        public string SiteName { get; set; }

        [Default(null), Name("complex_unit_type_description")]
        public string ComplexUnitTypeDescription { get; set; }

        [Default(null), Name("complex_level_type_description")]
        public string ComplexLevelTypeDescription { get; set; }

        [Default(null), Name("alias_principal")]
        public string AliasPrincipal { get; set; }

        [Default(null), Name("street_aliases")]
        public string StreetAliases { get; set; }

        [Default(null), Name("locality_aliases")]
        public string LocalityAliases { get; set; }

        [Default(null), Name("locality_neighbours")]
        public string LocalityNeighbours { get; set; }

        [Default(null), Name("primary_secondary")]
        public string PrimarySecondary { get; set; }

        [Default(null), Name("gnaf_street_locality_pid")]
        public string GnafStreetLocalityPid { get; set; }

        [Default(null), Name("gnaf_locality_pid")]
        public string GnafLocalityPid { get; set; }

        [Default(null), Name("street_type_description")]
        public string StreetTypeDescription { get; set; }

        [Default(null), Name("origin")]
        public string Origin { get; set; }

        [Default(default(List<string>)), Name("data_sources"), TypeConverter(typeof(ListTypeConverter))]
        public List<string> DataSources { get; set; }
        
        [Ignore]
        public string Key => AddressIdentifier;

        public bool RequiresReplace(CombinedRecord other)
        {
            // other stuff matters like lat long
            return DisplayAddress == other.DisplayAddress;
        }
    }
}
