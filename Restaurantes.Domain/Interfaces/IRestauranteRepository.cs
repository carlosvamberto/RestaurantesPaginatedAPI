using Restaurantes.Domain.Entities;

namespace Restaurantes.Domain.Interfaces
{
    public interface IRestauranteRepository
    {
        IQueryable<Restaurante> GetFilteredQuery(string? nome, string? tipo, string? cidade);
    }
}
