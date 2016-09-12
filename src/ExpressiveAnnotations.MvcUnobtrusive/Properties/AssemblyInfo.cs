﻿/* https://github.com/jwaliszko/ExpressiveAnnotations
 * Copyright (c) 2014 Jarosław Waliszko
 * Licensed MIT: http://opensource.org/licenses/MIT */

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("ExpressiveAnnotations.MvcUnobtrusive")]
[assembly: AssemblyCopyright("Copyright © Jarosław Waliszko 2014")]
[assembly: AssemblyProduct("ExpressiveAnnotations.MvcUnobtrusive")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("7a61f32b-50cb-4c55-8122-6e1c8e672c29")]

#if !SIGNED
[assembly: InternalsVisibleTo("ExpressiveAnnotations.MvcUnobtrusive.Tests")]
#else
[assembly: InternalsVisibleTo("ExpressiveAnnotations.MvcUnobtrusive.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001000ff42e23a8247bbd1c54c6f4428d3e592e505391131b6e28a381dadbb26a88f8407d96afa9993877cd71d3b541470d8ebef5a8dcd780fccf270b1846e14d70b68732f98d8ba9dada92d1f128885fe903011a2185a4be124f5b00618413229f73692638e3c7c22d81cf9365f207a954c6b522183280c07011c325168c148865a4")]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("2.5.1.0")]
[assembly: AssemblyFileVersion("2.5.1.0")]
