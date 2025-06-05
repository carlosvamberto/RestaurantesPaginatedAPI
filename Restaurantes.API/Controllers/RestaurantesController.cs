using Microsoft.AspNetCore.Mvc;
using Restaurantes.Application.Requests;
using Restaurantes.Application.Services;
using Restaurantes.Domain.Entities;

namespace Restaurantes.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantesController : ControllerBase
    {
        private readonly IRestauranteService _restauranteService;

        public RestaurantesController(IRestauranteService restauranteService)
        {
            _restauranteService = restauranteService;
        }

        /// <summary>
        /// Lista restaurantes com base nos filtros fornecidos (Nome, Tipo e Cidade).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] GetRestaurantesRequest request)
        {
            var result = await _restauranteService.GetFilteredAsync(request);
            return Ok(result);
        }
    }
}
