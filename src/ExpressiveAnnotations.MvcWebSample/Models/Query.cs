﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using ExpressiveAnnotations.Attributes;
using ExpressiveAnnotations.MvcWebSample.Inheritance;

namespace ExpressiveAnnotations.MvcWebSample.Models
{
    public class Query
    {
        public const string SIMONS_CAT = @"Simon's cat named ""\\""
 (Double Backslash)";

        public Query()
        {
            ContactDetails = new Contact();
        }

        public IEnumerable<SelectListItem> Sports => new[]
        {
            new SelectListItem {Text = Resources.None, Value = "None"},
            new SelectListItem {Text = Resources.Normal, Value = "Normal"},
            new SelectListItem {Text = Resources.Extreme, Value = "Extreme"}
        };

        public IEnumerable<SelectListItem> Countries => new[]
        {
            new SelectListItem {Text = Resources.Poland, Value = "Poland"},
            new SelectListItem {Text = Resources.Germany, Value = "Germany"},
            new SelectListItem {Text = Resources.France, Value = "France"},
            new SelectListItem {Text = Resources.Other, Value = "Other"}
        };

        public IEnumerable<SelectListItem> Answers => new[]
        {
            new SelectListItem {Text = string.Empty, Value = null},
            new SelectListItem {Text = Resources.Yes, Value = true.ToString()},
            new SelectListItem {Text = Resources.No, Value = false.ToString()}
        };

        public IEnumerable<SelectListItem> Flights => new[]
        {
            new SelectListItem {Text = string.Empty, Value = Guid.Empty.ToString()},
            new SelectListItem {Text = "58776d02-7028-4299-81f5-db234c44b294", Value = "58776d02-7028-4299-81f5-db234c44b294"},
            new SelectListItem {Text = "8b7ee575-eeaa-441c-811f-db6eaacc7115", Value = "8b7ee575-eeaa-441c-811f-db6eaacc7115"},
            new SelectListItem {Text = "6b403167-5792-4871-bdf3-cdce8e9b90c0", Value = "6b403167-5792-4871-bdf3-cdce8e9b90c0"}
        };

        public IEnumerable<int?> Years => Enumerable.Range(15, 82).Cast<int?>();

        [UIHint("IntArray")]
        public int[] EarlyYears => new[] {15, 16, 17};

        public int[] AvailableDonations => new[] {1, 4, 9, 16, 25, 36, 49};

