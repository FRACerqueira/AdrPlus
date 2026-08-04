// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using System.Resources;
using System.Runtime.CompilerServices;

// Specifies the neutral culture for the assembly's resources
[assembly: NeutralResourcesLanguage("en-US")]

// Make internal types visible to the test assembly
[assembly: InternalsVisibleTo("AdrPlus.Tests")]

// Make internal types visible to NSubstitute for mocking
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
