## RestaurantesAPI - Article using .NET WebAPI Paginated + Redis

Recentemente desenvolvi uma **API REST** utilizando **.NET 8** com foco em performance e escalabilidade. Nesse projeto, implementei o **Redis** como mecanismo de cache, permitindo reduzir a carga no banco de dados e acelerar o tempo de resposta das requisições.

Agora, neste novo exemplo, irei além do cache, adicionando suporte à paginação nas consultas. A paginação é fundamental quando se trabalha com grandes volumes de dados, pois permite retornar os resultados de forma segmentada, melhorando a experiência do usuário e o consumo de recursos do sistema.

> I recently developed a **REST API** using **.NET 8**, focusing on performance and scalability. In that project, I implemented **Redis** as a caching mechanism to reduce database load and improve response times.
> 
> Now, in this new example, I’m going a step further by adding pagination support to the queries. Pagination is essential when dealing with large datasets, as it allows results to be returned in manageable segments, improving user experience and optimizing resource usage.

Antes de demonstrar as alterações necessárias, realizei um fork do projeto **RestaurantesAPI** no GitHub. O repositório está disponível no seguinte endereço: [https://github.com/carlosvamberto/RestaurantesAPI](https://github.com/carlosvamberto/RestaurantesAPI).

> Before demonstrating the required changes, I forked the **RestaurantesAPI** project on GitHub. The repository is available at: [https://github.com/carlosvamberto/RestaurantesAPI](https://github.com/carlosvamberto/RestaurantesAPI).

### Restaurantes.Application.Requests

A primeira alteração foi realizada no arquivo `GetRestaurantesRequest`, localizado em `Restaurantes.Application.Requests`. Nele, adicionei duas novas propriedades: `PageNumber` e `PageSize`, que serão utilizadas para controlar a paginação das consultas.

> The first change was made to the `GetRestaurantesRequest` file, located in `Restaurantes.Application.Requests`. I added two new properties: `PageNumber` and `PageSize`, which will be used to handle pagination in the queries.

```cs
namespace Restaurantes.Application.Requests
{
    public class GetRestaurantesRequest
    {
        public string? Nome { get; set; }
        public string? Tipo { get; set; }
        public string? Cidade { get; set; }
        public int PageNumber { get; set; } = 1; // Página atual, padrão é 1
        public int PageSize { get; set; } = 10; // Itens por página, padrão 10
    }
}
```

Ainda nessa mesma pasta, criei uma nova classe `PagedResult.cs` responsável por encapsular a resposta das consultas. Além da lista de restaurantes, essa classe também retorna as propriedades `PageSize` e `TotalPages`, permitindo um controle mais completo da paginação.

> In the same folder, I created a new class `PagedResult.cs` to encapsulate the query response. In addition to returning the list of restaurants, this class also includes the `PageSize` and `TotalPages` properties, providing more complete pagination control.

```cs
namespace Restaurantes.Application.Requests
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
```

### Restaurantes.Application.Services

Agora, vamos atualizar a interface `IRestauranteService`, substituindo o retorno do tipo `IEnumerable` por um `PagedResult`. Com isso, além da lista de restaurantes, também serão retornadas informações adicionais relacionadas à paginação.

> Now, we’ll update the `IRestauranteService` interface by replacing the `IEnumerable` return type with a `PagedResult`. This change allows us to return not only the list of restaurants but also additional pagination-related information.

```cs
using Restaurantes.Application.Requests;
using Restaurantes.Domain.Entities;

namespace Restaurantes.Application.Services
{
    public interface IRestauranteService
    {
        Task<PagedResult<Restaurante>> GetFilteredAsync(GetRestaurantesRequest request);
    }
}
```

### Restaurantes.Infrastructure.Services

Após a alteração na interface, o próximo passo é ajustar sua implementação na classe `RestauranteService`, garantindo que o método passe a retornar um `PagedResult` com os dados paginados corretamente.

> After updating the interface, the next step is to modify its implementation in the `RestauranteService` class, ensuring that the method now returns a properly structured `PagedResult` with paginated data.

```cs
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
```

### Restaurantes.Domain.Interfaces

Também vamos modificar a interface do repositório para que o método responsável pela listagem retorne um `IQueryable`, permitindo que a execução da consulta seja adiada e que filtros, paginação e projeções possam ser aplicados de forma mais flexível na camada de serviço.

> We will also update the repository interface so that the method responsible for listing returns an `IQueryable`. This allows deferred query execution and enables more flexible application of filters, pagination, and projections in the service layer.

```cs
using Microsoft.EntityFrameworkCore;
using Restaurantes.Domain.Entities;
using System.Linq;

namespace Restaurantes.Domain.Interfaces
{
    public interface IRestauranteRepository
    {
        IQueryable<Restaurante> GetFilteredQuery(string? nome, string? tipo, string? cidade);
    }
}
```

### Restaurantes.Infrastructure.Repositories

Agora, vamos modificar a implementação dessa interface, que está localizada no arquivo `RestauranteRepository`, para adequá-la às novas definições e suportar consultas paginadas.

> Now, we will update the implementation of this interface, located in the `RestauranteRepository` file, to align it with the new definitions and support paginated queries.

```cs
using Microsoft.EntityFrameworkCore;
using Restaurantes.Domain.Entities;
using Restaurantes.Domain.Interfaces;
using Restaurantes.Infrastructure.Context;
using System.Linq;

namespace Restaurantes.Infrastructure.Repositories
{
    public class RestauranteRepository : IRestauranteRepository
    {
        private readonly MyDbContext _context;

        public RestauranteRepository(MyDbContext context)
        {
            _context = context;
        }

        public IQueryable<Restaurante> GetFilteredQuery(string? nome, string? tipo, string? cidade)
        {
            var query = _context.Restaurantes.AsQueryable();

            if (!string.IsNullOrEmpty(nome))
                query = query.Where(r => r.Nome.Contains(nome));
            if (!string.IsNullOrEmpty(tipo))
                query = query.Where(r => r.Tipo.Contains(tipo));
            if (!string.IsNullOrEmpty(cidade))
                query = query.Where(r => r.Cidade.Contains(cidade));

            return query;
        }
    }
}
```

### Restaurantes.API.Controllers

Por fim, vamos atualizar o Controller para que ele aceite os parâmetros `PageSize` e `PageNumber`, que foram adicionados à classe `GetRestaurantesRequest`, permitindo a correta paginação das requisições.

> Finally, we will update the Controller to accept the `PageSize` and `PageNumber` parameters, which were added to the `GetRestaurantesRequest` class, enabling proper pagination of the requests.

```cs
using Microsoft.AspNetCore.Mvc;
using Restaurantes.Application.Requests;
using Restaurantes.Application.Services;

namespace Restaurantes.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantesController : ControllerBase
    {
        private readonly IRestauranteService _restauranteService;

        public RestaurantesController(IRestauranteService restauranteService)
        {
            _restauranteService = restauranteService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetRestaurantesRequest request)
        {
            var result = await _restauranteService.GetFilteredAsync(request);
            return Ok(result);
        }
    }
}
```