        public TimeSpan WeekPeriod => new TimeSpan(7, 0, 0, 0);

        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.GoAbroad))]
        public bool GoAbroad { get; set; }
        
        [Required(ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FieldRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.Age))]
        public ushort? Age { get; set; }

        [RequiredIf("GoAbroad == true", ErrorMessage = "?")]
        public string ID { get; set; }

        [RequiredIf("GoAbroad == true",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FieldConditionallyRequired))]
        [AssertThat("IsDigitChain(PassportNumber)",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.DigitsOnlyAccepted))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.PassportNumber))]
        public string PassportNumber { get; set; }        

        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.Country))]
        public string Country { get; set; }

        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.NextCountry))]
        public string NextCountry { get; set; }

        [RequiredIf(@"GoAbroad == true
                      && (
                             (NextCountry != 'Other' && NextCountry == Country)
                             || (Age > 24 && 55 >= Age)
                         )",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.ReasonForTravelRequired))]
        [RequiredIf("ArrayContains(Age, [15, 16, 17])", // alternative without array literal: ArrayContains(Age, EarlyYears)",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.ReasonForTravelRequiredForYouth))]
        [AssertThat(@"ReasonForTravel != 'John\'s cat named ""\\\'""\n (Backslash Quote)' && ReasonForTravel != SIMONS_CAT",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.SecretAnswerDetected))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.ReasonForTravel))]
        public string ReasonForTravel { get; set; }

        [UIHint("ISO8601Date")]
        [ValueParser("NonStandardDateParser")] // serialized DOM field value, when language is set to polish, is given in yyyy-mm-dd format -
                                               // - because built-in client-side logic cannot handle such a format, custom parser is indicated for use by this attribute
        public DateTime LatestSuggestedReturnDate { get; set; }

        [RequiredIf("GoAbroad == true",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FieldConditionallyRequired))]
        [AssertThat("ReturnDate >= Today()", Priority = 1, // to be invoked firstly
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FutureDateRequired))]
        [AssertThat("ReturnDate >= Today() + WeekPeriod", Priority = 2, // to be invoked secondly
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.MoreThanAWeekRequired))]
        [AssertThat("ReturnDate < AddYears(Today(), 1)",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.NoMoreThanAYear))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.ReturnDate))]
        [ValueParser("NonStandardDateParser")]
        public DateTime? ReturnDate { get; set; }

        [RequiredIf("GoAbroad == true && ReturnDate > LatestSuggestedReturnDate", AllowEmptyStrings = true,
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.ReasonForLongTravelRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.ReasonForLongTravel))]
        public string ReasonForLongTravel { get; set; }

        [RequiredIf("GoAbroad == true",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FieldConditionallyRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.PoliticalStability))]
        public Stability? PoliticalStability { get; set; }

        [AssertThat(@"(
                          AwareOfTheRisks == true
                          && (PoliticalStability == Stability.Low || PoliticalStability == Stability.Uncertain)
                      ) 
                      || PoliticalStability == null || PoliticalStability == Stability.High",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.AwareOfTheRisksRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.AwareOfTheRisks))]
        public bool AwareOfTheRisks { get; set; }

        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.SportType))]
        public string SportType { get; set; }

        [RequiredIf("SportType == 'Extreme' || (SportType != 'None' && GoAbroad == true)",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.BloodTypeRequired))]
        [AssertThat("IsBloodType(Trim(BloodType))",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.BloodTypeInvalid))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.BloodType))]
        public string BloodType { get; set; }

        [AssertThat("AgreeForContact == true",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.AgreeForContactRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.AgreeForContact))]
        public bool AgreeForContact { get; set; }

        [RequiredIf(@"AgreeForContact == true
                      && (ContactDetails.Email != null || ContactDetails.Phone != null)
                      && (ContactDetails.Addresses[0].Details != null || ContactDetails.Addresses[1].Details != null)",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.ImmediateContactRequired))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.ImmediateContact))]
        public bool? ImmediateContact { get; set; }

        [AssertThat(@"FlightId != Guid('00000000-0000-0000-0000-000000000000') || !GoAbroad",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FlightIdentifierInvalid))]        
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.FlightId))]
        public Guid FlightId { get; set; }

        [RequiredIf("GoAbroad == true",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.FieldConditionallyRequired))]
        [AssertThat(@"GoAbroad
                          ? ArrayLength(SelectedDonations) > 2
                          : ArrayLength(SelectedDonations) > 1",
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.NotEnoughDonations))]
        [ValueParser("ArrayParser")]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.Donation))]
        public int[] SelectedDonations { get; set; }

        [CustomRequiredIf("GoAbroad == true")]
        [CustomAssertThat("Length(コメント) > 1e1 - 1", Priority = 2)]
        [CustomAssertThat("Length(コメント) > 1e1 - 6", Priority = 1, 
            ErrorMessageResourceType = typeof (Resources), ErrorMessageResourceName = nameof(Resources.JustAFewCharsMore))]
        [Display(ResourceType = typeof (Resources), Name = nameof(Resources.Comment))]
        public string コメント { get; set; }

        public Contact ContactDetails { get; set; }

        public bool IsBloodType(string group)
        {
            return Regex.IsMatch(group, @"^(A|B|AB|0)[\+-]$"); /* Reminder: verbatim string usage is adviced when working with regex patterns. Regex patterns are full of backslashes, 
                                                                * and backslash characters need to be escaped in regular string literals. Verbatim string literal, on the other hand, 
                                                                * is such a string which does not need to be escaped. It is treated literally by the compiler, since compiler does not 
                                                                * interpret backslash control characters anymore - they lose any special significance for it (one thing to remember is 
                                                                * usage of "" for quote escape sequence: http://msdn.microsoft.com/en-us/library/aa691090(v=vs.71).aspx).
                                                                */
        }

        public DateTime AddYears(DateTime from, int years)
        {
            return from.AddYears(years);
        }
    }
}
