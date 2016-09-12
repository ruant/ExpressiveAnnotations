﻿/* https://github.com/jwaliszko/ExpressiveAnnotations
 * Copyright (c) 2014 Jarosław Waliszko
 * Licensed MIT: http://opensource.org/licenses/MIT */

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Web.Mvc;
using ExpressiveAnnotations.Analysis;
using ExpressiveAnnotations.Attributes;
using ExpressiveAnnotations.Functions;
using ExpressiveAnnotations.MvcUnobtrusive.Caching;

namespace ExpressiveAnnotations.MvcUnobtrusive.Validators
{
    /// <summary>
    ///     Base class for expressive validators.
    /// </summary>
    /// <typeparam name="T">Any type derived from <see cref="ExpressiveAttribute" /> class.</typeparam>
    public abstract class ExpressiveValidator<T> : DataAnnotationsModelValidator<T> where T : ExpressiveAttribute
    {
        /// <summary>
        ///     Constructor for expressive model validator.
        /// </summary>
        /// <param name="metadata">The model metadata instance.</param>
        /// <param name="context">The controller context instance.</param>
        /// <param name="attribute">The expressive attribute instance.</param>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException"></exception>
        protected ExpressiveValidator(ModelMetadata metadata, ControllerContext context, T attribute)
            : base(metadata, context, attribute)
        {
            try
            {
                Debug.WriteLine($"[ctor entry] process: {Process.GetCurrentProcess().Id}, thread: {Thread.CurrentThread.ManagedThreadId}");

                var fieldId = $"{metadata.ContainerType.FullName}.{metadata.PropertyName}".ToLowerInvariant();
                AttributeFullId = $"{attribute.TypeId}.{fieldId}".ToLowerInvariant();
                AttributeWeakId = $"{typeof (T).FullName}.{fieldId}".ToLowerInvariant();
                FieldName = metadata.PropertyName;

                ResetSuffixAllocation();

                var item = MapCache<string, CacheItem>.GetOrAdd(AttributeFullId, _ => // map cache is based on static dictionary, set-up once for entire application instance
                {                                                  // (by design, no reason to recompile once compiled expressions)
                    Debug.WriteLine($"[cache add] process: {Process.GetCurrentProcess().Id}, thread: {Thread.CurrentThread.ManagedThreadId}");

                    var parser = new Parser();
                    parser.RegisterToolchain();
                    parser.Parse<bool>(metadata.ContainerType, attribute.Expression);

                    var fields = parser.GetFields();
                    FieldsMap = fields.ToDictionary(x => x.Key, x => Helper.GetCoarseType(x.Value.Type));
                    ConstsMap = parser.GetConsts();
                    ParsersMap = fields
                        .Select(kvp => new
                        {
                            FullName = kvp.Key,
                            ParserAttribute = ((MemberExpression) kvp.Value).Member.GetAttributes<ValueParserAttribute>().SingleOrDefault()
                        }).Where(x => x.ParserAttribute != null)
                        .ToDictionary(x => x.FullName, x => x.ParserAttribute.ParserName);

                    if (!ParsersMap.ContainsKey(metadata.PropertyName))
                    {
                        var currentField = metadata.ContainerType
                            .GetProperties().Single(p => metadata.PropertyName == p.Name);
                        var valueParser = currentField.GetAttributes<ValueParserAttribute>().SingleOrDefault();
                        if (valueParser != null)
                            ParsersMap.Add(new KeyValuePair<string, string>(metadata.PropertyName, valueParser.ParserName));
                    }

                    AssertNoNamingCollisionsAtCorrespondingSegments();
                    attribute.Compile(metadata.ContainerType); // compile expressions in attributes (to be cached for subsequent invocations)

                    return new CacheItem
                    {
                        FieldsMap = FieldsMap,
                        ConstsMap = ConstsMap,
                        ParsersMap = ParsersMap
                    };
                });

                FieldsMap = item.FieldsMap;
                ConstsMap = item.ConstsMap;
                ParsersMap = item.ParsersMap;

                Expression = attribute.Expression;

                IDictionary<string, Guid> errFieldsMap;
                FormattedErrorMessage = attribute.FormatErrorMessage(metadata.GetDisplayName(), attribute.Expression, metadata.ContainerType, out errFieldsMap); // fields names, in contrast to values, do not change in runtime, so will be provided in message (less code in js)
                ErrFieldsMap = errFieldsMap;
            }
            catch (Exception e)
            {
                throw new ValidationException(
                    $"{GetType().Name}: validation applied to {metadata.PropertyName} field failed.", e);
            }
        }

