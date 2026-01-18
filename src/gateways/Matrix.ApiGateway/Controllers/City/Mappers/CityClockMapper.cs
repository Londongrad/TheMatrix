using Matrix.ApiGateway.Contracts.City.Responses;
using Matrix.ApiGateway.DownstreamClients.CityCore.Models;

namespace Matrix.ApiGateway.Controllers.City.Mappers
{
    public static class CityClockMapper
    {
        public static CityClockResponseDto ToBffResponse(this CityCoreClockResponseDto source)
        {
            return new CityClockResponseDto
            {
                CityId = source.CityId,
                SimTimeUtc = source.SimTimeUtc,
                TickId = source.TickId,
                Speed = source.Speed,
                State = MapState(source.State)
            };
        }

        private static string MapState(int state)
        {
            return state switch
            {
                1 => "running",
                2 => "paused",
                _ => "unknown"
            };
        }
    }
}
