// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Core
{
    internal interface IFirstInstall
    {
        Task<bool> Install(CancellationToken cancellationToken);
    }
}