        /// <summary>
        ///     Gets the expression.
        /// </summary>
        protected string Expression { get; private set; }

        /// <summary>
        ///     Gets the formatted error message.
        /// </summary>
        protected string FormattedErrorMessage { get; private set; }

        /// <summary>
        ///     Gets fields names and corresponding guid identifiers obfuscating such fields in error message string.
        /// </summary>
        protected IDictionary<string, Guid> ErrFieldsMap { get; private set; }

        /// <summary>
        ///     Gets names and coarse types of properties extracted from specified expression within given context.
        /// </summary>
        protected IDictionary<string, string> FieldsMap { get; private set; }

        /// <summary>
        ///     Gets properties names and parsers registered for them via <see cref="ValueParserAttribute" />.
        /// </summary>
        protected IDictionary<string, string> ParsersMap { get; private set; }

        /// <summary>
        ///     Gets names and values of constants extracted from specified expression within given context.
        /// </summary>
        protected IDictionary<string, object> ConstsMap { get; private set; }

        /// <summary>
        ///     Gets attribute strong identifier - attribute type identifier concatenated with annotated field identifier.
        /// </summary>
        private string AttributeFullId { get; set; }

        /// <summary>
        ///     Gets attribute partial identifier - attribute type name concatenated with annotated field identifier.
        /// </summary>
        private string AttributeWeakId { get; set; }

        /// <summary>
        ///     Gets name of the annotated field.
        /// </summary>        
        private string FieldName { get; set; }

        /// <summary>
        ///     Generates client validation rule with the basic set of parameters.
        /// </summary>
        /// <param name="type">The validation type.</param>
        /// <returns>
        ///     Client validation rule with the basic set of parameters.
        /// </returns>
        /// <exception cref="System.ComponentModel.DataAnnotations.ValidationException"></exception>
        protected ModelClientValidationRule GetBasicRule(string type)
        {
            try
            {
                var rule = new ModelClientValidationRule
                {
                    ErrorMessage = FormattedErrorMessage,
                    ValidationType = ProvideUniqueValidationType(type)
                };

                rule.ValidationParameters.Add("expression", Expression.ToJson());

                Debug.Assert(FieldsMap != null);
                if (FieldsMap.Any())
                    rule.ValidationParameters.Add("fieldsmap", FieldsMap.ToJson());
                Debug.Assert(ConstsMap != null);
                if (ConstsMap.Any())
                    rule.ValidationParameters.Add("constsmap", ConstsMap.ToJson());
                Debug.Assert(ParsersMap != null);
                if (ParsersMap.Any())
                    rule.ValidationParameters.Add("parsersmap", ParsersMap.ToJson());
                Debug.Assert(ErrFieldsMap != null);
                if (ErrFieldsMap.Any())
                    rule.ValidationParameters.Add("errfieldsmap", ErrFieldsMap.ToJson());

                return rule;
            }
            catch (Exception e)
            {
                throw new ValidationException(
                    $"{GetType().Name}: collecting of client validation rules for {FieldName} field failed.", e);
            }
        }

        /// <summary>
        ///     Provides unique validation type within current annotated field range, when multiple annotations are used (required for client-side).
        /// </summary>
        /// <param name="baseName">Base name.</param>
        /// <returns>
        ///     Unique validation type within current request.
        /// </returns>
        private string ProvideUniqueValidationType(string baseName)
        {
            return $"{baseName}{AllocateSuffix()}";
        }

        private string AllocateSuffix()
        {
            var count = RequestStorage.Get<int>(AttributeWeakId);
            count++;
            AssertAttribsQuantityAllowed(count);
            RequestStorage.Set(AttributeWeakId, count);
            return count == 1 ? string.Empty : char.ConvertFromUtf32(95 + count); // single lowercase letter from latin alphabet or an empty string
        }

        private void ResetSuffixAllocation()
        {
            RequestStorage.Remove(AttributeWeakId);
        }

        private void AssertNoNamingCollisionsAtCorrespondingSegments()
        {
            string name;
            int level;
            if (Helper.SegmentsCollide(FieldsMap.Keys, ConstsMap.Keys, out name, out level))
                throw new InvalidOperationException(
                    $"Naming collisions cannot be accepted by client-side - {name} part at level {level} is ambiguous.");
        }

        private void AssertAttribsQuantityAllowed(int count)
        {
            const int max = 27;
            if (count > max)
                throw new InvalidOperationException(
                    $"No more than {max} unique attributes of the same type can be applied for a single field or property.");
        }
    }
}
