// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Core
{
    internal interface IMainProgram
    {
        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}
