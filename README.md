﻿![logo](logo.png)

#<a id="expressiveannotations-annotation-based-conditional-validation">ExpressiveAnnotations<sup><sup><sup>[annotation-based conditional validation]</sup></sup></sup></a>

[![Build status](https://img.shields.io/appveyor/ci/jwaliszko/ExpressiveAnnotations.svg)](https://ci.appveyor.com/project/jwaliszko/ExpressiveAnnotations)
[![Coverage status](https://img.shields.io/codecov/c/github/jwaliszko/ExpressiveAnnotations.svg)](https://codecov.io/github/jwaliszko/ExpressiveAnnotations)
[![Release version](https://img.shields.io/github/release/jwaliszko/ExpressiveAnnotations.svg)](https://github.com/jwaliszko/ExpressiveAnnotations/releases/latest)
[![License](http://img.shields.io/badge/license-MIT-blue.svg)](http://opensource.org/licenses/MIT)

A small .NET and JavaScript library which provides annotation-based conditional validation mechanisms. Given attributes allow to forget about imperative way of step-by-step verification of validation conditions in many cases. Since fields validation requirements are applied as metadata, domain-related code is more condensed.

###Table of contents
 - [What is the context behind this work?](#what-is-the-context-behind-this-implementation)
 - [`RequiredIf` vs. `AssertThat` - where is the difference?](#requiredif-vs-assertthat---where-is-the-difference)
 - [What are brief examples of usage?](#what-are-brief-examples-of-usage)
 - [Declarative vs. imperative programming - what is it about?](#declarative-vs-imperative-programming---what-is-it-about)
 - [EA expressions specification](#expressions-specification)
   - [Grammar definition](#grammar-definition)
   - [Operators precedence and associativity](#operators-precedence)
   - [Built-in functions (methods ready to be used by expressions)](#built-in-functions)
 - [How to construct conditional validation attributes?](#how-to-construct-conditional-validation-attributes)
   - [Signatures description](#signatures)
   - [Implementation details outline](#implementation)
   - [Traps (discrepancies between server- and client-side expressions evaluation)](#traps)   
 - [What about the support of ASP.NET MVC client-side validation?](#what-about-the-support-of-aspnet-mvc-client-side-validation)
 - [Frequently asked questions](#frequently-asked-questions)
   - [Is it possible to compile all usages of annotations at once?](#is-it-possible-to-compile-all-usages-of-annotations-at-once) <sup>(re server-side)</sup>
   - [What if there is no built-in function I need?](#what-if-there-is-no-built-in-function-i-need) <sup>(re client and server-side)</sup>
   - [Can I have custom utility-like functions outside of my models?](#can-I-have-custom-utility-like-functions-outside-of-my-models) <sup>(re server-side)</sup>
   - [How to cope with values of custom types?](#how-to-cope-with-values-of-custom-types) <sup>(re client-side)</sup>
   - [How to cope with dates given in non-standard formats?](#how-to-cope-with-dates-given-in-non-standard-formats) <sup>(re client-side)</sup>
   - [What if `ea` variable is already used by another library?](#what-if-ea-variable-is-already-used-by-another-library) <sup>(re client-side)</sup>
   - [How to control frequency of dependent fields validation?](#how-to-control-frequency-of-dependent-fields-validation) <sup>(re client-side)</sup>
   - [Can I increase web console verbosity for debug purposes?](#can-i-increase-web-console-verbosity-for-debug-purposes) <sup>(re client-side)</sup>
   - [How to fetch field value or display name in error message?](#how-to-fetch-field-value-or-display-name-in-error-message) <sup>(re client and server-side)</sup>
   - [Is there any event raised when validation is done?](#is-there-any-event-raised-when-validation-is-done) <sup>(re client-side)</sup>
   - [`RequiredIf` attribute is not working, what is wrong?](#requiredif-attribute-is-not-working-what-is-wrong) <sup>(re client and server-side)</sup>
   - [Is there a possibility to perform asynchronous validation?](#is-there-a-possibility-to-perform-asynchronous-validation) <sup>(re client-side, experimental)</sup>
   - [What if my question is not covered by FAQ section?](#what-if-my-question-is-not-covered-by-faq-section)
 - [Installation instructions](#installation)
 - [Contributors](#contributors)
 - [License](#license)

###<a id="what-is-the-context-behind-this-implementation">What is the context behind this work?</a>

There are number of cases where the concept of metadata is used for justified reasons. Attributes are one of the ways to associate complementary information with existing data. Such annotations may also define the correctness of data. 

Declarative validation when [compared](#declarative-vs-imperative-programming---what-is-it-about) to imperative approach seems to be more convenient in many cases. Clean, compact code - all validation logic defined within the model scope. Simple to write, obvious to read.

###<a id="requiredif-vs-assertthat---where-is-the-difference">`RequiredIf` vs. `AssertThat` - where is the difference?</a>

* `RequiredIf` - if value is not yet provided, check whether it is required (annotated field is required to be non-null, when given condition is satisfied),
* `AssertThat` - if value is already provided, check whether the condition is met (non-null annotated field is considered as valid, when given condition is satisfied).

###<a id="what-are-brief-examples-of-usage">What are brief examples of usage?</a>

If you'll be interested in comprehensive examples afterwards, take a look inside chosen demo project:

* [**ASP.NET MVC web sample**](src/ExpressiveAnnotations.MvcWebSample),
* [**WPF MVVM desktop sample**](src/ExpressiveAnnotations.MvvmDesktopSample).

For the time being, to keep your ear to the ground, let's walk through few exemplary code snippets:
```C#
using ExpressiveAnnotations.Attributes;

[RequiredIf("GoAbroad == true")]
public string PassportNumber { get; set; }
```
Above we are saying, that annotated field is required when condition given in the logical expression is satisfied (passport number is required, if go abroad field has true boolean value).

Simple enough, let's move to another variation:
```C#
[AssertThat("ReturnDate >= Today()")]
public DateTime? ReturnDate { get; set; }
```
By the usage of this attribute type, we are not validating field requirement as before - its value is allowed to be null this time. Nevertheless, if some value is already given, provided restriction needs to be satisfied (return date needs to be greater than or equal to the date returned by `Today()` [built-in function](#built-in-functions)).

As shown below, both types of attributes may be combined (moreover, the same type can be applied multiple times for a single field):
```C#
[RequiredIf("Details.Email != null")]
[RequiredIf("Details.Phone != null")]
[AssertThat("AgreeToContact == true")]
public bool? AgreeToContact { get; set; }
```
Literal translation means, that if either email or phone is provided, you are forced to authorize someone to contact with you (boolean value indicating contact permission has to be true). What is more, we can see that nested properties are supported by [the expressions parser](#implementation). 

Finally, take a brief look at following construction:
```C#
[RequiredIf(@"GoAbroad == true
              && (
                     (NextCountry != 'Other' && NextCountry == Country)
                     || (Age > 24 && Age <= 55)
                 )")]
public string ReasonForTravel { get; set; }
```

Restriction above is slightly more complex than its predecessors, but still can be quickly understood (reason for travel has to be provided if you plan to go abroad and, either want to visit the same definite country twice, or are between 25 and 55).

###<a id="declarative-vs-imperative-programming---what-is-it-about">Declarative vs. imperative programming - what is it about?</a>

With **declarative** programming you write logic that expresses *what* you want, but not necessarily *how* to achieve it. You declare your desired results, but not step-by-step.

In our case, this concept is materialized by attributes, e.g.
```C#
[RequiredIf("GoAbroad == true && NextCountry != 'Other' && NextCountry == Country",
    ErrorMessage = "If you plan to travel abroad, why visit the same country twice?")]
public string ReasonForTravel { get; set; }
```
Here, we're saying "Ensure the field is required according to given condition."

With **imperative** programming you define the control flow of the computation which needs to be done. You tell the compiler what you want, exactly step by step.

If we choose this way instead of model fields decoration, it has negative impact on the complexity of the code. Logic responsible for validation is now implemented somewhere else in our application, e.g. inside controllers actions instead of model class itself:
```C#
    if (!model.GoAbroad)
        return View("Success");
    if (model.NextCountry == "Other")
        return View("Success");
    if (model.NextCountry != model.Country)
        return View("Success");
    
    ModelState.AddModelError("ReasonForTravel", 
        "If you plan to travel abroad, why visit the same country twice?");
    return View("Home", model);
}
```
Here instead, we're saying "If condition is met, return some view. Otherwise, add error message to state container. Return other view."

###<a id="expressions-specification">EA expressions specification</a>

#####<a id="grammar">Grammar definition</a>

Valid expressions handled by EA parser comply with syntax defined by the following grammar:

```
exp         => cond-exp
cond-exp    => l-or-exp ['?' exp ':' exp]       // right associative (right recursive)
l-or-exp    => l-and-exp  ('||' l-and-exp)*     // left associative (non-recursive alternative to left recursion)
l-and-exp   => b-and-exp ('&&' b-and-exp)*
b-or-exp    => xor-exp ('|' xor-exp)*
xor-exp     => b-and-exp ('^' b-and-exp)*
b-and-exp   => eq-exp ('&' eq-exp)*
eq-exp      => rel-exp (('==' | '!=') rel-exp)*
rel-exp     => shift-exp (('>' | '>=' | '<' | '<=') shift-exp)*
shift-exp   => add-exp (('<<' | '>>') add-exp)*
add-exp     => mul-exp (('+' | '-') mul-exp)*
mul-exp     => unary-exp (('*' | '/' | '%')  unary-exp)*
unary-exp   => ('+' | '-' | '!' | '~') unary-exp | primary-exp
primary-exp => null-lit | bool-lit | num-lit | string-lit | arr-access | id-access | '(' exp ')'

arr-access  => arr-lit |
               arr-lit '[' exp ']' ('[' exp ']' | '.' identifier)*

id-access   => identifier |
               identifier ('[' exp ']' | '.' identifier)* |
               func-call ('[' exp ']' | '.' identifier)*
               
func-call   => identifier '(' [exp-list] ')'

null-lit    => 'null'
bool-lit    => 'true' | 'false'
num-lit     => int-lit | float-lit
int-lit     => dec-lit | bin-lit | hex-lit
array-lit   => '[' [exp-list] ']'

exp-list    => exp (',' exp)*
```
Terminals are expressed in quotes. Each nonterminal is defined by a rule in the grammar except for *dec-lit*, *bin-lit*, *hex-lit*, *float-lit*, *string-lit* and *identifier*, which are assumed to be implicitly defined (*identifier* specifies names of functions, properties, constants and enums).

Expressions are built of unicode letters and numbers (i.e. `[L*]` and `[N*]` [categories](https://en.wikipedia.org/wiki/Unicode_character_property) respectively) with the usage of following components:

* logical operators: `!a`, `a||b`, `a&&b`,
* comparison operators: `a==b`, `a!=b`, `a<b`, `a<=b`, `a>b`, `a>=b`,
* arithmetic operators: `+a`, `-a`, `a+b`, `a-b`, `a*b`, `a/b`, `a%b`, `~a`, `a&b`, `a^b`, `a|b`, `a<<b`, `a>>b`,
* other operators: `a()`, `a[]`, `a.b`, `a?b:c`,
* literals:
  * null, i.e. `null`,
  * boolean, i.e. `true` and `false`,
  * decimal integer, e.g. `123`,
  * binary integer (with `0b` prefix), e.g. `0b1010`,
  * hexadecimal integer (with `0x` prefix), e.g. `0xFF`,
  * float, e.g. `1.5` or `0.3e-2`,
  * string, e.g. `'in single quotes'` (internal quote escape sequence is `\'`, character representing new line is `\n`),
  * array (comma separated items within square brackets), e.g. `[1,2,3]`,
  * identifier, i.e. names of functions, properties, constants and enums.

#####<a id="operators-precedence">Operators precedence and associativity</a>

The following table lists the precedence and associativity of operators (listed top to bottom, in descending precedence):

<table>
    <thead>
        <tr>
            <th>Precedence</th>
            <th>Operator</th>
            <th>Description</th>
            <th>Associativity</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td valign="top" rowspan="3">1</td>
            <td>
                <code>()</code><br />
            </td>
            <td>Function call (postfix)</td>
            <td valign="top" rowspan="3">Left to right</td>
        </tr>
        <tr>
            <td>
                <code>[]</code><br />
            </td>
            <td>Subscript (postfix)</td>
        </tr>
        <tr>
            <td>
                <code>.</code>
            </td>
            <td>Member access (postfix)</td>
        </tr>
        <tr>
            <td valign="top" rowspan="2">2</td>
            <td>
                <code>+</code> <code>-</code>
            </td>
            <td>Unary plus and minus</td>
            <td valign="top" rowspan="2">Right to left</td>
        </tr>
        <tr>
            <td>
                <code>!</code> <code>~</code>
            </td>
            <td>Logical NOT and bitwise NOT (one's complement)</td>
        </tr>
        <tr>
            <td>3</td>
            <td>
                <code>*</code> <code>/</code> <code>%</code>
            </td>
            <td>Multiplication, division, and remainder</td>
            <td valign="top" rowspan="11">Left to right</td>
        </tr>
        <tr>
            <td>4</td>
            <td>
                <code>+</code> <code>-</code>
            </td>
            <td>Addition and subtraction</td>
        </tr>
        <tr>
            <td>5</td>
            <td>
                <code>&lt;&lt;</code> <code>&gt;&gt;</code>
            </td>
            <td>Bitwise left shift and right shift</td>
        </tr>
        <tr>
            <td valign="top" rowspan="2">6</td>
            <td>
                <code>&lt;</code> <code>&lt;=</code>
            </td>
            <td>Relational operators &lt; and ≤ respectively</td>
        </tr>
        <tr>
            <td>
                <code>&gt;</code> <code>&gt;=</code>
            </td>
            <td>Relational operators &gt; and ≥ respectively</td>
        </tr>
        <tr>
            <td>7</td>
            <td>
                <code>==</code> <code>!=</code>
            </td>
            <td>Equality operators = and ≠ respectively</td>
        </tr>
        <tr>
            <td>8</td>
            <td><code>&amp;</code></td>
            <td>Bitwise AND</td>
        </tr>
        <tr>
            <td>9</td>
            <td><code>^</code></td>
            <td>Bitwise XOR (exclusive OR)</td>
        </tr>
        <tr>
            <td>10</td>
            <td><code>|</code></td>
            <td>Bitwise OR (inclusive OR)</td>
        </tr>
        <tr>
            <td>11</td>
            <td><code>&amp;&amp;</code></td>
            <td>Logical AND</td>
        </tr>
        <tr>
            <td>12</td>
            <td><code>||</code></td>
            <td>Logical OR</td>
        </tr>
        <tr>
            <td>13</td>
            <td><code>?:</code></td>
            <td>Ternary conditional</td>
            <td>Right to left</td>
        </tr>
    </tbody>
</table>

#####<a id="built-in-functions">Built-in functions (methods ready to be used by expressions)</a>

As already noted, there is an option to reinforce expressions with functions, e.g.
```C#
[AssertThat("StartsWith(CodeName, 'abc.') || EndsWith(CodeName, '.xyz')")]
public string CodeName { get; set; }
```
Toolchain functions available out of the box at server- and client-side:

* `DateTime Now()`
    * Gets the current local date and time (client-side returns the number of milliseconds since January 1, 1970, 00:00:00 UTC).
* `DateTime Today()`
    * Gets the current date with the time component set to 00:00:00 (client-side returns the number of milliseconds since January 1, 1970, 00:00:00 UTC).
* `DateTime Date(int year, int month, int day)`
    * Initializes a new date to a specified year, month (months are 1-based), and day, with the time component set to 00:00:00 (client-side returns the number of milliseconds since January 1, 1970, 00:00:00 UTC).
* `DateTime Date(int year, int month, int day, int hour, int minute, int second)`
    * Initializes a new date to a specified year, month (months are 1-based), day, hour, minute, and second (client-side returns the number of milliseconds since January 1, 1970, 00:00:00 UTC).
* `DateTime ToDate(string dateString)`
    * Converts the specified string representation of a date and time to its equivalents: `DateTime` at server-side - uses .NET [`DateTime.Parse(string dateString)`](https://msdn.microsoft.com/en-us/library/vstudio/1k1skd40(v=vs.100).aspx), and number of milliseconds since January 1, 1970, 00:00:00 UTC at client-side - uses JavaScript [`Date.parse(dateString)`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/parse).
* `TimeSpan TimeSpan(int days, int hours, int minutes, int seconds)`
    * Initializes a new time period according to specified days, hours, minutes, and seconds (client-side period is expressed in milliseconds).
* `int Length(str)`
    * Gets the number of characters in a specified string (null-safe).
* `string Trim(string str)`
    * Removes all leading and trailing white-space characters from a specified string (null-safe).
* `string Concat(string strA, string strB)`
    * Concatenates two specified strings (null-safe).
* `string Concat(string strA, string strB, strC)`
    * Concatenates three specified strings (null-safe).
* `int CompareOrdinal(string strA, string strB)`
    * Compares strings using ordinal sort rules. An integer that indicates the lexical relationship 
      between the two comparands is returned (null-safe): 
        * -1    - strA is less than strB,
        * &nbsp;0    - strA and strB are equal,
        * &nbsp;1    - strA is greater than strB.
* `int CompareOrdinalIgnoreCase(string strA, string strB)`
    * Compares strings using ordinal sort rules and ignoring the case of the strings being compared (null-safe).
* `bool StartsWith(string str, string prefix)`
    * Determines whether the beginning of specified string matches a specified prefix (null-safe).
* `bool StartsWithIgnoreCase(string str, string prefix)`
    * Determines whether the beginning of specified string matches a specified prefix, ignoring the case of the strings (null-safe).
* `bool EndsWith(string str, string suffix)`
    * Determines whether the end of specified string matches a specified suffix (null-safe).
* `bool EndsWithIgnoreCase(string str, string suffix)`
    * Determines whether the end of specified string matches a specified suffix, ignoring the case of the strings (null-safe).
* `bool Contains(string str, string substr)`
    * Returns a value indicating whether a specified substring occurs within a specified string (null-safe).
* `bool ContainsIgnoreCase(string str, string substr)`
    * Returns a value indicating whether a specified substring occurs within a specified string, ignoring the case of the strings (null-safe).
* `bool IsNullOrWhiteSpace(string str)`
    * Indicates whether a specified string is null, empty, or consists only of white-space characters (null-safe).
* `bool IsDigitChain(string str)`
    * Indicates whether a specified string represents a sequence of digits (ASCII characters only, null-safe).
* `bool IsNumber(string str)`
    * Indicates whether a specified string represents integer or float number (ASCII characters only, null-safe).
* `bool IsEmail(string str)`
    * Indicates whether a specified string represents valid e-mail address (null-safe).
* `bool IsUrl(string str)`
    * Indicates whether a specified string represents valid url (null-safe).
* `bool IsRegexMatch(string str, string regex)`
    * Indicates whether the regular expression finds a match in the input string (null-safe).
* `Guid Guid(string str)`
    * Initializes a new instance of the Guid structure by using the value represented by a specified string.
* `double Min(params double[] values)`
    * Returns the minimum value in a sequence of numeric values.
* `double Max(params double[] values)`
    * Returns the maximum value in a sequence of numeric values.
* `double Sum(params double[] values)`
    * Computes the sum of the numeric values in a sequence.
* `double Average(params double[] values)`
    * Computes the average of the numeric values in a sequence.

###<a id="how-to-construct-conditional-validation-attributes">How to construct conditional validation attributes?</a>

#####<a id="signatures">Signatures description</a>

```
RequiredIfAttribute(
    string expression,
    [bool AllowEmptyStrings], 
    [int Priority]           
    [string ErrorMessage]    ...) - Validation attribute, executed for null-only annotated
                                    field, which indicates that such a field is required
                                    to be non-null, when computed result of given logical 
                                    expression is true.
AssertThatAttribute(
    string expression,
    [int Priority]           
    [string ErrorMessage]    ...) - Validation attribute, executed for non-null annotated 
                                    field, which indicates that assertion given in logical 
                                    expression has to be satisfied, for such a field to be 
                                    considered as valid.

expression        - The logical expression based on which specified condition is computed.
AllowEmptyStrings - Gets or sets a flag indicating whether the attribute should allow empty 
                    or whitespace strings. False by default.
Priority          - Gets or sets the hint, available for any concerned external components, 
                    indicating the order in which this attribute should be executed among 
                    others of its kind, i.e. ExpressiveAttribute. Value is optional and not
                    set by default, which means that execution order is undefined.
ErrorMessage      - Gets or sets an explicit error message string. A difference to default 
                    behavior is awareness of new format items, i.e. {fieldPath[:indicator]}. 
                    Given in curly brackets, can be used to extract values of specified 
                    fields, e.g. {field}, {field.field}, within current model context or 
                    display names of such fields, e.g. {field:n}. Braces can be escaped by 
                    double-braces, i.e. to output a { use {{ and to output a } use }}. The 
                    same logic works for messages provided in resources.
```

Note above covers almost exhaustively what is actually needed to work with EA. Nevertheless, the full API documentation, generated with [Sandcastle](https://sandcastle.codeplex.com/) (with the support of [SHFB](http://shfb.codeplex.com/)), can be downloaded (in the form of compiled HTML help file) from [here](doc/api/api.chm?raw=true) (includes only C# API, no JavaScript part there).

#####<a id="implementation">Implementation details outline</a>

Implementation core is based on [expressions parser](src/ExpressiveAnnotations/Analysis/Parser.cs?raw=true), which runs on the grammar [shown above](#grammar-definition).

Specified expression string is parsed and converted into [expression tree](http://msdn.microsoft.com/en-us/library/bb397951.aspx) structure. A delegate containing compiled version of the lambda expression described by produced expression tree is returned as a result of the parser job. Such delegate is then invoked for specified model object. As a result of expression evaluation, boolean flag is returned, indicating that expression is true or false.

For the sake of performance optimization, expressions provided to attributes are compiled only once. Such compiled lambdas are then cached inside attributes instances and invoked for any subsequent validation requests without recompilation.

When working with ASP.NET MVC stack, unobtrusive client-side validation mechanism is [additionally available](#what-about-the-support-of-aspnet-mvc-client-side-validation). Client receives unchanged expression string from server. Such an expression is then evaluated using JavaScript [`eval()`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/eval) method within the context of reflected model object. Such a model, analogously to the server-side one, is basically deserialized DOM form (with some type-safety assurances and registered toolchain methods).

#####<a id="traps">Traps (discrepancies between server- and client-side expressions evaluation)</a>

Because client-side handles expressions in its unchanged form (as provided to attribute), attention is needed when dealing with `null` keyword - there are discrepancies between EA parser (mostly follows C# rules) and JavaScript, e.g.

* `null + "text"` - in C# `"text"`, in JS `"nulltext"`,
* `2 * null`      - in C# `null`  , in JS `0`,
* `null > -1`     - in C# `false` , in JS `true`,
* and more...

###<a id="what-about-the-support-of-aspnet-mvc-client-side-validation">What about the support of ASP.NET MVC client-side validation?</a>

Client-side validation is fully supported. Enable it for your web project within the next few steps:

1. Reference both assemblies to your project: core [**ExpressiveAnnotations.dll**](src/ExpressiveAnnotations) and subsidiary [**ExpressiveAnnotations.MvcUnobtrusive.dll**](src/ExpressiveAnnotations.MvcUnobtrusive).
2. In Global.asax register required validators (`IClientValidatable` interface is not directly implemented by the attributes, to avoid coupling of ExpressionAnnotations assembly with System.Web.Mvc dependency):

    ```C#
    using ExpressiveAnnotations.Attributes;
    using ExpressiveAnnotations.MvcUnobtrusive.Validators;

    protected void Application_Start()
    {
        DataAnnotationsModelValidatorProvider.RegisterAdapter(
            typeof (RequiredIfAttribute), typeof (RequiredIfValidator));
        DataAnnotationsModelValidatorProvider.RegisterAdapter(
            typeof (AssertThatAttribute), typeof (AssertThatValidator));
    ```
    Alternatively, use predefined `ExpressiveAnnotationsModelValidatorProvider` (recommended):
    ```C#
    using ExpressiveAnnotations.MvcUnobtrusive.Providers;
    
    protected void Application_Start()
    {
        ModelValidatorProviders.Providers.Remove(
            ModelValidatorProviders.Providers
                .FirstOrDefault(x => x is DataAnnotationsModelValidatorProvider));
        ModelValidatorProviders.Providers.Add(
            new ExpressiveAnnotationsModelValidatorProvider());
    ```
    Despite the fact this provider automatically registers adapters for expressive validation attributes, it additionally respects their processing priorities when validation is performed (i.e. the [`Priority`](#signatures) property actually means something in practice).
3. Include [**expressive.annotations.validate.js**](src/expressive.annotations.validate.js?raw=true) script in your page (it should be included in bundle below jQuery validation files):

    ```JavaScript
    <script src="/Scripts/jquery.validate.js"></script>
    <script src="/Scripts/jquery.validate.unobtrusive.js"></script>
    ...
    <script src="/Scripts/expressive.annotations.validate.js"></script>
    ```

For supplementary reading visit the [installation section](#installation).

###<a id="frequently-asked-questions">Frequently asked questions</a>

#####<a id="is-it-possible-to-compile-all-usages-of-annotations-at-once">Is it possible to compile all usages of annotations at once?</a>

Yes, a complete list of types with annotations can be retrieved and compiled collectively. It can be useful, e.g. during unit tesing phase, when without the necessity of your main application startup, all the compile-time errors (syntax errors, typechecking errors) done to your expressions can be discovered. The following extension is helpful:

```C#
public static IEnumerable<ExpressiveAttribute> CompileExpressiveAttributes(this Type type)
{
    var properties = type.GetProperties()
        .Where(p => Attribute.IsDefined(p, typeof (ExpressiveAttribute)));
    var attributes = new List<ExpressiveAttribute>();
    foreach (var prop in properties)
    {
        var attribs = prop.GetCustomAttributes<ExpressiveAttribute>().ToList();
        attribs.ForEach(x => x.Compile(prop.DeclaringType));
        attributes.AddRange(attribs);
    }
    return attributes;
}
```
with the succeeding usage manner:

```C#
// compile all expressions for specified model:
var compiled = typeof (SomeModel).CompileExpressiveAttributes().ToList();

// ... or for current assembly:
compiled = Assembly.GetExecutingAssembly().GetTypes()
    .SelectMany(t => t.CompileExpressiveAttributes()).ToList();

// ... or for all assemblies within current domain:
compiled = AppDomain.CurrentDomain.GetAssemblies()
    .SelectMany(a => a.GetTypes()
        .SelectMany(t => t.CompileExpressiveAttributes())).ToList();
```
Notice that such compiled lambdas will be cached inside attributes instances stored in `compiled` list.
That means that subsequent compilation requests:
```C#
compiled.ForEach(x => x.Compile(typeof (SomeModel));
```
do nothing (due to optimization purposes), unless invoked with enabled recompilation switch:
```C#
compiled.ForEach(x => x.Compile(typeof (SomeModel), force: true); 
```
Finally, this reveals compile-time errors only, you can still can get runtime errors though, e.g.:

```C#
var parser = new Parser();
parser.AddFunction<object, bool>("CastToBool", obj => (bool) obj);

parser.Parse<object>("CastToBool(null)"); // compilation succeeds
parser.Parse<object>("CastToBool(null)").Invoke(null); // invocation fails (type casting err)
```

#####<a id="what-if-there-is-no-built-in-function-i-need">What if there is no built-in function I need?</a>

Create it yourself. Any custom function defined within the model class scope at server-side is automatically recognized and can be used inside expressions, e.g.
```C#
class Model
{
    public bool IsBloodType(string group) 
    {
        return Regex.IsMatch(group, @"^(A|B|AB|0)[\+-]$");
    }

    [AssertThat("IsBloodType(BloodType)")] // method known here (context aware expressions)
    public string BloodType { get; set; }
```
 If client-side validation is needed as well, function of the same signature (name and the number of parameters) must be available there. JavaScript corresponding implementation should be registered by the following instruction:
```JavaScript
<script>    
    ea.addMethod('IsBloodType', function(group) {
        return /^(A|B|AB|0)[\+-]$/.test(group);
    });
```
Many signatures can be defined for a single function name. Types are not taken under consideration as a differentiating factor though. Methods overloading is based on the number of arguments only. Functions with the same name and exact number of arguments are considered as ambiguous. The next issue important here is the fact that custom methods take precedence over built-in ones. If exact signatures are provided built-in methods are simply overridden by new definitions.

#####<a id="can-I-have-custom-utility-like-functions-outside-of-my-models">Can I have custom utility-like functions outside of my models?

Sure, provide your own methods provider, or extend existing global one, i.e.

* extend existing provider:

 ```C#
    protected void Application_Start()
    {
        Toolchain.Instance.AddFunction<int[], int>("ArrayLength", array => array.Length);
```
* define new provider:

 ```C#
    public class CustomFunctionsProvider : IFunctionsProvider
    {
        public IDictionary<string, IList<LambdaExpression>> GetFunctions()
        {
            return new Dictionary<string, IList<LambdaExpression>>
            {
                {"ArrayLength", new LambdaExpression[] {(Expression<Func<int[], int>>) (array => array.Length)}}
            };
        }
    }

    protected void Application_Start()
    {
        Toolchain.Instance.Recharge(new CustomFunctionsProvider()); 
```

#####<a id="how-to-cope-with-values-of-custom-types">How to cope with values of custom types?</a>

If you need to handle value string extracted from DOM field in any non built-in way, you can redefine given type-detection logic. The default mechanism recognizes and handles automatically types identified as: `timespan`, `datetime`, `numeric`, `string`, `bool` and `guid`. If non of them is matched for a particular field, JSON deserialization is invoked. You can provide your own deserializers though. The process is as follows:

* at server-side decorate your property with special attribute which gives a hint to client-side, which parser should be chosen for corresponding DOM field value deserialization:
    ```C#
    class Model
    {
        [ValueParser('customparser')]
        public CustomType SomeField { get; set; }
    ```

* at client-side register such a parser:
    ```JavaScript
    <script>
        ea.addValueParser('customparser', function(value, field) {
            // parameters: value - raw data string extracted by default from DOM element
            //             field - DOM element name for which parser was invoked
            return ... // handle exctracted field value string on your own
        });
    ```

Finally, there is a possibility to override built-in conversion globally. In this case, use the type name to register your value parser - all fields of such a type will be intercepted by it, e.g.
```JavaScript
<script>
    ea.addValueParser('typename', function (value) {
        return ... // handle specified type (numeric, datetime, etc.) parsing on your own
    });
```
If you redefine default mechanism, you can still have the `ValueParser` annotation on any fields you consider exceptional - annotation gives the highest parsing priority.

#####<a id="how-to-cope-with-dates-given-in-non-standard-formats">How to cope with dates given in non-standard formats?</a>

When values of DOM elements are extracted, they are converted to appropriate types. For fields containing date strings, JavaScript `Date.parse()` method is used by default. As noted in [MDN](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Date/parse), the input parameter is:

>A string representing an RFC 2822 or ISO 8601 date (other formats may be used, but results may be unexpected)

When some non-standard format needs to be handled, simply override the default behavior and provide your own implementation. E.g. when dealing with UK format dd/mm/yyyy, solution is:
```C#
class Model
{
    [ValueParser('ukdateparser')]
    public DateTime SomeField { get; set; }
```
```JavaScript
<script>
    ea.addValueParser('ukdateparser', function(value) {
        var arr = value.split('/');
        var date = new Date(arr[2], arr[1] - 1, arr[0]);
        return date.getTime(); // return msecs since January 1, 1970, 00:00:00 UTC
    });
```

#####<a id="what-if-ea-variable-is-already-used-by-another-library">What if `ea` variable is already used by another library?</a>

Use `noConflict()` method. In case of naming collision return control of the `ea` variable back to its origins. Old references of `ea` are saved during ExpressiveAnnotations initialization - `noConflict()` simply restores them:
```JavaScript
<script src="another.js"></script>
<script src="expressive.annotations.validate.js"></script>
<script>
    var expann = ea.noConflict();
    expann.addMethod... // do something with ExpressiveAnnotations
    ea... // do something with original ea variable
```

#####<a id="how-to-control-frequency-of-dependent-fields-validation">How to control frequency of dependent fields validation?</a>

When a field value is modified, validation results for some other fields, directly dependent on currenty modified one, may be affected. To control the frequency of when dependent fields validation is triggered, change default `ea.settings.dependencyTriggers` settings. It is a string containing one or more DOM field event types (such as *change*, *keyup* or custom event names), associated with currently modified field, for which fields directly dependent on are validated. Multiple event types can be bound at once by including each one separated by a space.

Default value is *'change keyup'* (for more information check `eventType` parameter of jQuery [`bind()`](http://api.jquery.com/bind) method). If you want to turn this feature off entirely, set it to *undefined* (validation will be fired on form submit attempt only).
```JavaScript
<script>
    ea.settings.dependencyTriggers = 'change'; // mute some excessive activity if you wish,
                                               // or turn it off entirely (set to undefined)
```
Alternatively, to enforce re-binding of already attached validation handlers, use following construction:
```JavaScript
<script>
    ea.settings.apply({
        dependencyTriggers: 'new set of events'
    });
```

#####<a id="can-i-increase-web-console-verbosity-for-debug-purposes">Can I increase web console verbosity for debug purposes?</a>

If you need more insightful overview of what client-side script is doing (including warnings if detected) enable logging:
```JavaScript
<script>
    ea.settings.debug = true; // output debug messages to the web console 
                              // (should be disabled for release code)
```

#####<a id="#how-to-fetch-field-value-or-display-name-in-error-message">How to fetch field value or display name in error message?</a>

* to get a value, wrap the field name in braces, e.g. `{field}`, or for nested fields - `{field.field}`,
* to get display name, given in `DisplayAttribute`, use additional `n` (or `N`) suffix, e.g. `{field:n}`. 

Notice that `{{` is treated as the escaped bracket character.

#####<a id="#is-there-any-event-raised-when-validation-is-done">Is there any event raised when validation is done?</a>

Each element validated by EA triggers an `eavalid` event, with the following extra parameters:

* type of the attribute for which validation was executed: `'requiredif'` or `'assertthat'`,
* state of the validation: `true` or `false`,
* expression which was evaluated.

Attach to it in the following manner:
```JavaScript
<script>
    $('form').find('input, select, textarea').on('eavalid', function(e, type, valid, expr) {
        console.log('event triggered by ' + e.currentTarget.name);
    });
```

#####<a id="#requiredif-attribute-is-not-working-what-is-wrong">`RequiredIf` attribute is not working, what is wrong?</a>

Make sure `RequiredIf` is applied to a field which *accepts null values*.

In the other words, it is redundant to apply this attribute to a field of non-nullable [value type](https://msdn.microsoft.com/en-us/library/s1ax56ch.aspx), like e.g. `int`, which is a struct representing integral numeric type, `DateTime`, etc. Because the value of such a type is always non-null, requirement demand is constantly fulfilled. Instead, for value types use their nullable forms, e.g. `int?`, `DateTime?`, etc.

```C#
[RequiredIf("true")] // no effect...
public int Value { get; set; } // ...unless int? is used
```
```C#
[RequiredIf("true")] // no effect...
public DateTime Value { get; set; } // ...unless DateTime? is used
```

#####<a id="#is-there-a-possibility-to-perform-asynchronous-validation">Is there a possibility to perform asynchronous validation?</a>

Currently not. Although there is an ongoing work on [async-work branch](https://github.com/jwaliszko/ExpressiveAnnotations/tree/async-work), created especially for asynchronous-related ideas. If you feel you'd like to contribute, either by providing better solution, review code or just test what is currently there, your help is always highly appreciated.

#####<a id="what-if-my-question-is-not-covered-by-faq-section">What if my question is not covered by FAQ section?</a>

If you're searching for an answer to some other problem, not covered by this document, try to browse through [already posted issues](../../issues?q=label%3Aquestion) labelled by *question* tag, or possibly have a look [at Stack Overflow](http://stackoverflow.com/search?tab=newest&q=expressiveannotations).

###<a id="installation">Installation instructions</a>

Simplest way is using the [NuGet](https://www.nuget.org) Package Manager Console:

* [complete package](https://www.nuget.org/packages/ExpressiveAnnotations) - both assemblies and the script included (allows [complete MVC validation](#what-about-the-support-of-aspnet-mvc-client-side-validation)):

    [![NuGet complete](https://img.shields.io/nuget/v/ExpressiveAnnotations.svg)](http://nuget.org/packages/ExpressiveAnnotations)

    ###`PM> Install-Package ExpressiveAnnotations`

* [minimal package](https://www.nuget.org/packages/ExpressiveAnnotations.dll) - core assembly only (MVC-related client-side coating components excluded):

    [![NuGet minimal](https://img.shields.io/nuget/v/ExpressiveAnnotations.dll.svg)](http://nuget.org/packages/ExpressiveAnnotations.dll)

    ###`PM> Install-Package ExpressiveAnnotations.dll`

###<a id="contributors">Contributors</a>

[GitHub Users](../../graphs/contributors)

Special thanks to Szymon Małczak

###<a id="license">License</a>

Copyright (c) 2014 Jarosław Waliszko

Licensed MIT: http://opensource.org/licenses/MIT
