using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Restaurantes.Application.Requests;
using Restaurantes.Application.Services;
using Restaurantes.Domain.Entities;
using Restaurantes.Domain.Interfaces;
using System.Text.Json;

namespace Restaurantes.Infrastructure.Services
{
    public class RestauranteService : IRestauranteService
    {
        private readonly IRestauranteRepository _repository;
        private readonly IDistributedCache _cache;

        public RestauranteService(IRestauranteRepository repository, IDistributedCache cache)
        {
            _repository = repository;
            _cache = cache;
        }

        public async Task<PagedResult<Restaurante>> GetFilteredAsync(GetRestaurantesRequest request)
        {
            // Chave do cache agora inclui parâmetros de paginação
            var cacheKey = $"restaurantes:{request.Nome}:{request.Tipo}:{request.Cidade}:{request.PageNumber}:{request.PageSize}";

            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null && cached != "[]")
            {
                return JsonSerializer.Deserialize<PagedResult<Restaurante>>(cached)!;
            }

            // Obtém a query filtrada (sem executar ainda)
            var query = _repository.GetFilteredQuery(request.Nome, request.Tipo, request.Cidade);

            // Conta o total de itens (para paginação)
            var totalCount = await query.CountAsync();

            // Aplica paginação
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var result = new PagedResult<Restaurante>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var json = JsonSerializer.Serialize(result);

            await _cache.SetStringAsync(cacheKey, json, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            });

            return result;
        }
    }
}