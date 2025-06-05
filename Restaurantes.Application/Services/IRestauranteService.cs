using Restaurantes.Application.Requests;
using Restaurantes.Domain.Entities;

namespace Restaurantes.Application.Services
{
    public interface IRestauranteService
    {
        Task<PagedResult<Restaurante>> GetFilteredAsync(GetRestaurantesRequest request);
    }
}
